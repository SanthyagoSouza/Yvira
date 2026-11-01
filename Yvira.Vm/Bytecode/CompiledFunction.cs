using System.Collections.Generic;

namespace Yvira.Vm.Bytecode;

public class CompiledFunction
{
    public string Name { get; }
    public int Arity { get; set; }
    public Chunk Chunk { get; }

    public CompiledFunction(string name, int arity = 0)
    {
        Name = name;
        Arity = arity;
        Chunk = new Chunk();
    }

    public override string ToString() => $"<fn {Name}>";
}

public class CallFrame
{
    public CompiledFunction Function { get; set; }
    public byte[] Code { get; set; }
    public object?[] Constants { get; set; }
    public int Ip;
    public int StackOffset;

    public CallFrame(CompiledFunction function, int ip, int stackOffset)
    {
        Function = function;
        Code = function.Chunk.Code.ToArray();
        Constants = function.Chunk.Constants.ToArray();
        Ip = ip;
        StackOffset = stackOffset;
    }
}
