using FMOD;
using FMODUnity;
using UnityEngine;

namespace ProximityChat
{
    public class CoreVoiceEmitter : VoiceEmitter
    {
        protected ChannelGroup _channelGroup;
        protected Vector3 _prevPosition;

        public override void Init(uint sampleRate = 48000, int channelCount = 1, VoiceFormat inputFormat = VoiceFormat.PCM16Samples)
        {
            base.Init(sampleRate, channelCount, inputFormat);
            _prevPosition = transform.position;
            RuntimeManager.CoreSystem.playSound(_voiceSound, _channelGroup, true, out _channel);
        }

        public override void SetVolume(float volume)
        {
            _channel.setVolume(volume);
        }

        public override void SetOcclusion(float value) { }

        protected override void SetPaused(bool isPaused)
        {
            _channel.setPaused(isPaused);
        }

        protected override void Update()
        {
            if (!_initialized) return;

            base.Update();

            Vector3 position = transform.position;
            Vector3 velocity = Time.deltaTime > 0f
                ? (position - _prevPosition) / Time.deltaTime
                : Vector3.zero;

            ATTRIBUTES_3D attributes = RuntimeUtils.To3DAttributes(transform, velocity);
            _channel.set3DAttributes(ref attributes.position, ref attributes.velocity);
            _prevPosition = position;
        }
    }
}