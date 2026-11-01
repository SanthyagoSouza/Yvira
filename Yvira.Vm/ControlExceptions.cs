namespace Yvira.Vm;

public class BreakException : System.Exception { }
public class ContinueException : System.Exception { }
public class YviraRuntimeException : System.Exception
{
    public object? Value { get; }
    public YviraRuntimeException(object? value)
        : base(value?.ToString() ?? "Erro em tempo de execução.")
    {
        Value = value;
    }
}
