import os

path = r"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\Yvira.Ast\AstNodes.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

target = "public class ProgramNode : AstNode"
addition = """// Superpoder @realtime: @realtime.servidor(...) ou @realtime.cliente(...)
public class RealtimeExpressionNode : ExpressionNode
{
    public string Kind { get; } // "servidor" ou "cliente"
    public IReadOnlyList<ExpressionNode> Arguments { get; }

    public RealtimeExpressionNode(string kind, IReadOnlyList<ExpressionNode> arguments)
    {
        Kind = kind;
        Arguments = arguments;
    }
}

public class ProgramNode : AstNode"""

if target in c:
    c = c.replace(target, addition)
    with open(path, "w", encoding="utf-8") as f:
        f.write(c)
    print("AST_PATCHED_OK")
else:
    print("AST_TARGET_NOT_FOUND")
