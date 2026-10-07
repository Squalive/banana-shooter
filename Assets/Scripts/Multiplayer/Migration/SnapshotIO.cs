using System;
using System.IO;
using System.Text;
using Manager;
using UnityEngine;

namespace Multiplayer.Migration
{
    public sealed class SnapshotWriter : IDisposable
    {
        private readonly MemoryStream _stream = new();
        private readonly BinaryWriter _writer;

        public SnapshotWriter()
        {
            _writer = new BinaryWriter(_stream, Encoding.UTF8, true);
        }

        public byte[] ToArray()
        {
            _writer.Flush();
            return _stream.ToArray();
        }

        public void Dispose()
        {
            _writer.Dispose();
            _stream.Dispose();
        }

        public void Bool(bool v) => _writer.Write(v);
        public void Byte(byte v) => _writer.Write(v);
        public void Short(short v) => _writer.Write(v);
        public void UShort(ushort v) => _writer.Write(v);
        public void Int(int v) => _writer.Write(v);
        public void UInt(uint v) => _writer.Write(v);
        public void ULong(ulong v) => _writer.Write(v);
        public void Float(float v) => _writer.Write(v);
        public void String(string v) => _writer.Write(v ?? string.Empty);

        public void Vector3(Vector3 v)
        {
            _writer.Write(v.x);
            _writer.Write(v.y);
            _writer.Write(v.z);
        }

        public void Quaternion(Quaternion v)
        {
            _writer.Write(v.x);
            _writer.Write(v.y);
            _writer.Write(v.z);
            _writer.Write(v.w);
        }

        public void Color(Color v)
        {
            _writer.Write(v.r);
            _writer.Write(v.g);
            _writer.Write(v.b);
            _writer.Write(v.a);
        }

        public void Shorts(short[] v)
        {
            _writer.Write(v?.Length ?? -1);
            if (v == null) return;
            foreach (var s in v) _writer.Write(s);
        }

        public void UShorts(ushort[] v)
        {
            _writer.Write(v?.Length ?? -1);
            if (v == null) return;
            foreach (var s in v) _writer.Write(s);
        }

        public void ULongs(ulong[] v)
        {
            _writer.Write(v?.Length ?? -1);
            if (v == null) return;
            foreach (var s in v) _writer.Write(s);
        }

        public void Ints(int[] v)
        {
            _writer.Write(v?.Length ?? -1);
            if (v == null) return;
            foreach (var s in v) _writer.Write(s);
        }

        public void Bytes(byte[] v)
        {
            _writer.Write(v?.Length ?? -1);
            if (v == null) return;
            _writer.Write(v);
        }

        public void Cosmetics(InventoryManager.CosmeticIndex c)
        {
            Bool(c != null);
            if (c == null) return;
            Ints(new[] { c.hatIndex, c.faceIndex, c.shoesIndex, c.hairIndex, c.clothesIndex, c.pantIndex });
            Ints(new[] { c.hatParticle, c.faceParticle, c.shoesParticle, c.hairParticle, c.clothesParticle, c.pantParticle });
            foreach (var color in new[] { c.hatColor, c.faceColor, c.shoesColor, c.hairColor, c.clothesColor, c.pantColor })
                Color(color);
            foreach (var shiny in new[] { c.hatShiny, c.faceShiny, c.shoesShiny, c.hairShiny, c.clothesShiny, c.pantShiny })
                Float(shiny);
            Int(c.musicBoxIndex);
            Int(c.menuSceneIndex);
            ULongs(c.ids);
            ULongs(c.weaponIds);
            UShorts(c.weaponIndex);
        }
    }

    public sealed class SnapshotReader
    {
        private readonly BinaryReader _reader;

        public SnapshotReader(byte[] data)
        {
            _reader = new BinaryReader(new MemoryStream(data, false), Encoding.UTF8);
        }

        public bool Bool() => _reader.ReadBoolean();
        public byte Byte() => _reader.ReadByte();
        public short Short() => _reader.ReadInt16();
        public ushort UShort() => _reader.ReadUInt16();
        public int Int() => _reader.ReadInt32();
        public uint UInt() => _reader.ReadUInt32();
        public ulong ULong() => _reader.ReadUInt64();
        public float Float() => _reader.ReadSingle();
        public string String() => _reader.ReadString();

        public Vector3 Vector3() => new(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

        public Quaternion Quaternion() => new(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

        public Color Color() => new(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

        public short[] Shorts()
        {
            int n = _reader.ReadInt32();
            if (n < 0) return null;
            var v = new short[n];
            for (int i = 0; i < n; i++) v[i] = _reader.ReadInt16();
            return v;
        }

        public ushort[] UShorts()
        {
            int n = _reader.ReadInt32();
            if (n < 0) return null;
            var v = new ushort[n];
            for (int i = 0; i < n; i++) v[i] = _reader.ReadUInt16();
            return v;
        }

        public ulong[] ULongs()
        {
            int n = _reader.ReadInt32();
            if (n < 0) return null;
            var v = new ulong[n];
            for (int i = 0; i < n; i++) v[i] = _reader.ReadUInt64();
            return v;
        }

        public int[] Ints()
        {
            int n = _reader.ReadInt32();
            if (n < 0) return null;
            var v = new int[n];
            for (int i = 0; i < n; i++) v[i] = _reader.ReadInt32();
            return v;
        }

        public byte[] Bytes()
        {
            int n = _reader.ReadInt32();
            return n < 0 ? null : _reader.ReadBytes(n);
        }

        public InventoryManager.CosmeticIndex Cosmetics()
        {
            if (!Bool()) return null;
            var c = new InventoryManager.CosmeticIndex();
            var idx = Ints();
            c.hatIndex = idx[0]; c.faceIndex = idx[1]; c.shoesIndex = idx[2]; c.hairIndex = idx[3]; c.clothesIndex = idx[4]; c.pantIndex = idx[5];
            var part = Ints();
            c.hatParticle = part[0]; c.faceParticle = part[1]; c.shoesParticle = part[2]; c.hairParticle = part[3]; c.clothesParticle = part[4]; c.pantParticle = part[5];
            c.hatColor = Color(); c.faceColor = Color(); c.shoesColor = Color(); c.hairColor = Color(); c.clothesColor = Color(); c.pantColor = Color();
            c.hatShiny = Float(); c.faceShiny = Float(); c.shoesShiny = Float(); c.hairShiny = Float(); c.clothesShiny = Float(); c.pantShiny = Float();
            c.musicBoxIndex = Int();
            c.menuSceneIndex = Int();
            c.ids = ULongs();
            c.weaponIds = ULongs();
            c.weaponIndex = UShorts();
            return c;
        }
    }
}
