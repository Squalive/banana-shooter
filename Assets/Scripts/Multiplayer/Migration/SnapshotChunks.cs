using System;
using System.Collections.Generic;

namespace Multiplayer.Migration
{
    public static class SnapshotChunks
    {
        public static List<byte[]> Split(byte[] data, int size)
        {
            var parts = new List<byte[]>();
            for (int offset = 0; offset < data.Length || parts.Count == 0; offset += size)
            {
                int len = Math.Min(size, data.Length - offset);
                var part = new byte[Math.Max(len, 0)];
                if (len > 0) Buffer.BlockCopy(data, offset, part, 0, len);
                parts.Add(part);
            }
            return parts;
        }
    }

    public sealed class SnapshotAssembler
    {
        private uint _seq;
        private byte[][] _parts;
        private int _received;

        public byte[] Add(uint seq, int index, int count, byte[] chunk)
        {
            if (count <= 0 || index < 0 || index >= count || chunk == null) return null;
            if (_parts != null && seq < _seq) return null;

            if (_parts == null || seq != _seq || _parts.Length != count)
            {
                if (_parts == null && seq <= _seq && _seq != 0) return null;
                _seq = seq;
                _parts = new byte[count][];
                _received = 0;
            }

            if (_parts[index] != null) return null;
            _parts[index] = chunk;
            if (++_received < count) return null;

            int total = 0;
            foreach (var p in _parts) total += p.Length;
            var data = new byte[total];
            int offset = 0;
            foreach (var p in _parts)
            {
                Buffer.BlockCopy(p, 0, data, offset, p.Length);
                offset += p.Length;
            }
            _parts = null;
            return data;
        }
    }
}
