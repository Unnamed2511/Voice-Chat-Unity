using System;
using Concentus.Structs;

namespace ProximityChat
{
    public sealed class VoiceDecoder : IDisposable
    {
        private readonly OpusDecoder _opusDecoder;
        private readonly short[] _decodeBuffer;
        private bool _disposed;

        public VoiceDecoder()
        {
            _opusDecoder = new OpusDecoder(VoiceConsts.OpusSampleRate, 1);
            _decodeBuffer = new short[VoiceConsts.MaxOpusFrameSize];
        }

        public Span<short> DecodeVoiceSamples(Span<byte> encodedData)
        {
            int frameSize = OpusPacketInfo.GetNumSamples(encodedData, 0, encodedData.Length, _opusDecoder.SampleRate);
            int decodedCount = _opusDecoder.Decode(encodedData, _decodeBuffer, frameSize);
            return _decodeBuffer.AsSpan(0, decodedCount);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _opusDecoder.Dispose();
            _disposed = true;
        }
    }
}