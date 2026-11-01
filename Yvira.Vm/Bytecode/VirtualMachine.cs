using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Yvira.Vm.Bytecode;

public class VirtualMachine
{
    private object?[] _stack = new object?[4096];
    private int _sp = 0; // Stack pointer
    private readonly CallFrame[] _frames = new CallFrame[512];
    private int _frameCount = 0;

    private readonly Dictionary<string, object?> _globals = new(StringComparer.OrdinalIgnoreCase);
    private readonly Action<string> _outputHandler;

    public VirtualMachine(Action<string>? outputHandler = null)
    {
        _outputHandler = outputHandler ?? Console.WriteLine;
    }

    public object? Execute(CompiledFunction mainFunction)
    {
        _sp = 0;
        _frameCount = 0;

        Push(mainFunction);
        _frames[_frameCount++] = new CallFrame(mainFunction, 0, 0);

        return Run();
    }

    public object? Execute(Chunk chunk)
    {
        var fn = new CompiledFunction("<script>", 0);
        foreach (var b in chunk.Code) fn.Chunk.Code.Add(b);
        foreach (var c in chunk.Constants) fn.Chunk.Constants.Add(c);
        return Execute(fn);
    }

    private object? Run()
    {
        var frames = _frames;
        var stack = _stack;
        int sp = _sp;

        while (_frameCount > 0)
        {
            var frame = frames[_frameCount - 1];
            var code = frame.Code;
            var constants = frame.Constants;
            int codeLen = code.Length;
            int ip = frame.Ip;
            int stackOffset = frame.StackOffset;

            while (ip < codeLen)
            {
                var instruction = (OpCode)code[ip++];

                switch (instruction)
                {
                    case OpCode.OpConstant:
                    {
                        int constIdx = (code[ip] << 8) | code[ip + 1];
                        ip += 2;
                        if (sp >= stack.Length) { _sp = sp; Push(constants[constIdx]); sp = _sp; stack = _stack; }
                        else { stack[sp++] = constants[constIdx]; }
                        break;
                    }

                    case OpCode.OpNull:
                        if (sp >= stack.Length) { _sp = sp; Push(null); sp = _sp; stack = _stack; }
                        else { stack[sp++] = null; }
                        break;

                    case OpCode.OpTrue:
                        if (sp >= stack.Length) { _sp = sp; Push(true); sp = _sp; stack = _stack; }
                        else { stack[sp++] = true; }
                        break;

                    case OpCode.OpFalse:
                        if (sp >= stack.Length) { _sp = sp; Push(false); sp = _sp; stack = _stack; }
                        else { stack[sp++] = false; }
                        break;

                    case OpCode.OpPop:
                        if (sp > stackOffset) sp--;
                        break;

                    // Aritmética Rápida (Fast Path para Double / Primitivos)
                    case OpCode.OpAdd:
                    {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        if (a is double da && b is double db)
                        {
                            stack[sp++] = da + db;
                        }
                        else if (a is int ia && b is int ib)
                        {
                            stack[sp++] = (double)(ia + ib);
                        }
                        else if (a is YviraMatrix ma && b is YviraMatrix mb)
                        {
                            stack[sp++] = ma + mb;
                        }
                        else if (a is YviraMatrix ma2)
                        {
                            stack[sp++] = ma2 + Convert.ToDouble(b);
                        }
                        else if (b is YviraMatrix mb2)
                        {
                            stack[sp++] = mb2 + Convert.ToDouble(a);
                        }
                        else if (a is string || b is string)
                        {
                            stack[sp++] = a?.ToString() + b?.ToString();
                        }
                        else
                        {
                            stack[sp++] = Convert.ToDouble(a) + Convert.ToDouble(b);
                        }
                        break;
                    }

                    case OpCode.OpSubtract:
                    {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        if (a is double da && b is double db)
                        {
                            stack[sp++] = da - db;
                        }
                        else if (a is int ia && b is int ib)
                        {
                            stack[sp++] = (double)(ia - ib);
                        }
                        else if (a is YviraMatrix ma && b is YviraMatrix mb)
                        {
                            stack[sp++] = ma - (object)mb;
                        }
                        else if (a is YviraMatrix ma2)
                        {
                            stack[sp++] = ma2 - (object)Convert.ToDouble(b);
                        }
                        else
                        {
                            stack[sp++] = Convert.ToDouble(a) - Convert.ToDouble(b);
                        }
                        break;
                    }

                    case OpCode.OpMultiply:
                    {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        if (a is double da && b is double db)
                        {
                            stack[sp++] = da * db;
                        }
                        else if (a is int ia && b is int ib)
                        {
                            stack[sp++] = (double)(ia * ib);
                        }
                        else if (a is YviraMatrix ma && b is YviraMatrix mb)
                        {
                            stack[sp++] = ma * mb;
                        }
                        else if (a is YviraMatrix ma2)
                        {
                            stack[sp++] = ma2 * Convert.ToDouble(b);
                        }
                        else if (b is YviraMatrix mb2)
                        {
                            stack[sp++] = mb2 * Convert.ToDouble(a);
                        }
                        else
                        {
                            stack[sp++] = Convert.ToDouble(a) * Convert.ToDouble(b);
                        }
                        break;
                    }

                    case OpCode.OpDivide:
                    {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        if (a is double da && b is double db)
                        {
                            stack[sp++] = da / db;
                        }
                        else
                        {
                            stack[sp++] = Convert.ToDouble(a) / Convert.ToDouble(b);
                        }
                        break;
                    }

                    case OpCode.OpModulo:
                    {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        if (a is double da && b is double db)
                        {
                            stack[sp++] = da % db;
                        }
                        else
                        {
                            stack[sp++] = Convert.ToDouble(a) % Convert.ToDouble(b);
                        }
                        break;
                    }

                    case OpCode.OpExponent:
                    {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        stack[sp++] = Math.Pow(Convert.ToDouble(a), Convert.ToDouble(b));
                        break;
                    }

                    case OpCode.OpNegate:
                    {
                        var a = stack[--sp];
                        if (a is double da) stack[sp++] = -da;
                        else stack[sp++] = -Convert.ToDouble(a);
                        break;
                    }

                    case OpCode.OpNot:
                    {
                        var a = stack[--sp];
                        stack[sp++] = !IsTruthy(a);
                        break;
                    }

                    // Comparações Rápidas
                    case OpCode.OpEqual:
                    {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        if (a is double da && b is double db) stack[sp++] = da == db;
                        else stack[sp++] = Equals(a, b);
                        break;
                    }

                    case OpCode.OpNotEqual:
                    {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        if (a is double da && b is double db) stack[sp++] = da != db;
                        else stack[sp++] = !Equals(a, b);
                        break;
                    }

                    case OpCode.OpGreater:
                    {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        if (a is double da && b is double db) stack[sp++] = da > db;
                        else stack[sp++] = Convert.ToDouble(a) > Convert.ToDouble(b);
                        break;
                    }

                    case OpCode.OpGreaterEqual:
                    {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        if (a is double da && b is double db) stack[sp++] = da >= db;
                        else stack[sp++] = Convert.ToDouble(a) >= Convert.ToDouble(b);
                        break;
                    }

                    case OpCode.OpLess:
                    {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        if (a is double da && b is double db) stack[sp++] = da < db;
                        else stack[sp++] = Convert.ToDouble(a) < Convert.ToDouble(b);
                        break;
                    }

                    case OpCode.OpLessEqual:
                    {
                        var b = stack[--sp];
                        var a = stack[--sp];
                        if (a is double da && b is double db) stack[sp++] = da <= db;
                        else stack[sp++] = Convert.ToDouble(a) <= Convert.ToDouble(b);
                        break;
                    }

                    // Variáveis Globais
                    case OpCode.OpDefineGlobal:
                    {
                        int nameIdx = (code[ip] << 8) | code[ip + 1];
                        ip += 2;
                        string name = (string)constants[nameIdx]!;
                        _globals[name] = stack[--sp];
                        break;
                    }

                    case OpCode.OpGetGlobal:
                    {
                        int nameIdx = (code[ip] << 8) | code[ip + 1];
                        ip += 2;
                        string name = (string)constants[nameIdx]!;
                        if (_globals.TryGetValue(name, out var val))
                        {
                            if (sp >= stack.Length) { _sp = sp; Push(val); sp = _sp; stack = _stack; }
                            else { stack[sp++] = val; }
                        }
                        else
                        {
                            throw new Exception($"Variável global indefinida: '{name}'");
                        }
                        break;
                    }

                    case OpCode.OpSetGlobal:
                    {
                        int nameIdx = (code[ip] << 8) | code[ip + 1];
                        ip += 2;
                        string name = (string)constants[nameIdx]!;
                        _globals[name] = stack[sp - 1];
                        break;
                    }

                    // Variáveis Locais com Offset do Call Frame
                    case OpCode.OpGetLocal:
                    {
                        int slot = (code[ip] << 8) | code[ip + 1];
                        ip += 2;
                        var val = stack[stackOffset + slot];
                        if (sp >= stack.Length) { _sp = sp; Push(val); sp = _sp; stack = _stack; }
                        else { stack[sp++] = val; }
                        break;
                    }

                    case OpCode.OpSetLocal:
                    {
                        int slot = (code[ip] << 8) | code[ip + 1];
                        ip += 2;
                        stack[stackOffset + slot] = stack[sp - 1];
                        break;
                    }

                    // Saltos e Loops
                    case OpCode.OpJump:
                    {
                        int offset = (code[ip] << 8) | code[ip + 1];
                        ip += 2 + offset;
                        break;
                    }

                    case OpCode.OpJumpIfFalse:
                    {
                        int offset = (code[ip] << 8) | code[ip + 1];
                        ip += 2;
                        var cond = stack[--sp];
                        if (!IsTruthy(cond))
                        {
                            ip += offset;
                        }
                        break;
                    }

                    case OpCode.OpLoop:
                    {
                        int offset = (code[ip] << 8) | code[ip + 1];
                        ip = ip + 2 - offset;
                        break;
                    }

                    // Chamada de Funções (OpCall)
                    case OpCode.OpCall:
                    {
                        int argCount = (code[ip] << 8) | code[ip + 1];
                        ip += 2;
                        var callee = stack[sp - 1 - argCount];

                        if (callee is CompiledFunction targetFn)
                        {
                            if (argCount != targetFn.Arity)
                            {
                                throw new Exception($"Função '{targetFn.Name}' esperava {targetFn.Arity} argumentos, mas recebeu {argCount}.");
                            }

                            if (_frameCount >= frames.Length)
                            {
                                throw new StackOverflowException("Estouro de pilha de chamadas (Stack Overflow) na VM Yvira.");
                            }

                            frame.Ip = ip;
                            int newStackOffset = sp - argCount - 1;
                            frames[_frameCount++] = new CallFrame(targetFn, 0, newStackOffset);
                            goto next_frame;
                        }
                        else
                        {
                            throw new Exception($"Tentativa de invocar um objeto não invocável: '{callee}'");
                        }
                    }

                    // Retorno de Função (OpReturn)
                    case OpCode.OpReturn:
                    {
                        var result = stack[--sp];
                        _frameCount--;

                        if (_frameCount == 0)
                        {
                            _sp = 0;
                            return result;
                        }

                        sp = stackOffset;
                        stack[sp++] = result;
                        goto next_frame;
                    }

                    // Impressão Otimizada
                    case OpCode.OpPrint:
                    {
                        int argCount = (code[ip] << 8) | code[ip + 1];
                        ip += 2;
                        var args = new object?[argCount];
                        for (int i = argCount - 1; i >= 0; i--)
                        {
                            args[i] = stack[--sp];
                        }
                        _outputHandler(string.Join(" ", args));
                        break;
                    }

                    // Matrizes e Coleções
                    case OpCode.OpMatrix:
                    {
                        int rows = (code[ip] << 8) | code[ip + 1];
                        int cols = (code[ip + 2] << 8) | code[ip + 3];
                        ip += 4;

                        var mat = new YviraMatrix(rows, cols);
                        int totalElements = rows * cols;
                        for (int i = totalElements - 1; i >= 0; i--)
                        {
                            int r = i / cols;
                            int c = i % cols;
                            mat.Set(r, c, Convert.ToDouble(stack[--sp]));
                        }
                        if (sp >= stack.Length) { _sp = sp; Push(mat); sp = _sp; stack = _stack; }
                        else { stack[sp++] = mat; }
                        break;
                    }

                    case OpCode.OpArray:
                    {
                        int count = (code[ip] << 8) | code[ip + 1];
                        ip += 2;
                        var elements = new object?[count];
                        for (int i = count - 1; i >= 0; i--)
                        {
                            elements[i] = stack[--sp];
                        }
                        var list = new YviraList(elements);
                        if (sp >= stack.Length) { _sp = sp; Push(list); sp = _sp; stack = _stack; }
                        else { stack[sp++] = list; }
                        break;
                    }

                    case OpCode.OpGetIndex:
                    {
                        int dimensions = (code[ip] << 8) | code[ip + 1];
                        ip += 2;

                        if (dimensions == 2)
                        {
                            int colIdx = Convert.ToInt32(stack[--sp]);
                            int rowIdx = Convert.ToInt32(stack[--sp]);
                            var target = stack[--sp];

                            if (target is YviraMatrix mat)
                            {
                                double val = mat.Get(rowIdx, colIdx);
                                if (sp >= stack.Length) { _sp = sp; Push(val); sp = _sp; stack = _stack; }
                                else { stack[sp++] = val; }
                            }
                            else
                            {
                                throw new Exception($"Apenas matrizes suportam indexação bidimensional [r, c]. Objeto era: {target?.GetType().Name}");
                            }
                        }
                        else
                        {
                            int idx = Convert.ToInt32(stack[--sp]);
                            var target = stack[--sp];
                            object? res;
                            if (target is YviraMatrix m) res = m.Rows == 1 ? m.Get(0, idx) : (m.Cols == 1 ? m.Get(idx, 0) : m.GetRow(idx));
                            else if (target is YviraList l) res = l.Get(idx);
                            else if (target is string s) res = s[idx].ToString();
                            else throw new Exception($"Tipo não suporta indexação: {target?.GetType().Name}");

                            if (sp >= stack.Length) { _sp = sp; Push(res); sp = _sp; stack = _stack; }
                            else { stack[sp++] = res; }
                        }
                        break;
                    }

                    case OpCode.OpInvoke:
                    {
                        int nameConst = (code[ip] << 8) | code[ip + 1];
                        int argCount = (code[ip + 2] << 8) | code[ip + 3];
                        ip += 4;
                        string memberName = (string)constants[nameConst]!;

                        var args = new object?[argCount];
                        for (int i = argCount - 1; i >= 0; i--) args[i] = stack[--sp];
                        var target = stack[--sp];

                        if (target is YviraMatrix mat)
                        {
                            object? result = memberName switch
                            {
                                "T" or "transpose" => mat.Transpose(),
                                "det" or "determinant" => mat.Determinant(),
                                "inverse" or "inv" => mat.Inverse(),
                                "trace" => mat.Trace(),
                                "sum" => mat.Sum(),
                                "mean" => mat.Mean(),
                                "max" => mat.Max(),
                                "min" => mat.Min(),
                                "rows" => (double)mat.Rows,
                                "cols" => (double)mat.Cols,
                                "get" when argCount == 1 => (mat.Rows == 1 ? mat.Get(0, Convert.ToInt32(args[0])) : mat.GetRow(Convert.ToInt32(args[0]))),
                                "get" when argCount == 2 => mat.Get(Convert.ToInt32(args[0]), Convert.ToInt32(args[1])),
                                _ => throw new Exception($"Método de Matriz desconhecido ou inválido: '{memberName}'")
                            };
                            if (sp >= stack.Length) { _sp = sp; Push(result); sp = _sp; stack = _stack; }
                            else { stack[sp++] = result; }
                        }
                        else if (target is YviraList list)
                        {
                            object? result = memberName switch
                            {
                                "length" or "len" or "count" => (double)list.Count,
                                "add" or "push" => CallListAdd(list, args),
                                _ => throw new Exception($"Método de Lista desconhecido: '{memberName}'")
                            };
                            if (sp >= stack.Length) { _sp = sp; Push(result); sp = _sp; stack = _stack; }
                            else { stack[sp++] = result; }
                        }
                        else
                        {
                            throw new Exception($"Objeto '{target?.GetType().Name}' não possui membro '{memberName}'.");
                        }
                        break;
                    }

                    default:
                        throw new NotImplementedException($"Opcode não implementado na VM: {instruction}");
                }
            }

            frame.Ip = ip;
            _frameCount--;

        next_frame:
            continue;
        }

        _sp = sp;
        return null;
    }

    private static object? CallListAdd(YviraList list, object?[] args)
    {
        if (args.Length > 0) list.Add(args[0]);
        return null;
    }

    private void Push(object? val)
    {
        if (_sp >= _stack.Length)
        {
            Array.Resize(ref _stack, _stack.Length * 2);
        }
        _stack[_sp++] = val;
    }

    private static bool IsTruthy(object? value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is double d) return d != 0.0;
        if (value is string s) return s.Length > 0;
        return true;
    }
}
