using System;
using Yvira.Ast;

namespace Yvira.Vm;

// Exceção especial de controle de fluxo usada para propagar o 'return' através da pilha de execução
public class ReturnException : Exception
{
    public object? Value { get; }

    public ReturnException(object? value)
    {
        Value = value;
    }
}

// Representa uma função do usuário Yvira em memória
public class YviraFunction
{
    public FunctionDeclarationNode Declaration { get; }
    public Environment Closure { get; }

    public YviraFunction(FunctionDeclarationNode declaration, Environment closure)
    {
        Declaration = declaration;
        Closure = closure;
    }

    public YviraFunction Bind(YviraInstance instance)
    {
        var env = new Environment(Closure);
        env.Define("this", instance);
        return new YviraFunction(Declaration, env);
    }

    public object? Call(Interpreter interpreter, System.Collections.Generic.IReadOnlyList<object?> arguments)
    {
        var functionEnv = new Environment(Closure);

        for (int i = 0; i < Declaration.Parameters.Count; i++)
        {
            var paramName = Declaration.Parameters[i].Name;
            object? val = i < arguments.Count ? arguments[i] : null;
            functionEnv.Define(paramName, val);
        }

        try
        {
            interpreter.ExecuteBlockWithEnv(Declaration.Body, functionEnv);
        }
        catch (ReturnException ret)
        {
            return ret.Value;
        }

        return null;
    }
}