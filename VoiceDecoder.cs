using System;
using Concentus.Structs;

namespace ProximityChat
{
    public class VoiceDecoder
    {
        private OpusDecoder _opusDecoder;
        private short[] _decodeBuffer;

        public VoiceDecoder()
        {
            _opusDecoder = new OpusDecoder(VoiceConsts.OpusSampleRate, 1);
            _decodeBuffer = new short[2880];
        }

        public Span<short> DecodeVoiceSamples(Span<byte> encodedVoiceData)
        {
            int frameSize = OpusPacketInfo.GetNumSamples(encodedVoiceData, 0, encodedVoiceData.Length, _opusDecoder.SampleRate);
            int decodedSize = _opusDecoder.Decode(encodedVoiceData, _decodeBuffer, frameSize);
            return _decodeBuffer.AsSpan(0, decodedSize);
        }

        public Span<short> DecodeVoiceSamples(byte[] encodedVoiceData)
        {
            return DecodeVoiceSamples(encodedVoiceData.AsSpan());
        }
    }
}
