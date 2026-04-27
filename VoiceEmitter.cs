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

        protected VoiceDataQueue<byte> _voiceBytesQueue;
        protected VoiceDataQueue<short> _voiceSamplesQueue;

        protected byte[] _emptyBytes;
        protected uint _writePosition;
        protected uint _availablePlaybackByteCount;
        protected uint _prevPlaybackPosition;
        protected bool _soundIsFull;
        protected bool _initialized;
        private bool _soundReleased;

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
                _voiceBytesQueue = new VoiceDataQueue<byte>((int)_soundParams.length);
            else
                _voiceSamplesQueue = new VoiceDataQueue<short>((int)(_soundParams.length / VoiceConsts.SampleSize));

            RuntimeManager.CoreSystem.createSound(
                string.Empty,
                MODE.LOOP_NORMAL | MODE.OPENUSER | MODE._3D,
                ref _soundParams,
                out _voiceSound);

            _prevPlaybackPosition = 0;
            _initialized = true;
        }

        public void EnqueueBytesForPlayback(Span<byte> voiceBytes)
        {
            if (_inputFormat != VoiceFormat.PCM16Bytes)
                throw new InvalidOperationException("Incorrect input format. Expected PCM16Bytes.");
            _voiceBytesQueue.Enqueue(voiceBytes);
        }

        public void EnqueueSamplesForPlayback(Span<short> voiceSamples)
        {
            if (_inputFormat != VoiceFormat.PCM16Samples)
                throw new InvalidOperationException("Incorrect input format. Expected PCM16Samples.");
            _voiceSamplesQueue.Enqueue(voiceSamples);
        }

        public abstract void SetVolume(float volume);
        public abstract void SetOcclusion(float value);

        protected void WriteVoiceBytes(byte[] voiceBytes, uint writePosition, uint byteCount)
        {
            if (byteCount == 0) return;
            _voiceSound.@lock(writePosition, byteCount, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);
            if (len1 > 0) Marshal.Copy(voiceBytes, 0, ptr1, (int)len1);
            if (len2 > 0) Marshal.Copy(voiceBytes, (int)len1, ptr2, (int)len2);
            _voiceSound.unlock(ptr1, ptr2, len1, len2);
        }

        protected void WriteVoiceSamples(short[] voiceSamples, uint writePosition, uint sampleCount)
        {
            if (sampleCount == 0) return;
            uint byteCount = sampleCount * VoiceConsts.SampleSize;
            _voiceSound.@lock(writePosition, byteCount, out IntPtr ptr1, out IntPtr ptr2, out uint len1, out uint len2);
            int samplesInPtr1 = (int)(len1 / VoiceConsts.SampleSize);
            int samplesInPtr2 = (int)(len2 / VoiceConsts.SampleSize);
            if (samplesInPtr1 > 0) Marshal.Copy(voiceSamples, 0, ptr1, samplesInPtr1);
            if (samplesInPtr2 > 0) Marshal.Copy(voiceSamples, samplesInPtr1, ptr2, samplesInPtr2);
            _voiceSound.unlock(ptr1, ptr2, len1, len2);
        }

        protected abstract void SetPaused(bool isPaused);

        protected uint GetPlayedByteCount(uint startPos, uint endPos)
        {
            return endPos >= startPos
                ? endPos - startPos
                : _soundParams.length - startPos + endPos;
        }

        protected uint GetAvailablePlaybackByteCount(uint playbackPos, uint writePos, bool soundIsFull = false)
        {
            if (writePos > playbackPos) return writePos - playbackPos;
            if (writePos < playbackPos) return _soundParams.length - playbackPos + writePos;
            return soundIsFull ? 0 : _soundParams.length;
        }

        protected uint GetAvailableWriteByteCount(uint playbackPos, uint writePos, bool soundIsFull = false)
        {
            if (writePos < playbackPos) return playbackPos - writePos;
            if (writePos > playbackPos) return _soundParams.length - writePos + playbackPos;
            return soundIsFull ? 0 : _soundParams.length;
        }

        protected virtual void Update()
        {
            if (!_initialized) return;

            _channel.getPosition(out uint playbackPosition, TIMEUNIT.PCMBYTES);

            uint bytesPlayed = GetPlayedByteCount(_prevPlaybackPosition, playbackPosition);

            if (_availablePlaybackByteCount > 0 && bytesPlayed > _availablePlaybackByteCount)
                _writePosition = playbackPosition;

            if (bytesPlayed > 0)
                WriteVoiceBytes(_emptyBytes, _prevPlaybackPosition, bytesPlayed);

            uint availableWrite = GetAvailableWriteByteCount(playbackPosition, _writePosition, _soundIsFull);

            uint writeLength;
            if (_inputFormat == VoiceFormat.PCM16Bytes)
                writeLength = (uint)Mathf.Min(_voiceBytesQueue.EnqueuePosition, availableWrite);
            else
                writeLength = (uint)Mathf.Min(_voiceSamplesQueue.EnqueuePosition, availableWrite / VoiceConsts.SampleSize);

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

                uint writtenBytes = (_inputFormat == VoiceFormat.PCM16Bytes)
                    ? writeLength
                    : writeLength * VoiceConsts.SampleSize;

                _writePosition = (_writePosition + writtenBytes) % _soundParams.length;
                _soundIsFull = _writePosition == playbackPosition;
            }

            _availablePlaybackByteCount = GetAvailablePlaybackByteCount(playbackPosition, _writePosition, _soundIsFull);
            SetPaused(_availablePlaybackByteCount == 0);
            _prevPlaybackPosition = playbackPosition;
        }

        protected virtual void OnDestroy()
        {
            if (_initialized && !_soundReleased)
            {
                _voiceSound.release();
                _soundReleased = true;
            }
        }
    }
}