using System;
using System.Collections.Generic;
using Yvira.Ast;
using Yvira.Lexer;

namespace Yvira.Vm.Bytecode;

public class Compiler
{
    private class CompilerContext
    {
        public CompiledFunction Function { get; }
        public List<string> Locals { get; } = new();
        public int ScopeDepth { get; set; } = 0;
        public CompilerContext? Enclosing { get; }

        public CompilerContext(CompiledFunction function, CompilerContext? enclosing = null)
        {
            Function = function;
            Enclosing = enclosing;
            // Reserva o slot 0 da pilha (referência à própria função ou this)
            Locals.Add(function.Name);
        }
    }

    private CompilerContext _current;

    public Compiler()
    {
        var topLevelFn = new CompiledFunction("<script>", 0);
        _current = new CompilerContext(topLevelFn);
    }

    private Chunk CurrentChunk => _current.Function.Chunk;

    public CompiledFunction Compile(ProgramNode program)
    {
        foreach (var stmt in program.Statements)
        {
            CompileStatement(stmt);
        }
        CurrentChunk.WriteOp(OpCode.OpNull);
        CurrentChunk.WriteOp(OpCode.OpReturn);
        return _current.Function;
    }

    private void CompileStatement(StatementNode stmt)
    {
        switch (stmt)
        {
            case FunctionDeclarationNode funcDecl:
                CompileFunctionDeclaration(funcDecl);
                break;

            case ReturnStatementNode retStmt:
                CompileReturn(retStmt);
                break;

            case VariableDeclarationNode varDecl:
                CompileVarDecl(varDecl);
                break;

            case ExpressionStatementNode exprStmt:
                // Se for chamada de print, já consome seus argumentos e não deixa valor residual
                if (exprStmt.Expression is CallExpressionNode call && (call.Callee.Name == "print" || call.Callee.Name == "println"))
                {
                    CompileCall(call);
                }
                else
                {
                    CompileExpression(exprStmt.Expression);
                    CurrentChunk.WriteOp(OpCode.OpPop);
                }
                break;

            case BlockStatementNode block:
                BeginScope();
                foreach (var s in block.Statements) CompileStatement(s);
                EndScope();
                break;

            case IfStatementNode ifStmt:
                CompileIf(ifStmt);
                break;

            case WhileStatementNode whileStmt:
                CompileWhile(whileStmt);
                break;

            default:
                throw new NotImplementedException($"Comando ainda não suportado no compilador de Bytecode: {stmt.GetType().Name}");
        }
    }

    private void CompileFunctionDeclaration(FunctionDeclarationNode funcDecl)
    {
        string funcName = funcDecl.Name.Name;
        int arity = funcDecl.Parameters.Count;
        var compiledFn = new CompiledFunction(funcName, arity);

        var prevContext = _current;
        _current = new CompilerContext(compiledFn, prevContext);
        BeginScope();

        // Registra os parâmetros como variáveis locais no frame da função
        foreach (var param in funcDecl.Parameters)
        {
            _current.Locals.Add(param.Name);
        }

        foreach (var s in funcDecl.Body.Statements)
        {
            CompileStatement(s);
        }

        // Garante retorno implícito de null se o fluxo alcançar o final da função
        CurrentChunk.WriteOp(OpCode.OpNull);
        CurrentChunk.WriteOp(OpCode.OpReturn);

        EndScope();
        _current = prevContext;

        // Armazena a função compilada como constante e define no escopo pai
        int constIdx = CurrentChunk.AddConstant(compiledFn);
        CurrentChunk.WriteOp(OpCode.OpConstant);
        WriteShort(constIdx);

        if (_current.ScopeDepth > 0)
        {
            _current.Locals.Add(funcName);
        }
        else
        {
            int nameConst = CurrentChunk.AddConstant(funcName);
            CurrentChunk.WriteOp(OpCode.OpDefineGlobal);
            WriteShort(nameConst);
        }
    }

    private void CompileReturn(ReturnStatementNode retStmt)
    {
        if (retStmt.Value != null)
        {
            CompileExpression(retStmt.Value);
        }
        else
        {
            CurrentChunk.WriteOp(OpCode.OpNull);
        }
        CurrentChunk.WriteOp(OpCode.OpReturn);
    }

    private void CompileVarDecl(VariableDeclarationNode varDecl)
    {
        string name = varDecl.Identifier.Name;

        if (varDecl.Initializer != null)
        {
            CompileExpression(varDecl.Initializer);
        }
        else
        {
            CurrentChunk.WriteOp(OpCode.OpNull);
        }

        if (_current.ScopeDepth > 0)
        {
            _current.Locals.Add(name);
        }
        else
        {
            int nameConst = CurrentChunk.AddConstant(name);
            CurrentChunk.WriteOp(OpCode.OpDefineGlobal);
            WriteShort(nameConst);
        }
    }

    private void CompileIf(IfStatementNode ifStmt)
    {
        CompileExpression(ifStmt.Condition);

        int thenJump = EmitJump(OpCode.OpJumpIfFalse);
        CurrentChunk.WriteOp(OpCode.OpPop); // descarta a condição

        CompileStatement(ifStmt.ThenBranch);

        int elseJump = EmitJump(OpCode.OpJump);
        PatchJump(thenJump);
        CurrentChunk.WriteOp(OpCode.OpPop); // descarta a condição no caso false

        if (ifStmt.ElseBranch != null)
        {
            CompileStatement(ifStmt.ElseBranch);
        }

        PatchJump(elseJump);
    }

    private void CompileWhile(WhileStatementNode whileStmt)
    {
        int loopStart = CurrentChunk.Code.Count;

        CompileExpression(whileStmt.Condition);
        int exitJump = EmitJump(OpCode.OpJumpIfFalse);
        CurrentChunk.WriteOp(OpCode.OpPop);

        CompileStatement(whileStmt.Body);

        EmitLoop(loopStart);

        PatchJump(exitJump);
        CurrentChunk.WriteOp(OpCode.OpPop);
    }

    private void CompileExpression(ExpressionNode expr)
    {
        switch (expr)
        {
            case NumberLiteralNode num:
                CurrentChunk.WriteConstant(num.Value);
                break;

            case StringLiteralNode str:
                CurrentChunk.WriteConstant(str.Value);
                break;

            case BooleanLiteralNode b:
                CurrentChunk.WriteOp(b.Value ? OpCode.OpTrue : OpCode.OpFalse);
                break;

            case IdentifierNode id:
                CompileIdentifier(id.Name);
                break;

            case BinaryExpressionNode bin:
                CompileBinary(bin);
                break;

            case UnaryExpressionNode un:
                CompileUnary(un);
                break;

            case AssignmentExpressionNode assign:
                CompileAssignment(assign);
                break;

            case CallExpressionNode call:
                CompileCall(call);
                break;

            case NullLiteralNode:
                CurrentChunk.WriteOp(OpCode.OpNull);
                break;

            case ArrayLiteralNode arr:
                CompileArray(arr);
                break;

            case IndexAccessNode idx:
                CompileIndexAccess(idx);
                break;

            case MemberAccessNode member:
                CompileMemberAccess(member);
                break;

            case MethodCallNode method:
                CompileMethodCall(method);
                break;

            case ParenthesizedExpressionNode paren:
                CompileExpression(paren.Expression);
                break;

            default:
                throw new NotImplementedException($"Expressão ainda não suportada no compilador de Bytecode: {expr.GetType().Name}");
        }
    }

    private void CompileArray(ArrayLiteralNode arr)
    {
        // Se for matriz 2D: [[1, 2], [3, 4]]
        if (arr.Elements.Count > 0 && arr.Elements[0] is ArrayLiteralNode firstRow)
        {
            int rows = arr.Elements.Count;
            int cols = firstRow.Elements.Count;

            // Empilha todos os elementos linearmente
            foreach (var elem in arr.Elements)
            {
                if (elem is ArrayLiteralNode row)
                {
                    foreach (var val in row.Elements)
                    {
                        CompileExpression(val);
                    }
                }
            }

            CurrentChunk.WriteOp(OpCode.OpMatrix);
            WriteShort(rows);
            WriteShort(cols);
            return;
        }

        // Vetor 1D: [1, 2, 3]
        foreach (var el in arr.Elements)
        {
            CompileExpression(el);
        }
        CurrentChunk.WriteOp(OpCode.OpArray);
        WriteShort(arr.Elements.Count);
    }

    private void CompileIndexAccess(IndexAccessNode idx)
    {
        CompileExpression(idx.Target);
        foreach (var indexExpr in idx.Indices)
        {
            CompileExpression(indexExpr);
        }
        CurrentChunk.WriteOp(OpCode.OpGetIndex);
        WriteShort(idx.Indices.Count);
    }

    private void CompileMemberAccess(MemberAccessNode member)
    {
        CompileExpression(member.Target);
        int nameConst = CurrentChunk.AddConstant(member.MemberName);
        CurrentChunk.WriteOp(OpCode.OpInvoke);
        WriteShort(nameConst);
        WriteShort(0); // 0 argumentos (propriedade como .T ou .rows)
    }

    private void CompileMethodCall(MethodCallNode method)
    {
        CompileExpression(method.Target);
        foreach (var arg in method.Arguments)
        {
            CompileExpression(arg);
        }
        int nameConst = CurrentChunk.AddConstant(method.MethodName);
        CurrentChunk.WriteOp(OpCode.OpInvoke);
        WriteShort(nameConst);
        WriteShort(method.Arguments.Count);
    }

    private void CompileIdentifier(string name)
    {
        int localIdx = ResolveLocal(name);
        if (localIdx != -1)
        {
            CurrentChunk.WriteOp(OpCode.OpGetLocal);
            WriteShort(localIdx);
        }
        else
        {
            int nameConst = CurrentChunk.AddConstant(name);
            CurrentChunk.WriteOp(OpCode.OpGetGlobal);
            WriteShort(nameConst);
        }
    }

    private void CompileAssignment(AssignmentExpressionNode assign)
    {
        string name = assign.Target.Name;
        int localIdx = ResolveLocal(name);

        if (assign.OperatorToken.Type == TokenType.Assign)
        {
            CompileExpression(assign.Value);
        }
        else
        {
            // Atribuição composta (+=, -=, *=, /=)
            CompileIdentifier(name);
            CompileExpression(assign.Value);
            switch (assign.OperatorToken.Type)
            {
                case TokenType.PlusAssign: CurrentChunk.WriteOp(OpCode.OpAdd); break;
                case TokenType.MinusAssign: CurrentChunk.WriteOp(OpCode.OpSubtract); break;
                case TokenType.StarAssign: CurrentChunk.WriteOp(OpCode.OpMultiply); break;
                case TokenType.SlashAssign: CurrentChunk.WriteOp(OpCode.OpDivide); break;
            }
        }

        if (localIdx != -1)
        {
            CurrentChunk.WriteOp(OpCode.OpSetLocal);
            WriteShort(localIdx);
        }
        else
        {
            int nameConst = CurrentChunk.AddConstant(name);
            CurrentChunk.WriteOp(OpCode.OpSetGlobal);
            WriteShort(nameConst);
        }
    }

    private void CompileCall(CallExpressionNode call)
    {
        // Se for print nativo otimizado
        if (call.Callee.Name == "print" || call.Callee.Name == "println")
        {
            foreach (var arg in call.Arguments)
            {
                CompileExpression(arg);
            }
            CurrentChunk.WriteOp(OpCode.OpPrint);
            WriteShort(call.Arguments.Count);
            return;
        }

        // Chamada de função normal: primeiro avalia a função (callee), depois os argumentos
        CompileIdentifier(call.Callee.Name);
        foreach (var arg in call.Arguments)
        {
            CompileExpression(arg);
        }
        CurrentChunk.WriteOp(OpCode.OpCall);
        WriteShort(call.Arguments.Count);
    }

    private void CompileBinary(BinaryExpressionNode bin)
    {
        CompileExpression(bin.Left);
        CompileExpression(bin.Right);

        switch (bin.OperatorToken.Type)
        {
            case TokenType.Plus: CurrentChunk.WriteOp(OpCode.OpAdd); break;
            case TokenType.Minus: CurrentChunk.WriteOp(OpCode.OpSubtract); break;
            case TokenType.Star: CurrentChunk.WriteOp(OpCode.OpMultiply); break;
            case TokenType.Slash: CurrentChunk.WriteOp(OpCode.OpDivide); break;
            case TokenType.Percent: CurrentChunk.WriteOp(OpCode.OpModulo); break;
            case TokenType.Exponent: CurrentChunk.WriteOp(OpCode.OpExponent); break;

            case TokenType.EqualsEquals: CurrentChunk.WriteOp(OpCode.OpEqual); break;
            case TokenType.NotEquals: CurrentChunk.WriteOp(OpCode.OpNotEqual); break;
            case TokenType.GreaterThan: CurrentChunk.WriteOp(OpCode.OpGreater); break;
            case TokenType.GreaterOrEqual: CurrentChunk.WriteOp(OpCode.OpGreaterEqual); break;
            case TokenType.LessThan: CurrentChunk.WriteOp(OpCode.OpLess); break;
            case TokenType.LessOrEqual: CurrentChunk.WriteOp(OpCode.OpLessEqual); break;

            default:
                throw new NotImplementedException($"Operador binário não suportado: {bin.OperatorToken.Text}");
        }
    }

    private void CompileUnary(UnaryExpressionNode un)
    {
        CompileExpression(un.Operand);
        switch (un.OperatorToken.Type)
        {
            case TokenType.Minus: CurrentChunk.WriteOp(OpCode.OpNegate); break;
            case TokenType.LogicalNot or TokenType.KeywordNot: CurrentChunk.WriteOp(OpCode.OpNot); break;
            default:
                throw new NotImplementedException($"Operador unário não suportado: {un.OperatorToken.Text}");
        }
    }

    private int ResolveLocal(string name)
    {
        for (int i = _current.Locals.Count - 1; i >= 0; i--)
        {
            if (_current.Locals[i] == name) return i;
        }
        return -1;
    }

    private void BeginScope() => _current.ScopeDepth++;
    private void EndScope()
    {
        _current.ScopeDepth--;
        while (_current.Locals.Count > 0 && _current.ScopeDepth == 0)
        {
            CurrentChunk.WriteOp(OpCode.OpPop);
            _current.Locals.RemoveAt(_current.Locals.Count - 1);
        }
    }

    private int EmitJump(OpCode op)
    {
        CurrentChunk.WriteOp(op);
        CurrentChunk.Write(0xFF);
        CurrentChunk.Write(0xFF);
        return CurrentChunk.Code.Count - 2;
    }

    private void PatchJump(int offset)
    {
        int jump = CurrentChunk.Code.Count - offset - 2;
        if (jump > ushort.MaxValue) throw new Exception("Salto de controle muito longo para instruções de 16 bits.");
        CurrentChunk.Code[offset] = (byte)((jump >> 8) & 0xFF);
        CurrentChunk.Code[offset + 1] = (byte)(jump & 0xFF);
    }

    private void EmitLoop(int loopStart)
    {
        CurrentChunk.WriteOp(OpCode.OpLoop);
        int offset = CurrentChunk.Code.Count - loopStart + 2;
        if (offset > ushort.MaxValue) throw new Exception("Corpo do laço muito grande.");
        WriteShort(offset);
    }

    private void WriteShort(int value)
    {
        CurrentChunk.Write((byte)((value >> 8) & 0xFF));
        CurrentChunk.Write((byte)(value & 0xFF));
    }
}