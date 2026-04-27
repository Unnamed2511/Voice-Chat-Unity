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
            RuntimeManager.CoreSystem.playSound(_voiceSound, _channelGroup, true, out _channel);
        }

        public override void SetVolume(float volume)
        {
            _channel.setVolume(volume);
        }

        public override void SetOcclusion(float value)
        {
        }

        protected override void SetPaused(bool isPaused)
        {
            _channel.setPaused(isPaused);
        }

        protected override void Update()
        {
            if (!_initialized) return;

            base.Update();

            Vector3 position = transform.position;
            Vector3 velocity = (position - _prevPosition) / Time.deltaTime;
            ATTRIBUTES_3D attributes3D = RuntimeUtils.To3DAttributes(transform, velocity);
            _channel.set3DAttributes(ref attributes3D.position, ref attributes3D.velocity);
            _prevPosition = position;
        }
    }
}
