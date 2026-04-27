using System;
using System.Buffers;
using Mirror;
using ProximityChat;
using UnityEngine;

public enum VoiceChatMode { PushToTalk, Toggle }

public class VoiceNetworker : NetworkBehaviour
{
    [Header("Voice Chat Mode")]
    [SerializeField] private VoiceChatMode _voiceChatMode = VoiceChatMode.PushToTalk;

    [Header("Voice Level Monitor")]
    [SerializeField] private float _meterSensitivity = 4f;

    private VoiceRecorder _voiceRecorder;
    private VoiceEmitter _voiceEmitter;
    private VoiceEncoder _voiceEncoder;
    private VoiceDecoder _voiceDecoder;

    private bool _isActive;
    private bool _prevInput;
    private bool _playbackOwnVoice;
    private float _currentVoiceLevel;
    private float _peakVoiceLevel;
    private float _peakHoldTimer;

    private const float PeakHoldTime = 1.2f;
    private const float PeakDecayRate = 1.5f;
    private const float LevelSmoothUp = 25f;
    private const float LevelSmoothDown = 8f;

    public bool IsPlaybackOwnVoiceEnabled => _playbackOwnVoice;
    public VoiceChatMode CurrentVoiceChatMode => _voiceChatMode;
    public bool IsVoiceActive => _isActive;
    public float CurrentVoiceLevel => _currentVoiceLevel;
    public float PeakVoiceLevel => _peakVoiceLevel;
    public VoiceEmitter VoiceEmitter => _voiceEmitter;

    public float MeterSensitivity
    {
        get => _meterSensitivity;
        set => _meterSensitivity = Mathf.Clamp(value, 1f, 20f);
    }

    private void Awake()
    {
        _voiceRecorder = GetComponent<VoiceRecorder>();
        _voiceEmitter = GetComponent<VoiceEmitter>();
    }

    private void Start()
    {
        if (isLocalPlayer)
        {
            _voiceRecorder.enabled = true;
            _voiceRecorder.Init();
            _voiceEncoder = new VoiceEncoder(_voiceRecorder.RecordedSamplesQueue);
            _voiceDecoder = new VoiceDecoder();
            _voiceEmitter.Init(VoiceConsts.OpusSampleRate);
            _voiceEmitter.enabled = _playbackOwnVoice;
        }
        else
        {
            _voiceRecorder.enabled = false;
            _voiceDecoder = new VoiceDecoder();
            _voiceEmitter.Init(VoiceConsts.OpusSampleRate);
            _voiceEmitter.enabled = true;
        }

        ChangeSavedMicDevice();
        ChangeSavedMicMode();
    }

    private void OnEnable()
    {
        if (!TryGetSettings(out SettingsManager sm)) return;
        sm.OnMicrophoneModeChanged += ChangeSavedMicMode;
        sm.OnMicrophoneDeviceChanged += ChangeSavedMicDevice;
    }

    private void OnDisable()
    {
        if (!TryGetSettings(out SettingsManager sm)) return;
        sm.OnMicrophoneModeChanged -= ChangeSavedMicMode;
        sm.OnMicrophoneDeviceChanged -= ChangeSavedMicDevice;
    }

    private bool TryGetSettings(out SettingsManager sm)
    {
        sm = SettingsManager.Instance;
        if (sm == null) Debug.LogWarning("SettingsManager not initialized!");
        return sm != null;
    }

    private void ChangeSavedMicMode()
    {
        if (!TryGetSettings(out SettingsManager sm)) return;
        SetVoiceChatMode(sm.MicrophoneMode == "Toggle" ? VoiceChatMode.Toggle : VoiceChatMode.PushToTalk);
    }

    private void ChangeSavedMicDevice()
    {
        if (!TryGetSettings(out SettingsManager sm)) return;
        SetVoiceChatDevice(sm.MicrophoneDeviceIndex);
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        bool firePressed = InputManager.Instance.VoiceChatInput > 0f;
        bool justPressed = firePressed && !_prevInput;
        bool justReleased = !firePressed && _prevInput;

        switch (_voiceChatMode)
        {
            case VoiceChatMode.PushToTalk:
                if (justPressed) { _isActive = true; StartRecording(); }
                else if (justReleased) { _isActive = false; StopRecording(); }
                break;

            case VoiceChatMode.Toggle:
                if (justPressed)
                {
                    _isActive = !_isActive;
                    if (_isActive) StartRecording(); else StopRecording();
                }
                break;
        }

        _prevInput = firePressed;
    }

    public void SetVoiceChatDevice(int index)
    {
        if (_voiceRecorder.DriverIndex == index) return;
        if (_isActive) { _isActive = false; StopRecording(); }
        _voiceRecorder.ChangeRecordingIndex(index);
#if UNITY_EDITOR
        Debug.Log($"Microphone device index changed: {index}");
#endif
    }

    public void SetVoiceChatMode(VoiceChatMode newMode)
    {
        if (_voiceChatMode == newMode) return;
        if (_isActive) { _isActive = false; StopRecording(); }
        _voiceChatMode = newMode;
    }

    public void SetPlaybackOwnVoice(bool enabled)
    {
        if (!isLocalPlayer) return;
        _playbackOwnVoice = enabled;
        _voiceEmitter.enabled = enabled;
    }

    [Command]
    public void SendEncodedVoiceServerRpc(byte[] encodedVoiceData)
    {
        SendEncodedVoiceClientRpc(encodedVoiceData);
    }

    [ClientRpc]
    public void SendEncodedVoiceClientRpc(byte[] encodedVoiceData)
    {
        if (isLocalPlayer && !_playbackOwnVoice) return;
        if (_voiceDecoder == null)
        {
            Debug.LogWarning("VoiceDecoder not initialized.");
            return;
        }
        Span<short> decoded = _voiceDecoder.DecodeVoiceSamples(encodedVoiceData.AsSpan());
        _voiceEmitter.EnqueueSamplesForPlayback(decoded);
    }

    public void StartRecording()
    {
        if (!isLocalPlayer) return;
        _voiceRecorder.StartRecording();
    }

    public void StopRecording()
    {
        if (!isLocalPlayer) return;
        _voiceRecorder.StopRecording();
    }

    public void SetOutputVolume(float volume) => _voiceEmitter.SetVolume(volume);
    public void SetOcclusionValue(float value) => _voiceEmitter.SetOcclusion(value);

    private void LateUpdate()
    {
        if (!isLocalPlayer) return;

        bool hadVoice = false;

        if (_voiceEncoder.HasVoiceLeftToEncode)
        {
            SendVoiceFrame(_voiceEncoder.GetEncodedVoice());
            hadVoice = true;
        }

        if (!_voiceRecorder.IsRecording && !_voiceEncoder.QueueIsEmpty)
        {
            SendVoiceFrame(_voiceEncoder.GetEncodedVoice(true));
            hadVoice = true;
        }

        if (!hadVoice) DecayVoiceLevel();
    }

    private void SendVoiceFrame(Span<byte> encodedVoice)
    {
        if (encodedVoice.IsEmpty) return;

        byte[] rented = ArrayPool<byte>.Shared.Rent(encodedVoice.Length);
        encodedVoice.CopyTo(rented);

        UpdateVoiceLevel(rented, encodedVoice.Length);
        SendEncodedVoiceServerRpc(encodedVoice.ToArray());

        ArrayPool<byte>.Shared.Return(rented);
    }

    private void UpdateVoiceLevel(byte[] encodedData, int length)
    {
        float sumSquares = 0f;
        for (int i = 0; i < length - 1; i += 2)
        {
            short sample = (short)(encodedData[i] | (encodedData[i + 1] << 8));
            float normalized = sample / 32768f;
            sumSquares += normalized * normalized;
        }

        int sampleCount = length / 2;
        float rms = sampleCount > 0 ? Mathf.Sqrt(sumSquares / sampleCount) : 0f;
        float targetLevel = Mathf.Clamp01(rms * _meterSensitivity);

        float smoothSpeed = targetLevel > _currentVoiceLevel ? LevelSmoothUp : LevelSmoothDown;
        _currentVoiceLevel = Mathf.Lerp(_currentVoiceLevel, targetLevel, Time.deltaTime * smoothSpeed);

        if (targetLevel > _peakVoiceLevel)
        {
            _peakVoiceLevel = targetLevel;
            _peakHoldTimer = PeakHoldTime;
        }
        else
        {
            _peakHoldTimer -= Time.deltaTime;
            if (_peakHoldTimer <= 0f)
                _peakVoiceLevel = Mathf.MoveTowards(_peakVoiceLevel, 0f, PeakDecayRate * Time.deltaTime);
        }
    }

    private void DecayVoiceLevel()
    {
        _currentVoiceLevel = Mathf.Lerp(_currentVoiceLevel, 0f, Time.deltaTime * LevelSmoothDown);
        _peakHoldTimer -= Time.deltaTime;
        if (_peakHoldTimer <= 0f)
            _peakVoiceLevel = Mathf.MoveTowards(_peakVoiceLevel, 0f, PeakDecayRate * Time.deltaTime);
        if (_currentVoiceLevel < 0.001f) _currentVoiceLevel = 0f;
        if (_peakVoiceLevel < 0.001f) _peakVoiceLevel = 0f;
    }

    private void OnDestroy()
    {
        _voiceEncoder?.Dispose();
        _voiceDecoder?.Dispose();
    }
}