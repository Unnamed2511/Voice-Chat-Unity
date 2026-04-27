using System;
using System.Collections;
using System.Runtime.InteropServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace ProximityChat
{
    public class StudioVoiceEmitter : VoiceEmitter
    {
        private const float MaxWaitSeconds = 5f;

        [Header("FMOD Programmer Instrument Event Reference")]
        [SerializeField] protected EventReference _voiceEventReference;

        protected EVENT_CALLBACK _voiceCallback;
        protected EventInstance _voiceEventInstance;
        protected float _volume = 1f;
        protected float _occlusion = 0f;

        private GCHandle _soundHandle;

        public override void Init(uint sampleRate = 48000, int channelCount = 1, VoiceFormat inputFormat = VoiceFormat.PCM16Samples)
        {
            base.Init(sampleRate, channelCount, inputFormat);
            _initialized = false;

            _soundHandle = GCHandle.Alloc(_voiceSound);
            _voiceCallback = VoiceEventCallback;

            _voiceEventInstance = RuntimeManager.CreateInstance(_voiceEventReference);
            _voiceEventInstance.setUserData(GCHandle.ToIntPtr(_soundHandle));
            _voiceEventInstance.setCallback(_voiceCallback);
            _voiceEventInstance.start();
            _voiceEventInstance.setPaused(true);

            RuntimeManager.AttachInstanceToGameObject(_voiceEventInstance, gameObject, true);
            StartCoroutine(WaitToGetChannel());
        }

        public override void SetVolume(float volume)
        {
            _volume = volume;
            _voiceEventInstance.setVolume(_volume);
        }

        public override void SetOcclusion(float value)
        {
            _occlusion = value;
            _voiceEventInstance.setParameterByName("AudioOcclusion", _occlusion);
        }

        protected override void SetPaused(bool isPaused)
        {
            _voiceEventInstance.setPaused(isPaused);
        }

        private IEnumerator WaitToGetChannel()
        {
            float elapsed = 0f;
            while (elapsed < MaxWaitSeconds)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
                _voiceEventInstance.getPlaybackState(out PLAYBACK_STATE state);
                if (state == PLAYBACK_STATE.PLAYING) break;
            }

            if (elapsed >= MaxWaitSeconds)
            {
                yield break;
            }

            if (FMODUtilities.TryGetChannelForEvent(_voiceEventInstance, out Channel channel))
            {
                _channel = channel;
                _initialized = true;
            }
        }

        [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        private static RESULT VoiceEventCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
        {
            if (type != EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND) return RESULT.OK;

            var instance = new EventInstance { handle = instancePtr };
            instance.getUserData(out IntPtr userDataPtr);
            if (userDataPtr == IntPtr.Zero) return RESULT.OK;

            var handle = GCHandle.FromIntPtr(userDataPtr);
            if (!handle.IsAllocated) return RESULT.OK;

            var sound = (Sound)handle.Target;
            var param = (PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));
            param.sound = sound.handle;
            param.subsoundIndex = -1;
            Marshal.StructureToPtr(param, parameterPtr, false);

            return RESULT.OK;
        }

        protected override void OnDestroy()
        {
            if (_soundHandle.IsAllocated)
                _soundHandle.Free();

            if (_voiceEventInstance.isValid())
                _voiceEventInstance.release();

            base.OnDestroy();
        }
    }
}