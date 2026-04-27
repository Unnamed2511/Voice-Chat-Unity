using Mirror;
using ProximityChat;
using System;
using UnityEngine;

public enum VoiceChatMode
{
    PushToTalk,
    Toggle
}

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
    private VoiceDecoder _monitorDecoder;

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
            _monitorDecoder = new VoiceDecoder();
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
        if (SettingsManager.Instance == null)
        {
            Debug.LogWarning("SettingsManager not initialized!");
            return;
        }
        SettingsManager.Instance.OnMicrophoneModeChanged += ChangeSavedMicMode;
        SettingsManager.Instance.OnMicrophoneDeviceChanged += ChangeSavedMicDevice;
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogWarning("SettingsManager not initialized!");
            return;
        }
        SettingsManager.Instance.OnMicrophoneModeChanged -= ChangeSavedMicMode;
        SettingsManager.Instance.OnMicrophoneDeviceChanged -= ChangeSavedMicDevice;
    }

    private void ChangeSavedMicMode()
    {
        SettingsManager settingsManager = SettingsManager.Instance;
        if (settingsManager == null)
        {
            Debug.LogWarning("SettingsManager not initialized!");
            return;
        }

        switch (settingsManager.MicrophoneMode)
        {
            case "PushToTalk":
                SetVoiceChatMode(VoiceChatMode.PushToTalk);
                break;
            case "Toggle":
                SetVoiceChatMode(VoiceChatMode.Toggle);
                break;
        }
    }

    private void ChangeSavedMicDevice()
    {
        SettingsManager settingsManager = SettingsManager.Instance;
        if (settingsManager == null)
        {
            Debug.LogWarning("SettingsManager not initialized!");
            return;
        }
        SetVoiceChatDevice(settingsManager.MicrophoneDeviceIndex);
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
                if (justPressed)
                {
                    _isActive = true;
                    StartRecording();
                }
                else if (justReleased)
                {
                    _isActive = false;
                    StopRecording();
                }
                break;

            case VoiceChatMode.Toggle:
                if (justPressed)
                {
                    _isActive = !_isActive;
                    if (_isActive) StartRecording();
                    else StopRecording();
                }
                break;
        }

        _prevInput = firePressed;
    }

    public void SetVoiceChatDevice(int index)
    {
        if (_voiceRecorder.DriverIndex == index) return;

        if (_isActive)
        {
            _isActive = false;
            StopRecording();
        }

        _voiceRecorder.ChangeRecordingIndex(index);
        Debug.Log($"Microphone device index: {index}");
    }

    public void SetVoiceChatMode(VoiceChatMode newMode)
    {
        if (_voiceChatMode == newMode) return;

        if (_isActive)
        {
            _isActive = false;
            StopRecording();
        }

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
            Debug.LogWarning("VoiceDecoder not initialized on this client.");
            return;
        }

        Span<short> decodedVoiceSamples = _voiceDecoder.DecodeVoiceSamples(encodedVoiceData);
        _voiceEmitter.EnqueueSamplesForPlayback(decodedVoiceSamples);
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

    public void SetOutputVolume(float volume)
    {
        _voiceEmitter.SetVolume(volume);
    }

    public void SetOcclusionValue(float value)
    {
        _voiceEmitter.SetOcclusion(value);
    }

    private void LateUpdate()
    {
        if (!isLocalPlayer) return;

        bool hadVoiceThisFrame = false;

        if (_voiceEncoder.HasVoiceLeftToEncode)
        {
            Span<byte> encodedVoice = _voiceEncoder.GetEncodedVoice();
            byte[] encodedArray = encodedVoice.ToArray();
            UpdateVoiceLevel(encodedArray);
            hadVoiceThisFrame = true;
            SendEncodedVoiceServerRpc(encodedArray);
        }

        if (!_voiceRecorder.IsRecording && !_voiceEncoder.QueueIsEmpty)
        {
            Span<byte> encodedVoice = _voiceEncoder.GetEncodedVoice(true);
            byte[] encodedArray = encodedVoice.ToArray();
            UpdateVoiceLevel(encodedArray);
            hadVoiceThisFrame = true;
            SendEncodedVoiceServerRpc(encodedArray);
        }

        if (!hadVoiceThisFrame)
        {
            DecayVoiceLevel();
        }
    }

    private void UpdateVoiceLevel(byte[] encodedData)
    {
        if (_monitorDecoder == null) return;

        Span<short> samples = _monitorDecoder.DecodeVoiceSamples(encodedData);
        if (samples.Length == 0) return;

        float sumSquares = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float normalized = samples[i] / 32768f;
            sumSquares += normalized * normalized;
        }

        float rms = Mathf.Sqrt(sumSquares / samples.Length);
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
            {
                _peakVoiceLevel = Mathf.MoveTowards(_peakVoiceLevel, 0f, PeakDecayRate * Time.deltaTime);
            }
        }
    }

    private void DecayVoiceLevel()
    {
        _currentVoiceLevel = Mathf.Lerp(_currentVoiceLevel, 0f, Time.deltaTime * LevelSmoothDown);
        _peakHoldTimer -= Time.deltaTime;

        if (_peakHoldTimer <= 0f)
        {
            _peakVoiceLevel = Mathf.MoveTowards(_peakVoiceLevel, 0f, PeakDecayRate * Time.deltaTime);
        }

        if (_currentVoiceLevel < 0.001f) _currentVoiceLevel = 0f;
        if (_peakVoiceLevel < 0.001f) _peakVoiceLevel = 0f;
    }
}
