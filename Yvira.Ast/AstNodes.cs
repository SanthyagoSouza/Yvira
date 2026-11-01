using System.Collections.Generic;
using Yvira.Lexer;

namespace Yvira.Ast;

// 1. Base para qualquer nÃ³ da Ãrvore SintÃ¡tica Abstrata
public abstract class AstNode
{
}

// 2. Base para ExpressÃµes (tudo que avalia e retorna um valor)
public abstract class ExpressionNode : AstNode
{
}

// 3. Base para DeclaraÃ§Ãµes / Comandos (Statements - nÃ£o produzem valor diretamente)
public abstract class StatementNode : AstNode
{
}

// ==========================================
// EXPRESSÃ•ES LITERAIS E IDENTIFICADORES
// ==========================================

// NÃºmero literal: 10, 3.14
public class NumberLiteralNode : ExpressionNode
{
    public double Value { get; }

    public NumberLiteralNode(double value)
    {
        Value = value;
    }
}

// String literal: "texto"
public class StringLiteralNode : ExpressionNode
{
    public string Value { get; }

    public StringLiteralNode(string value)
    {
        Value = value;
    }
}

// Literal nulo: null
public class NullLiteralNode : ExpressionNode { }

// Booleano literal: true / false
public class BooleanLiteralNode : ExpressionNode
{
    public bool Value { get; }

    public BooleanLiteralNode(bool value)
    {
        Value = value;
    }
}

// Identificador: nome da variÃ¡vel ou funÃ§Ã£o (ex: total, x)
public class IdentifierNode : ExpressionNode
{
    public string Name { get; }

    public IdentifierNode(string name)
    {
        Name = name;
    }
}

// ExpressÃ£o entre parÃªnteses: (2 + 3)
public class ParenthesizedExpressionNode : ExpressionNode
{
    public ExpressionNode Expression { get; }

    public ParenthesizedExpressionNode(ExpressionNode expression)
    {
        Expression = expression;
    }
}

// ==========================================
// OPERAÃ‡Ã•ES
// ==========================================

// OperaÃ§Ã£o UnÃ¡ria: -x, !ativo, not flag
public class UnaryExpressionNode : ExpressionNode
{
    public Token OperatorToken { get; }
    public ExpressionNode Operand { get; }

    public UnaryExpressionNode(Token operatorToken, ExpressionNode operand)
    {
        OperatorToken = operatorToken;
        Operand = operand;
    }
}

// OperaÃ§Ã£o BinÃ¡ria: a + b, x * y, a == b, a and b
// Operação Ternária: condicao ? valorVerdadeiro : valorFalso
public class TernaryExpressionNode : ExpressionNode
{
    public ExpressionNode Condition { get; }
    public ExpressionNode TrueBranch { get; }
    public ExpressionNode FalseBranch { get; }

    public TernaryExpressionNode(ExpressionNode condition, ExpressionNode trueBranch, ExpressionNode falseBranch)
    {
        Condition = condition;
        TrueBranch = trueBranch;
        FalseBranch = falseBranch;
    }
}

public class BinaryExpressionNode : ExpressionNode
{
    public ExpressionNode Left { get; }
    public Token OperatorToken { get; }
    public ExpressionNode Right { get; }

    public BinaryExpressionNode(ExpressionNode left, Token operatorToken, ExpressionNode right)
    {
        Left = left;
        OperatorToken = operatorToken;
        Right = right;
    }
}

// ExpressÃ£o de AtribuiÃ§Ã£o: x = 10, total += 5
public class AssignmentExpressionNode : ExpressionNode
{
    public IdentifierNode Target { get; }
    public Token OperatorToken { get; } // =, +=, -=, *=, /=
    public ExpressionNode Value { get; }

    public AssignmentExpressionNode(IdentifierNode target, Token operatorToken, ExpressionNode value)
    {
        Target = target;
        OperatorToken = operatorToken;
        Value = value;
    }
}

// Expressão 'this'
public class ThisExpressionNode : ExpressionNode
{
    public Token KeywordToken { get; }

    public ThisExpressionNode(Token keywordToken)
    {
        KeywordToken = keywordToken;
    }
}

// Expressão de Atribuição a Membro de Objeto: obj.campo = valor, this.nome = "Ana"
public class MemberAssignmentExpressionNode : ExpressionNode
{
    public ExpressionNode Target { get; }
    public string MemberName { get; }
    public Token OperatorToken { get; }
    public ExpressionNode Value { get; }

    public MemberAssignmentExpressionNode(ExpressionNode target, string memberName, Token operatorToken, ExpressionNode value)
    {
        Target = target;
        MemberName = memberName;
        OperatorToken = operatorToken;
        Value = value;
    }
}

// Expressão de Atribuição a Índice/Elemento: lista[i] = valor, mat[r, c] = valor, map["chave"] = valor
public class IndexAssignmentExpressionNode : ExpressionNode
{
    public ExpressionNode Target { get; }
    public IReadOnlyList<ExpressionNode> Indices { get; }
    public Token OperatorToken { get; }
    public ExpressionNode Value { get; }

    public IndexAssignmentExpressionNode(ExpressionNode target, IReadOnlyList<ExpressionNode> indices, Token operatorToken, ExpressionNode value)
    {
        Target = target;
        Indices = indices;
        OperatorToken = operatorToken;
        Value = value;
    }
}

// Chamada de FunÃ§Ã£o: print("OlÃ¡", 123); ou calcular(x, y);
public class CallExpressionNode : ExpressionNode
{
    public IdentifierNode Callee { get; }
    public IReadOnlyList<ExpressionNode> Arguments { get; }

    public CallExpressionNode(IdentifierNode callee, IReadOnlyList<ExpressionNode> arguments)
    {
        Callee = callee;
        Arguments = arguments;
    }
}

// DeclaraÃ§Ã£o Literal de Array/Matriz: [1, 2, 3] ou [[1, 2], [3, 4]]

// Declaração Literal de Dicionário / Mapa: {"chave": 10, "outro": 20}
public class MapLiteralNode : ExpressionNode
{
    public IReadOnlyList<(ExpressionNode Key, ExpressionNode Value)> Entries { get; }

    public MapLiteralNode(IReadOnlyList<(ExpressionNode Key, ExpressionNode Value)> entries)
    {
        Entries = entries;
    }
}
public class ArrayLiteralNode : ExpressionNode
{
    public IReadOnlyList<ExpressionNode> Elements { get; }

    public ArrayLiteralNode(IReadOnlyList<ExpressionNode> elements)
    {
        Elements = elements;
    }
}

// Acesso a elementos ou fatias: m[0], m[0, 1] ou v[i]
public class IndexAccessNode : ExpressionNode
{
    public ExpressionNode Target { get; }
    public IReadOnlyList<ExpressionNode> Indices { get; }

    public IndexAccessNode(ExpressionNode target, IReadOnlyList<ExpressionNode> indices)
    {
        Target = target;
        Indices = indices;
    }
}

// Acesso a membros / mÃ©todos: m.transpose(), m.rows, m.row(0)
public class MemberAccessNode : ExpressionNode
{
    public ExpressionNode Target { get; }
    public string MemberName { get; }

    public MemberAccessNode(ExpressionNode target, string memberName)
    {
        Target = target;
        MemberName = memberName;
    }
}

// Chamada de mÃ©todo: m.transpose() ou m.row(0)
public class MethodCallNode : ExpressionNode
{
    public ExpressionNode Target { get; }
    public string MethodName { get; }
    public IReadOnlyList<ExpressionNode> Arguments { get; }

    public MethodCallNode(ExpressionNode target, string methodName, IReadOnlyList<ExpressionNode> arguments)
    {
        Target = target;
        MethodName = methodName;
        Arguments = arguments;
    }
}

// ==========================================
// STATEMENTS (DECLARAÃ‡Ã•ES / COMANDOS)
// ==========================================

// DeclaraÃ§Ã£o de VariÃ¡vel: let x = 10; ou const y = 20; ou x : Int = 10;
public class VariableDeclarationNode : StatementNode
{
    public Token? KeywordToken { get; }   // 'let', 'const' ou null se inferido diretamente
    public IdentifierNode Identifier { get; }
    public string? TypeAnnotation { get; } // Ex: "Int", "String" se fornecido com : Type
    public ExpressionNode? Initializer { get; }
    public bool IsConst { get; }

    public VariableDeclarationNode(Token? keywordToken, IdentifierNode identifier, string? typeAnnotation, ExpressionNode? initializer, bool isConst = false)
    {
        KeywordToken = keywordToken;
        Identifier = identifier;
        TypeAnnotation = typeAnnotation;
        Initializer = initializer;
        IsConst = isConst;
    }
}

// ExpressÃ£o usada como comando: x + 1; ou print("oi");
public class ExpressionStatementNode : StatementNode
{
    public ExpressionNode Expression { get; }

    public ExpressionStatementNode(ExpressionNode expression)
    {
        Expression = expression;
    }
}

// Bloco de comandos: { ... }
public class BlockStatementNode : StatementNode
{
    public IReadOnlyList<StatementNode> Statements { get; }

    public BlockStatementNode(IReadOnlyList<StatementNode> statements)
    {
        Statements = statements;
    }
}

// Comando condicional: if (condicao) { ... } else { ... }
public class IfStatementNode : StatementNode
{
    public ExpressionNode Condition { get; }
    public StatementNode ThenBranch { get; }
    public StatementNode? ElseBranch { get; }

    public IfStatementNode(ExpressionNode condition, StatementNode thenBranch, StatementNode? elseBranch)
    {
        Condition = condition;
        ThenBranch = thenBranch;
        ElseBranch = elseBranch;
    }
}

// Comando de laÃ§o: while (condicao) { ... }

// Comando de laço: for (let i = 0; i < 10; i = i + 1) { ... }
public class ForStatementNode : StatementNode
{
    public StatementNode? Initializer { get; }
    public ExpressionNode? Condition { get; }
    public ExpressionNode? Increment { get; }
    public StatementNode Body { get; }

    public ForStatementNode(StatementNode? initializer, ExpressionNode? condition, ExpressionNode? increment, StatementNode body)
    {
        Initializer = initializer;
        Condition = condition;
        Increment = increment;
        Body = body;
    }
}

// Comando de laço foreach: foreach (item in colecao) { ... } ou for (item in colecao) { ... }
public class ForeachStatementNode : StatementNode
{
    public IdentifierNode Variable { get; }
    public ExpressionNode Collection { get; }
    public StatementNode Body { get; }

    public ForeachStatementNode(IdentifierNode variable, ExpressionNode collection, StatementNode body)
    {
        Variable = variable;
        Collection = collection;
        Body = body;
    }
}

// Comandos de interrupção / continuação de laço
public class BreakStatementNode : StatementNode
{
}

public class ContinueStatementNode : StatementNode
{
}
public class WhileStatementNode : StatementNode
{
    public ExpressionNode Condition { get; }
    public StatementNode Body { get; }

    public WhileStatementNode(ExpressionNode condition, StatementNode body)
    {
        Condition = condition;
        Body = body;
    }
}

// DeclaraÃ§Ã£o de funÃ§Ã£o: fn nome(param1, param2) { ... }
public class FunctionDeclarationNode : StatementNode
{
    public IdentifierNode Name { get; }
    public IReadOnlyList<IdentifierNode> Parameters { get; }
    public BlockStatementNode Body { get; }

    public FunctionDeclarationNode(IdentifierNode name, IReadOnlyList<IdentifierNode> parameters, BlockStatementNode body)
    {
        Name = name;
        Parameters = parameters;
        Body = body;
    }
}

// Comando de retorno: return valor; ou return;
public class ReturnStatementNode : StatementNode
{
    public ExpressionNode? Value { get; }

    public ReturnStatementNode(ExpressionNode? value)
    {
        Value = value;
    }
}

// Comando de importação de arquivo: import "caminho" ou import "caminho" as alias
public class ImportStatementNode : StatementNode
{
    public string ModulePath { get; }
    public string? Alias { get; }

    public ImportStatementNode(string modulePath, string? alias = null)
    {
        ModulePath = modulePath;
        Alias = alias;
    }
}

// Raiz do programa / arquivo
// Comando throw: throw expr;
public class ThrowStatementNode : StatementNode
{
    public ExpressionNode Expression { get; }

    public ThrowStatementNode(ExpressionNode expression)
    {
        Expression = expression;
    }
}

// Bloco try-catch-finally
public class TryStatementNode : StatementNode
{
    public BlockStatementNode TryBlock { get; }
    public IdentifierNode? CatchVariable { get; }
    public BlockStatementNode? CatchBlock { get; }
    public BlockStatementNode? FinallyBlock { get; }

    public TryStatementNode(BlockStatementNode tryBlock, IdentifierNode? catchVariable, BlockStatementNode? catchBlock, BlockStatementNode? finallyBlock)
    {
        TryBlock = tryBlock;
        CatchVariable = catchVariable;
        CatchBlock = catchBlock;
        FinallyBlock = finallyBlock;
    }
}

// Método de Classe (com flag de static)
public class ClassMethodDeclarationNode : StatementNode
{
    public FunctionDeclarationNode Function { get; }
    public bool IsStatic { get; }

    public ClassMethodDeclarationNode(FunctionDeclarationNode function, bool isStatic)
    {
        Function = function;
        IsStatic = isStatic;
    }
}

// Declaração de Classe: class Nome extends Pai { fn ... }
public class ClassDeclarationNode : StatementNode
{
    public IdentifierNode Name { get; }
    public IdentifierNode? SuperClass { get; }
    public IReadOnlyList<ClassMethodDeclarationNode> Methods { get; }

    public ClassDeclarationNode(IdentifierNode name, IdentifierNode? superClass, IReadOnlyList<ClassMethodDeclarationNode> methods)
    {
        Name = name;
        SuperClass = superClass;
        Methods = methods;
    }
}

// Expressão de String Interpolada: $"Olá {nome}!"
public class InterpolatedStringNode : ExpressionNode
{
    public string RawText { get; }
    public IReadOnlyList<ExpressionNode> Expressions { get; }
    public IReadOnlyList<string> StringParts { get; }

    public InterpolatedStringNode(string rawText, IReadOnlyList<ExpressionNode> expressions, IReadOnlyList<string> stringParts)
    {
        RawText = rawText;
        Expressions = expressions;
        StringParts = stringParts;
    }
}

// Caso individual do match / switch: case / "valor" => { ... }
public class MatchArmNode : AstNode
{
    public ExpressionNode? Pattern { get; } // null = _ ou default
    public bool IsWildcard { get; }
    public AstNode Body { get; } // pode ser BlockStatementNode ou ExpressionNode ou StatementNode

    public MatchArmNode(ExpressionNode? pattern, bool isWildcard, AstNode body)
    {
        Pattern = pattern;
        IsWildcard = isWildcard;
        Body = body;
    }
}

// Estrutura match / switch: match (expr) { caso => corpo, ... }
public class MatchStatementNode : StatementNode
{
    public ExpressionNode Expression { get; }
    public IReadOnlyList<MatchArmNode> Arms { get; }

    public MatchStatementNode(ExpressionNode expression, IReadOnlyList<MatchArmNode> arms)
    {
        Expression = expression;
        Arms = arms;
    }
}

// Diretiva @die(...) ou @die;
public class DieStatementNode : StatementNode
{
    public IReadOnlyList<ExpressionNode> Arguments { get; }

    public DieStatementNode(IReadOnlyList<ExpressionNode> arguments)
    {
        Arguments = arguments;
    }
}

// Diretiva @sleep(segundos)
public class SleepStatementNode : StatementNode
{
    public ExpressionNode Duration { get; }

    public SleepStatementNode(ExpressionNode duration)
    {
        Duration = duration;
    }
}

// Agendamento Temporal: @time.funcao(intervalo, ...args)
public class TimeScheduleStatementNode : StatementNode
{
    public string Action { get; } // Ex: time ou stop
    public string FunctionName { get; }
    public ExpressionNode Interval { get; }
    public IReadOnlyList<ExpressionNode> Arguments { get; }

    public TimeScheduleStatementNode(string action, string functionName, ExpressionNode interval, IReadOnlyList<ExpressionNode> arguments)
    {
        Action = action;
        FunctionName = functionName;
        Interval = interval;
        Arguments = arguments;
    }
}

// Interrupcao de Agendamento: @stop.funcao()
public class StopScheduleStatementNode : StatementNode
{
    public string FunctionName { get; }

    public StopScheduleStatementNode(string functionName)
    {
        FunctionName = functionName;
    }
}

// Diretivas de Banco de Dados Nativas:
// @db("uri") ou @db.alias("uri")
public class DbConnectStatementNode : StatementNode
{
    public string? Alias { get; }
    public ExpressionNode UriExpression { get; }

    public DbConnectStatementNode(string? alias, ExpressionNode uriExpression)
    {
        Alias = alias;
        UriExpression = uriExpression;
    }
}

// @exec("sql", [params], options?) ou @exec.alias("sql", [params], options?)
public class DbExecNode : ExpressionNode
{
    public string? Alias { get; }
    public ExpressionNode SqlExpression { get; }
    public ExpressionNode? ParametersExpression { get; }
    public ExpressionNode? OptionsExpression { get; }

    public DbExecNode(string? alias, ExpressionNode sqlExpression, ExpressionNode? parametersExpression, ExpressionNode? optionsExpression = null)
    {
        Alias = alias;
        SqlExpression = sqlExpression;
        ParametersExpression = parametersExpression;
        OptionsExpression = optionsExpression;
    }
}

public class DbExecStatementNode : StatementNode
{
    public DbExecNode ExecNode { get; }

    public DbExecStatementNode(DbExecNode execNode)
    {
        ExecNode = execNode;
    }
}

// @query("sql", [params]) ou @query.alias("sql", [params])
public class DbQueryNode : ExpressionNode
{
    public string? Alias { get; }
    public ExpressionNode SqlExpression { get; }
    public ExpressionNode? ParametersExpression { get; }

    public DbQueryNode(string? alias, ExpressionNode sqlExpression, ExpressionNode? parametersExpression)
    {
        Alias = alias;
        SqlExpression = sqlExpression;
        ParametersExpression = parametersExpression;
    }
}

public class DbQueryStatementNode : StatementNode
{
    public DbQueryNode QueryNode { get; }

    public DbQueryStatementNode(DbQueryNode queryNode)
    {
        QueryNode = queryNode;
    }
}

// @transaction { ... } ou @transaction.alias { ... }
public class DbTransactionStatementNode : StatementNode
{
    public string? Alias { get; }
    public BlockStatementNode Body { get; }

    public DbTransactionStatementNode(string? alias, BlockStatementNode body)
    {
        Alias = alias;
        Body = body;
    }
}

// Superpoder @realtime: @realtime.servidor(...) ou @realtime.cliente(...)
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

// Diretiva @pdf("arquivo.pdf", [opcoes]) { ... } ou @pdf.fromView(...)
public class PdfDirectiveStatementNode : StatementNode
{
    public string? Method { get; }
    public ExpressionNode FilePathExpression { get; }
    public IReadOnlyList<ExpressionNode> Arguments { get; }
    public BlockStatementNode? Body { get; }

    public PdfDirectiveStatementNode(string? method, ExpressionNode filePathExpression, IReadOnlyList<ExpressionNode> arguments, BlockStatementNode? body = null)
    {
        Method = method;
        FilePathExpression = filePathExpression;
        Arguments = arguments;
        Body = body;
    }
}

public class ProgramNode : AstNode
{
    public IReadOnlyList<StatementNode> Statements { get; }

    public ProgramNode(IReadOnlyList<StatementNode> statements)
    {
        Statements = statements;
    }
}
