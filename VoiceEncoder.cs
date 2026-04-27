using System;
using Concentus.Enums;
using Concentus.Structs;

namespace ProximityChat
{
    public sealed class VoiceEncoder : IDisposable
    {
        private static readonly int[] FrameSizes = { 2880, 1920, 960, 480, 240, 120 };

        private readonly VoiceDataQueue<short> _samplesQueue;
        private readonly OpusEncoder _opusEncoder;
        private readonly byte[] _encodeBuffer;
        private bool _disposed;

        private int MinFrameSize => FrameSizes[FrameSizes.Length - 1];

        public bool HasVoiceLeftToEncode => _samplesQueue.EnqueuePosition > MinFrameSize;
        public bool QueueIsEmpty => _samplesQueue.EnqueuePosition == 0;

        public VoiceEncoder(VoiceDataQueue<short> voiceSamplesQueue)
        {
            _samplesQueue = voiceSamplesQueue;
            _opusEncoder = new OpusEncoder(VoiceConsts.OpusSampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
            _encodeBuffer = new byte[VoiceConsts.MaxOpusFrameSize * VoiceConsts.SampleSize];
        }

        public Span<byte> GetEncodedVoice(bool forceEncodeWithSilence = false)
        {
            if (forceEncodeWithSilence
                && _samplesQueue.EnqueuePosition > 0
                && _samplesQueue.EnqueuePosition < MinFrameSize)
            {
                int silenceNeeded = MinFrameSize - _samplesQueue.EnqueuePosition;
                Span<short> silence = stackalloc short[silenceNeeded];
                _samplesQueue.Enqueue(silence);
            }

            int frameSize = 0;
            for (int i = 0; i < FrameSizes.Length; i++)
            {
                if (_samplesQueue.EnqueuePosition >= FrameSizes[i])
                {
                    frameSize = FrameSizes[i];
                    break;
                }
            }

            if (frameSize == 0) return Span<byte>.Empty;

            int encodedSize = _opusEncoder.Encode(_samplesQueue.Data, frameSize, _encodeBuffer, _encodeBuffer.Length);
            _samplesQueue.Dequeue(frameSize);
            return _encodeBuffer.AsSpan(0, encodedSize);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _opusEncoder.Dispose();
            _disposed = true;
        }
    }
}