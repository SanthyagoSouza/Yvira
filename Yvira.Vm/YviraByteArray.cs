using System;
using System.Collections.Generic;
using System.Text;

namespace Yvira.Vm;

public class YviraByteArray
{
    private readonly List<byte> _bytes;

    public int Length => _bytes.Count;

    public YviraByteArray()
    {
        _bytes = new List<byte>();
    }

    public YviraByteArray(int capacity)
    {
        _bytes = new List<byte>(capacity);
    }

    public YviraByteArray(byte[] initial)
    {
        _bytes = new List<byte>(initial);
    }

    public YviraByteArray(IEnumerable<byte> initial)
    {
        _bytes = new List<byte>(initial);
    }

    public byte[] ToByteArray() => _bytes.ToArray();

    public byte Get(int index)
    {
        if (index < 0) index = _bytes.Count + index;
        if (index < 0 || index >= _bytes.Count)
            throw new IndexOutOfRangeException($"Índice fora dos limites do ByteArray: {index} (tamanho: {_bytes.Count})");
        return _bytes[index];
    }

    public void Set(int index, byte value)
    {
        if (index < 0) index = _bytes.Count + index;
        if (index < 0 || index >= _bytes.Count)
            throw new IndexOutOfRangeException($"Índice fora dos limites do ByteArray: {index} (tamanho: {_bytes.Count})");
        _bytes[index] = value;
    }

    public void WriteU8(byte value)
    {
        _bytes.Add(value);
    }

    public void WriteI8(sbyte value)
    {
        _bytes.Add((byte)value);
    }

    public void WriteU16(ushort value, bool littleEndian = true)
    {
        if (littleEndian)
        {
            _bytes.Add((byte)(value & 0xFF));
            _bytes.Add((byte)((value >> 8) & 0xFF));
        }
        else
        {
            _bytes.Add((byte)((value >> 8) & 0xFF));
            _bytes.Add((byte)(value & 0xFF));
        }
    }

    public void WriteI16(short value, bool littleEndian = true)
    {
        WriteU16((ushort)value, littleEndian);
    }

    public void WriteU32(uint value, bool littleEndian = true)
    {
        if (littleEndian)
        {
            _bytes.Add((byte)(value & 0xFF));
            _bytes.Add((byte)((value >> 8) & 0xFF));
            _bytes.Add((byte)((value >> 16) & 0xFF));
            _bytes.Add((byte)((value >> 24) & 0xFF));
        }
        else
        {
            _bytes.Add((byte)((value >> 24) & 0xFF));
            _bytes.Add((byte)((value >> 16) & 0xFF));
            _bytes.Add((byte)((value >> 8) & 0xFF));
            _bytes.Add((byte)(value & 0xFF));
        }
    }

    public void WriteI32(int value, bool littleEndian = true)
    {
        WriteU32((uint)value, littleEndian);
    }

    public void WriteF64(double value, bool littleEndian = true)
    {
        byte[] raw = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian != littleEndian)
        {
            Array.Reverse(raw);
        }
        _bytes.AddRange(raw);
    }

    public void WriteString(string value, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        byte[] encoded = encoding.GetBytes(value);
        _bytes.AddRange(encoded);
    }

    public void WriteBytes(byte[] data)
    {
        _bytes.AddRange(data);
    }

    public byte ReadU8(int index) => Get(index);

    public ushort ReadU16(int index, bool littleEndian = true)
    {
        byte b0 = Get(index);
        byte b1 = Get(index + 1);
        return littleEndian ? (ushort)(b0 | (b1 << 8)) : (ushort)((b0 << 8) | b1);
    }

    public uint ReadU32(int index, bool littleEndian = true)
    {
        byte b0 = Get(index);
        byte b1 = Get(index + 1);
        byte b2 = Get(index + 2);
        byte b3 = Get(index + 3);
        return littleEndian
            ? (uint)(b0 | (b1 << 8) | (b2 << 16) | (b3 << 24))
            : (uint)((b0 << 24) | (b1 << 16) | (b2 << 8) | b3);
    }

    public double ReadF64(int index, bool littleEndian = true)
    {
        byte[] buf = new byte[8];
        for (int i = 0; i < 8; i++) buf[i] = Get(index + i);
        if (BitConverter.IsLittleEndian != littleEndian)
        {
            Array.Reverse(buf);
        }
        return BitConverter.ToDouble(buf, 0);
    }

    public string ReadString(int index, int length, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        byte[] buf = new byte[length];
        for (int i = 0; i < length; i++) buf[i] = Get(index + i);
        return encoding.GetString(buf);
    }

    public YviraByteArray Slice(int start, int length)
    {
        if (start < 0) start = Math.Max(0, _bytes.Count + start);
        if (start > _bytes.Count) return new YviraByteArray();
        if (start + length > _bytes.Count) length = _bytes.Count - start;
        length = Math.Max(0, length);

        var sub = new byte[length];
        _bytes.CopyTo(start, sub, 0, length);
        return new YviraByteArray(sub);
    }

    public void Clear() => _bytes.Clear();

    public override string ToString()
    {
        return $"<ByteArray length={_bytes.Count}>";
    }
}
