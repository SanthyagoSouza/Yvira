using System;
using System.Collections.Generic;

namespace Yvira.Vm.Bytecode;

public class Chunk
{
    public List<byte> Code { get; } = new();
    public List<object?> Constants { get; } = new();
    public List<int> Lines { get; } = new();

    public void Write(byte b, int line = 1)
    {
        Code.Add(b);
        Lines.Add(line);
    }

    public void WriteOp(OpCode op, int line = 1)
    {
        Write((byte)op, line);
    }

    public int AddConstant(object? value)
    {
        Constants.Add(value);
        return Constants.Count - 1;
    }

    public void WriteConstant(object? value, int line = 1)
    {
        int constantIndex = AddConstant(value);
        WriteOp(OpCode.OpConstant, line);
        // Suporta índices de constante de 2 bytes (até 65.535 constantes por chunk)
        Write((byte)((constantIndex >> 8) & 0xFF), line);
        Write((byte)(constantIndex & 0xFF), line);
    }
}