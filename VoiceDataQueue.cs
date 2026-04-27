using System;
using UnityEngine;

namespace ProximityChat
{
    public class VoiceDataQueue<T>
    {
        private T[] _buffer;
        private int _writePosition;

        public int EnqueuePosition => _writePosition;
        public T[] Data => _buffer;

        public VoiceDataQueue(int defaultLength)
        {
            _writePosition = 0;
            _buffer = new T[defaultLength];
        }

        public void Enqueue(Span<T> voiceData)
        {
            ResizeIfNeeded(voiceData.Length);
            voiceData.CopyTo(new Span<T>(_buffer).Slice(_writePosition, voiceData.Length));
            _writePosition += voiceData.Length;
        }

        public void Dequeue(int count)
        {
            if (count <= 0) return;
            if (count > _writePosition)
                throw new ArgumentOutOfRangeException(nameof(count), "Attempted to dequeue more data than exists.");

            if (count != _writePosition)
            {
                Array.Copy(_buffer, count, _buffer, 0, _writePosition - count);
                Array.Clear(_buffer, _writePosition - count, count);
            }
            else
            {
                Array.Clear(_buffer, 0, _writePosition);
            }

            _writePosition -= count;
        }

        public void ModifyWritePosition(int delta)
        {
            int next = _writePosition + delta;
            if (next < 0 || next > _buffer.Length)
                throw new IndexOutOfRangeException("Write position modified to an illegal position.");
            _writePosition = next;
        }

        private void ResizeIfNeeded(int additionalCount)
        {
            if (_buffer.Length - _writePosition >= additionalCount) return;
            Array.Resize(ref _buffer, Mathf.Max(_buffer.Length * 2, _writePosition + additionalCount));
        }
    }
}