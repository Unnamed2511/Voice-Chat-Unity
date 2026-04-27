using System;
using System.Runtime.InteropServices;
using FMOD;
using FMODUnity;
using UnityEngine;

namespace ProximityChat
{
    public abstract class VoiceEmitter : MonoBehaviour
    {
        protected VoiceFormat _inputFormat;
        protected Sound _voiceSound;
        protected CREATESOUNDEXINFO _soundParams;
        protected uint _sampleRate;
        protected int _channelCount;
        protected Channel _channel;
        protected VoiceDataQueue _voiceBytesQueue;
        protected VoiceDataQueue _voiceSamplesQueue;
        protected byte[] _emptyBytes;
        protected uint _writePosition;
        protected uint _availablePlaybackByteCount;
        protected uint _prevPlaybackPosition;
        protected bool _soundIsFull;
        protected bool _initialized = false;

        public virtual void Init(uint sampleRate = 48000, int channelCount = 1, VoiceFormat inputFormat = VoiceFormat.PCM16Samples)
        {
            _sampleRate = sampleRate;
            _channelCount = channelCount;
            _inputFormat = inputFormat;

            _soundParams.cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
            _soundParams.numchannels = _channelCount;
            _soundParams.defaultfrequency = (int)_sampleRate;
            _soundParams.format = SOUND_FORMAT.PCM16;
            _soundParams.length = _sampleRate * VoiceConsts.SampleSize * (uint)_channelCount;

            _emptyBytes = new byte[_soundParams.length];

            if (_inputFormat == VoiceFormat.PCM16Bytes)
            {
                _voiceBytesQueue = new VoiceDataQueue<byte>(_soundParams.length);
            }
            else
            {
                _voiceSamplesQueue = new VoiceDataQueue<short>(_soundParams.length / VoiceConsts.SampleSize);
            }

            RuntimeManager.CoreSystem.createSound(
                _soundParams.userdata,
                MODE.LOOP_NORMAL | MODE.OPENUSER | MODE._3D,
                ref _soundParams,
                out _voiceSound);

            _initialized = true;
        }

        public void EnqueueBytesForPlayback(Span<byte> voiceBytes)
        {
            if (_inputFormat != VoiceFormat.PCM16Bytes)
                throw new Exception("Incorrect input format. Failed to enqueue voice bytes.");
            _voiceBytesQueue.Enqueue(voiceBytes);
        }

        public void EnqueueSamplesForPlayback(Span<short> voiceSamples)
        {
            if (_inputFormat != VoiceFormat.PCM16Samples)
                throw new Exception("Incorrect input format. Failed to dequeue voice bytes.");
            _voiceSamplesQueue.Enqueue(voiceSamples);
        }

        public abstract void SetVolume(float volume);
        public abstract void SetOcclusion(float value);

        protected void WriteVoiceBytes(byte[] voiceBytes, uint writePosition, uint byteCount)
        {
            if (byteCount <= 0) return;

            _voiceSound.@lock(writePosition, byteCount, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);

            if (len1 > 0)
                Marshal.Copy(voiceBytes, 0, ptr1, (int)len1);
            if (len2 > 0)
                Marshal.Copy(voiceBytes, (int)len1, ptr2, (int)len2);

            _voiceSound.unlock(ptr1, ptr2, len1, len2);
        }

        protected void WriteVoiceSamples(short[] voiceSamples, uint writePosition, uint sampleCount)
        {
            if (sampleCount <= 0) return;

            _voiceSound.@lock(writePosition, sampleCount * VoiceConsts.SampleSize, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);

            uint samplesInPtr1 = len1 / VoiceConsts.SampleSize;
            uint samplesInPtr2 = len2 / VoiceConsts.SampleSize;

            if (samplesInPtr1 > 0)
                Marshal.Copy(voiceSamples, 0, ptr1, (int)samplesInPtr1);
            if (samplesInPtr2 > 0)
                Marshal.Copy(voiceSamples, (int)samplesInPtr1, ptr2, (int)samplesInPtr2);

            _voiceSound.unlock(ptr1, ptr2, len1, len2);
        }

        protected abstract void SetPaused(bool isPaused);

        protected uint GetPlayedByteCount(uint playbackStartPosition, uint playbackEndPosition)
        {
            return playbackEndPosition >= playbackStartPosition
                ? playbackEndPosition - playbackStartPosition
                : _soundParams.length - playbackStartPosition + playbackEndPosition;
        }

        protected uint GetAvailablePlaybackByteCount(uint playbackPosition, uint writePosition, bool soundIsFull = false)
        {
            if (writePosition > playbackPosition)
                return writePosition - playbackPosition;
            else if (writePosition < playbackPosition)
                return _soundParams.length - playbackPosition + writePosition;
            else
                return soundIsFull ? _soundParams.length : 0;
        }

        protected uint GetAvailableWriteByteCount(uint playbackPosition, uint writePosition, bool soundIsFull = false)
        {
            if (writePosition < playbackPosition)
                return playbackPosition - writePosition;
            else if (writePosition > playbackPosition)
                return _soundParams.length - writePosition + playbackPosition;
            else
                return soundIsFull ? 0 : _soundParams.length;
        }

        protected virtual void Update()
        {
            if (!_initialized) return;

            _channel.getPosition(out uint playbackPosition, TIMEUNIT.PCMBYTES);

            uint bytesPlayedSinceLastFrame = GetPlayedByteCount(_prevPlaybackPosition, playbackPosition);

            if (_availablePlaybackByteCount > 0 && bytesPlayedSinceLastFrame > _availablePlaybackByteCount)
            {
                _writePosition = playbackPosition;
            }

            if (bytesPlayedSinceLastFrame > 0)
            {
                WriteVoiceBytes(_emptyBytes, _prevPlaybackPosition, bytesPlayedSinceLastFrame);
            }

            uint availableWriteByteCount = GetAvailableWriteByteCount(playbackPosition, _writePosition, _soundIsFull);
            uint writeLength = (_inputFormat == VoiceFormat.PCM16Bytes)
                ? (uint)Mathf.Min(_voiceBytesQueue.EnqueuePosition, availableWriteByteCount)
                : (uint)Mathf.Min(_voiceSamplesQueue.EnqueuePosition, availableWriteByteCount / VoiceConsts.SampleSize);

            if (writeLength > 0)
            {
                if (_inputFormat == VoiceFormat.PCM16Bytes)
                {
                    WriteVoiceBytes(_voiceBytesQueue.Data, _writePosition, writeLength);
                    _voiceBytesQueue.Dequeue((int)writeLength);
                }
                else
                {
                    WriteVoiceSamples(_voiceSamplesQueue.Data, _writePosition, writeLength);
                    _voiceSamplesQueue.Dequeue((int)writeLength);
                }

                uint writeLengthBytes = (_inputFormat == VoiceFormat.PCM16Bytes)
                    ? writeLength
                    : writeLength * VoiceConsts.SampleSize;

                _writePosition = (uint)Mathf.Repeat(_writePosition + writeLengthBytes, _soundParams.length);
                _soundIsFull = _writePosition == playbackPosition;
            }

            _availablePlaybackByteCount = GetAvailablePlaybackByteCount(playbackPosition, _writePosition, _soundIsFull);
            SetPaused(_availablePlaybackByteCount == 0);
            _prevPlaybackPosition = playbackPosition;
        }

        protected virtual void OnDestroy()
        {
            if (_initialized)
            {
                _voiceSound.release();
            }
        }
    }
}
