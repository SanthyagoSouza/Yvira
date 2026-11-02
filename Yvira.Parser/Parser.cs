using System;
using System.Collections.Generic;
using Yvira.Ast;
using Yvira.Lexer;

namespace Yvira.Parser;

public class Parser
{
    private readonly List<Token> _tokens = new();
    private int _position = 0;
    private readonly List<string> _diagnostics = new();

    public IReadOnlyList<string> Diagnostics => _diagnostics;

    public Parser(string source)
    {
        var lexer = new Lexer.Lexer(source);
        Token token;
        do
        {
            token = lexer.NextToken();
            // Ignoramos BadTokens registrando erro, mas mantemos fluxo
            if (token.Type == TokenType.BadToken)
            {
                _diagnostics.Add($"Erro lÃ©xico: Caractere inesperado '{token.Text}'.");
                continue;
            }
            if (token.Type == TokenType.CommentLine || token.Type == TokenType.CommentBlock)
            {
                continue;
            }
            _tokens.Add(token);
        } while (token.Type != TokenType.EndOfFile);
    }

    private Token Current => Peek(0);

    private Token Peek(int offset)
    {
        int index = _position + offset;
        if (index >= _tokens.Count)
            return _tokens[^1]; // Retorna EndOfFile
        return _tokens[index];
    }

    private Token Match(TokenType expectedType)
    {
        if (Current.Type == expectedType)
        {
            var token = Current;
            _position++;
            return token;
        }

        _diagnostics.Add($"Erro sintÃ¡tico: Esperava token '{expectedType}', mas encontrou '{Current.Type}' ('{Current.Text}').");
        return new Token(expectedType, Current.Text);
    }

    private Token Advance()
    {
        var token = Current;
        _position++;
        return token;
    }

    // Ponto de entrada do Parser para analisar um arquivo completo
    public ProgramNode ParseProgram()
    {
        var statements = new List<StatementNode>();

        while (Current.Type != TokenType.EndOfFile)
        {
            int startPos = _position;
            var stmt = ParseStatement();
            if (stmt != null)
            {
                statements.Add(stmt);
            }

            // ProteÃ§Ã£o contra loop infinito se nÃ£o consumirmos tokens
            if (_position == startPos)
            {
                Advance();
            }
        }

        return new ProgramNode(statements);
    }

    // Analisa uma instruÃ§Ã£o (statement)
    public StatementNode? ParseStatement()
    {
        // DeclaraÃ§Ã£o de funÃ§Ã£o: fn nome(a, b) { ... }
        // Importação de módulos / arquivos: import "caminho" [as alias]
        // Tratamento de erros: try / catch / finally
        // Automação Temporal: @time.funcao(intervalo, ...args) ou @stop.funcao()
        if (Current.Type == TokenType.At)
        {
            return ParseAtScheduleStatement();
        }

        if (Current.Type == TokenType.Try)
        {
            return ParseTryStatement();
        }

        // Lançamento de erro: throw expr;
        if (Current.Type == TokenType.Throw)
        {
            return ParseThrowStatement();
        }

        if (Current.Type == TokenType.Import)
        {
            return ParseImportStatement();
        }

        if (Current.Type == TokenType.Match || Current.Type == TokenType.Switch)
        {
            return ParseMatchStatement();
        }

        if (Current.Type == TokenType.Class)
        {
            return ParseClassDeclaration();
        }

        if (Current.Type == TokenType.Function)
        {
            return ParseFunctionDeclaration();
        }

        // Comando de retorno: return expr;
        if (Current.Type == TokenType.Return)
        {
            return ParseReturnStatement();
        }

        // DeclaraÃ§Ã£o de variÃ¡vel com let ou const
        if (Current.Type == TokenType.Let || Current.Type == TokenType.Const)
        {
            return ParseVariableDeclaration();
        }

        // Controle de fluxo: if
        if (Current.Type == TokenType.If)
        {
            return ParseIfStatement();
        }

        // Controle de fluxo: while
        if (Current.Type == TokenType.While)
        {
            return ParseWhileStatement();
        }

        // Controle de fluxo: for ou foreach
        if (Current.Type == TokenType.For || Current.Type == TokenType.Foreach)
        {
            return ParseForOrForeachStatement();
        }

        // Comandos de interrupção / desvio
        if (Current.Type == TokenType.Break)
        {
            Advance();
            if (Current.Type == TokenType.Semicolon) Advance();
            return new BreakStatementNode();
        }

        if (Current.Type == TokenType.Continue)
        {
            Advance();
            if (Current.Type == TokenType.Semicolon) Advance();
            return new ContinueStatementNode();
        }

        // Bloco delimitado por chaves { }
        if (Current.Type == TokenType.OpenBrace)
        {
            return ParseBlockStatement();
        }

        // VariÃ¡vel declarada por inferÃªncia ou com anotaÃ§Ã£o de tipo (ex: x = 10; ou x : Int;)
        if (Current.Type == TokenType.Identifier && (Peek(1).Type == TokenType.Colon || (Peek(1).Type == TokenType.Assign && IsNewVariableContext())))
        {
            // Tratado como declaraÃ§Ã£o ou atribuiÃ§Ã£o
        }

        return ParseExpressionStatement();
    }

    private bool IsNewVariableContext()
    {
        // Auxiliar que poderÃ¡ consultar escopo no futuro
        return false;
    }

    private StatementNode ParseAtScheduleStatement()
    {
        Advance(); // consome '@'
        var actionToken = Match(TokenType.Identifier);
        string action = actionToken.Text.ToLower();
        string? alias = null;

        if (Current.Type == TokenType.Dot)
        {
            Advance(); // consome '.'
            var aliasToken = Match(TokenType.Identifier);
            alias = aliasToken.Text;
        }

        // Caso: @pdf("arquivo.pdf", [opcoes]) { ... } ou @pdf.fromView(...)
        if (action == "pdf")
        {
            Match(TokenType.OpenParen);
            var filePathExpr = ParseExpression();
            var pdfArgs = new List<ExpressionNode>();
            while (Current.Type == TokenType.Comma)
            {
                Advance(); // consome ','
                pdfArgs.Add(ParseExpression());
            }
            Match(TokenType.CloseParen);

            BlockStatementNode? pdfBody = null;
            if (Current.Type == TokenType.OpenBrace)
            {
                pdfBody = ParseBlockStatement();
            }
            else if (Current.Type == TokenType.Semicolon)
            {
                Advance();
            }

            return new PdfDirectiveStatementNode(alias, filePathExpr, pdfArgs, pdfBody);
        }

        // Caso 1: @db("uri") ou @db.alias("uri")
        if (action == "db")
        {
            Match(TokenType.OpenParen);
            var uriExpr = ParseExpression();
            Match(TokenType.CloseParen);
            if (Current.Type == TokenType.Semicolon) Advance();
            return new DbConnectStatementNode(alias, uriExpr);
        }

        // Caso 2: @exec(...) ou @exec.alias(...) como Statement
        if (action == "exec")
        {
            var execNode = ParseDbExecExpression(alias);
            if (Current.Type == TokenType.Semicolon) Advance();
            return new DbExecStatementNode(execNode);
        }

        // Caso 3: @query(...) ou @query.alias(...) como Statement
        if (action == "query")
        {
            var queryNode = ParseDbQueryExpression(alias);
            if (Current.Type == TokenType.Semicolon) Advance();
            return new DbQueryStatementNode(queryNode);
        }

        // Caso 4: @transaction { ... } ou @transaction.alias { ... }
        if (action == "transaction")
        {
            var body = ParseBlockStatement();
            return new DbTransactionStatementNode(alias, body);
        }

        // Caso: @die(...) ou @die;
        if (action == "die")
        {
            var dieArgs = new List<ExpressionNode>();
            if (Current.Type == TokenType.OpenParen)
            {
                Match(TokenType.OpenParen);
                if (Current.Type != TokenType.CloseParen)
                {
                    while (true)
                    {
                        dieArgs.Add(ParseExpression());
                        if (Current.Type == TokenType.Comma)
                        {
                            Advance();
                            continue;
                        }
                        break;
                    }
                }
                Match(TokenType.CloseParen);
            }
            if (Current.Type == TokenType.Semicolon) Advance();
            return new DieStatementNode(dieArgs);
        }

        // Caso: @sleep(segundos)
        if (action == "sleep")
        {
            Match(TokenType.OpenParen);
            var durExpr = ParseExpression();
            Match(TokenType.CloseParen);
            if (Current.Type == TokenType.Semicolon) Advance();
            return new SleepStatementNode(durExpr);
        }

        // Caso 4: @stop.funcao()
        if (action == "stop")
        {
            string functionName = alias ?? "";
            Match(TokenType.OpenParen);
            Match(TokenType.CloseParen);
            if (Current.Type == TokenType.Semicolon) Advance();
            return new StopScheduleStatementNode(functionName);
        }

        // Caso 5: @time.funcao(intervalo, ...argumentos)
        string fnName = alias ?? "";
        Match(TokenType.OpenParen);
        var intervalExpr = ParseExpression();
        var arguments = new List<ExpressionNode>();

        while (Current.Type == TokenType.Comma)
        {
            Advance(); // consome ','
            arguments.Add(ParseExpression());
        }

        Match(TokenType.CloseParen);
        if (Current.Type == TokenType.Semicolon) Advance();

        return new TimeScheduleStatementNode(action, fnName, intervalExpr, arguments);
    }

    private DbExecNode ParseDbExecExpression(string? alias)
    {
        Match(TokenType.OpenParen);
        var sqlExpr = ParseExpression();
        ExpressionNode? paramsExpr = null;
        ExpressionNode? optionsExpr = null;

        if (Current.Type == TokenType.Comma)
        {
            Advance(); // consome ','
            paramsExpr = ParseExpression();

            if (Current.Type == TokenType.Comma)
            {
                Advance(); // consome 2ª ',' para opções
                optionsExpr = ParseExpression();
            }
        }

        Match(TokenType.CloseParen);
        return new DbExecNode(alias, sqlExpr, paramsExpr, optionsExpr);
    }

    private DbQueryNode ParseDbQueryExpression(string? alias)
    {
        Match(TokenType.OpenParen);
        var sqlExpr = ParseExpression();
        ExpressionNode? paramsExpr = null;

        if (Current.Type == TokenType.Comma)
        {
            Advance(); // consome ','
            paramsExpr = ParseExpression();
        }

        Match(TokenType.CloseParen);
        return new DbQueryNode(alias, sqlExpr, paramsExpr);
    }

    private StatementNode ParseVariableDeclaration()
    {
        var keywordToken = Advance(); // 'let' ou 'const'
        bool isConst = keywordToken.Type == TokenType.Const;

        var identifierToken = Match(TokenType.Identifier);
        var identifier = new IdentifierNode(identifierToken.Text);

        string? typeAnnotation = null;
        if (Current.Type == TokenType.Colon)
        {
            Advance(); // consome ':'
            var typeToken = Match(TokenType.Identifier);
            typeAnnotation = typeToken.Text;
        }

        ExpressionNode? initializer = null;
        if (Current.Type == TokenType.Assign)
        {
            Advance(); // consome '='
            initializer = ParseExpression();
        }

        if (Current.Type == TokenType.Semicolon)
        {
            Advance();
        }

        return new VariableDeclarationNode(keywordToken, identifier, typeAnnotation, initializer, isConst);
    }

    private IfStatementNode ParseIfStatement()
    {
        Advance(); // consome 'if'

        bool hasParen = Current.Type == TokenType.OpenParen;
        if (hasParen) Advance(); // consome '('

        var condition = ParseExpression();

        if (hasParen) Match(TokenType.CloseParen); // consome ')'

        var thenBranch = ParseStatementBlockOrSingle();
        StatementNode? elseBranch = null;

        if (Current.Type == TokenType.Else)
        {
            Advance(); // consome 'else'
            elseBranch = ParseStatementBlockOrSingle();
        }

        return new IfStatementNode(condition, thenBranch, elseBranch);
    }

    private StatementNode ParseForOrForeachStatement()
    {
        var keyword = Advance(); // 'for' ou 'foreach'
        bool isForeachKeyword = keyword.Type == TokenType.Foreach;

        bool hasParen = Current.Type == TokenType.OpenParen;
        if (hasParen) Advance(); // consome '('

        // Caso 1: foreach (item in colecao) OU for (item in colecao)
        if (isForeachKeyword || (Current.Type == TokenType.Identifier && Peek(1).Type == TokenType.In))
        {
            var varToken = Match(TokenType.Identifier);
            var varNode = new IdentifierNode(varToken.Text);
            Match(TokenType.In);
            var collection = ParseExpression();
            if (hasParen) Match(TokenType.CloseParen);
            var body = ParseStatementBlockOrSingle();
            return new ForeachStatementNode(varNode, collection, body);
        }

        // Caso 2: Loop clássico de 3 partes: for (let i = 0; i < 10; i += 1)
        StatementNode? initializer = null;
        if (Current.Type != TokenType.Semicolon)
        {
            if (Current.Type == TokenType.Let || Current.Type == TokenType.Const)
            {
                initializer = ParseVariableDeclaration(); // consome ';' se houver
            }
            else
            {
                initializer = ParseExpressionStatement(); // consome ';' se houver
            }
        }
        else
        {
            Advance(); // consome ';'
        }

        ExpressionNode? condition = null;
        if (Current.Type != TokenType.Semicolon && Current.Type != TokenType.CloseParen)
        {
            condition = ParseExpression();
        }
        if (Current.Type == TokenType.Semicolon)
        {
            Advance(); // consome ';'
        }

        ExpressionNode? increment = null;
        if (Current.Type != TokenType.CloseParen)
        {
            increment = ParseExpression();
        }

        if (hasParen) Match(TokenType.CloseParen);
        var forBody = ParseStatementBlockOrSingle();
        return new ForStatementNode(initializer, condition, increment, forBody);
    }

    private WhileStatementNode ParseWhileStatement()
    {
        Advance(); // consome 'while'

        bool hasParen = Current.Type == TokenType.OpenParen;
        if (hasParen) Advance(); // consome '('

        var condition = ParseExpression();

        if (hasParen) Match(TokenType.CloseParen); // consome ')'

        var body = ParseStatementBlockOrSingle();
        return new WhileStatementNode(condition, body);
    }

    private ClassDeclarationNode ParseClassDeclaration()
    {
        Advance(); // consome 'class'
        var nameToken = Match(TokenType.Identifier);
        var nameNode = new IdentifierNode(nameToken.Text);

        IdentifierNode? superNode = null;
        if (Current.Type == TokenType.Extends)
        {
            Advance(); // consome 'extends'
            var superToken = Match(TokenType.Identifier);
            superNode = new IdentifierNode(superToken.Text);
        }

        Match(TokenType.OpenBrace); // consome '{'
        var methods = new List<ClassMethodDeclarationNode>();
        while (Current.Type != TokenType.CloseBrace && Current.Type != TokenType.EndOfFile)
        {
            bool isStatic = false;
            if (Current.Type == TokenType.Static)
            {
                Advance(); // consome 'static'
                isStatic = true;
            }

            if (Current.Type == TokenType.Function)
            {
                var fnDecl = ParseFunctionDeclaration();
                methods.Add(new ClassMethodDeclarationNode(fnDecl, isStatic));
            }
            else
            {
                _diagnostics.Add($"Erro sintatico em classe '{nameNode.Name}': esperado 'fn' para definicao de metodo, mas encontrou '{Current.Text}'.");
                Advance();
            }
        }
        Match(TokenType.CloseBrace); // consome '}'
        return new ClassDeclarationNode(nameNode, superNode, methods);
    }

    private MatchStatementNode ParseMatchStatement()
    {
        Advance(); // consome 'match' ou 'switch'
        bool hasParen = false;
        if (Current.Type == TokenType.OpenParen)
        {
            Advance();
            hasParen = true;
        }

        var matchedExpr = ParseExpression();
        if (hasParen)
        {
            Match(TokenType.CloseParen);
        }

        Match(TokenType.OpenBrace); // '{'
        var arms = new List<MatchArmNode>();

        while (Current.Type != TokenType.CloseBrace && Current.Type != TokenType.EndOfFile)
        {
            if (Current.Type == TokenType.Case)
            {
                Advance(); // consome 'case' opcional
            }

            ExpressionNode? pattern = null;
            bool isWildcard = false;

            if (Current.Type == TokenType.Default || (Current.Type == TokenType.Identifier && Current.Text == "_"))
            {
                Advance();
                isWildcard = true;
            }
            else
            {
                pattern = ParseExpression();
            }

            if (Current.Type == TokenType.Arrow)
            {
                Advance(); // consome '=>' ou '->'
            }
            else if (Current.Type == TokenType.Colon)
            {
                Advance(); // consome ':'
            }

            AstNode armBody;
            if (Current.Type == TokenType.OpenBrace)
            {
                armBody = ParseBlockStatement();
            }
            else
            {
                var s = ParseStatement();
                armBody = s ?? new BlockStatementNode(new List<StatementNode>());
            }

            arms.Add(new MatchArmNode(pattern, isWildcard, armBody));

            if (Current.Type == TokenType.Comma || Current.Type == TokenType.Semicolon)
            {
                Advance();
            }
        }

        Match(TokenType.CloseBrace); // '}'
        return new MatchStatementNode(matchedExpr, arms);
    }

    private FunctionDeclarationNode ParseFunctionDeclaration()
    {
        Advance(); // consome 'fn'
        var nameToken = Match(TokenType.Identifier);
        var name = new IdentifierNode(nameToken.Text);

        Match(TokenType.OpenParen); // '('
        var parameters = new List<IdentifierNode>();

        if (Current.Type != TokenType.CloseParen)
        {
            while (true)
            {
                var paramToken = Match(TokenType.Identifier);
                parameters.Add(new IdentifierNode(paramToken.Text));

                if (Current.Type == TokenType.Comma)
                {
                    Advance();
                    continue;
                }
                break;
            }
        }

        Match(TokenType.CloseParen); // ')'

        // Corpo da funÃ§Ã£o (exigimos bloco { ... })
        var body = ParseBlockStatement();
        return new FunctionDeclarationNode(name, parameters, body);
    }

    private StatementNode ParseTryStatement()
    {
        Advance(); // consome 'try'
        var tryBlock = ParseBlockStatement();

        IdentifierNode? catchVar = null;
        BlockStatementNode? catchBlock = null;
        BlockStatementNode? finallyBlock = null;

        if (Current.Type == TokenType.Catch)
        {
            Advance(); // consome 'catch'
            if (Current.Type == TokenType.OpenParen)
            {
                Advance(); // consome '('
                if (Current.Type == TokenType.Identifier)
                {
                    catchVar = new IdentifierNode(Advance().Text);
                }
                Match(TokenType.CloseParen);
            }
            else if (Current.Type == TokenType.Identifier)
            {
                catchVar = new IdentifierNode(Advance().Text);
            }
            catchBlock = ParseBlockStatement();
        }

        if (Current.Type == TokenType.Finally)
        {
            Advance(); // consome 'finally'
            finallyBlock = ParseBlockStatement();
        }

        if (catchBlock == null && finallyBlock == null)
        {
            _diagnostics.Add("Erro sintático: Bloco 'try' deve ser seguido por ao menos um bloco 'catch' ou 'finally'.");
        }

        return new TryStatementNode(tryBlock, catchVar, catchBlock, finallyBlock);
    }

    private StatementNode ParseThrowStatement()
    {
        Advance(); // consome 'throw'
        var expr = ParseExpression();
        if (Current.Type == TokenType.Semicolon)
        {
            Advance();
        }
        return new ThrowStatementNode(expr);
    }

        private StatementNode ParseImportStatement()
    {
        Advance(); // Consome 'import'

        if (Current.Type != TokenType.String)
        {
            _diagnostics.Add($"Erro sintático: Esperava caminho do arquivo entre aspas após 'import', mas encontrou '{Current.Text}'.");
            return new ImportStatementNode(string.Empty);
        }

        var pathToken = Advance();
        string modulePath = pathToken.Text;
        if (modulePath.Length >= 2 && modulePath.StartsWith("\"") && modulePath.EndsWith("\""))
        {
            modulePath = modulePath.Substring(1, modulePath.Length - 2);
        }

        string? alias = null;
        if (Current.Type == TokenType.As)
        {
            Advance(); // Consome 'as'
            if (Current.Type == TokenType.Identifier)
            {
                alias = Advance().Text;
            }
            else
            {
                _diagnostics.Add($"Erro sintático: Esperava um identificador após 'as', mas encontrou '{Current.Text}'.");
            }
        }

        if (Current.Type == TokenType.Semicolon)
        {
            Advance();
        }

        return new ImportStatementNode(modulePath, alias);
    }

    private ReturnStatementNode ParseReturnStatement()
    {
        Advance(); // consome 'return'

        ExpressionNode? value = null;
        if (Current.Type != TokenType.Semicolon && Current.Type != TokenType.CloseBrace && Current.Type != TokenType.EndOfFile)
        {
            value = ParseExpression();
        }

        if (Current.Type == TokenType.Semicolon)
        {
            Advance();
        }

        return new ReturnStatementNode(value);
    }

    private StatementNode ParseStatementBlockOrSingle()
    {
        if (Current.Type == TokenType.OpenBrace)
        {
            return ParseBlockStatement();
        }

        var single = ParseStatement();
        return single ?? new BlockStatementNode(Array.Empty<StatementNode>());
    }

    private BlockStatementNode ParseBlockStatement()
    {
        Match(TokenType.OpenBrace);
        var statements = new List<StatementNode>();

        while (Current.Type != TokenType.CloseBrace && Current.Type != TokenType.EndOfFile)
        {
            var stmt = ParseStatement();
            if (stmt != null)
                statements.Add(stmt);
        }

        Match(TokenType.CloseBrace);
        return new BlockStatementNode(statements);
    }

    private ExpressionStatementNode ParseExpressionStatement()
    {
        var expr = ParseExpression();
        if (Current.Type == TokenType.Semicolon)
        {
            Advance();
        }
        return new ExpressionStatementNode(expr);
    }

    // Ponto de entrada para expressÃµes (inicia com precedÃªncia 0)
    public ExpressionNode ParseExpression(int parentPrecedence = 0)
    {
        ExpressionNode left;
        int unaryPrecedence = PrecedenceHelper.GetUnaryOperatorPrecedence(Current.Type);

        if (unaryPrecedence > 0 && unaryPrecedence >= parentPrecedence)
        {
            var operatorToken = Advance();
            var operand = ParseExpression(unaryPrecedence);
            left = new UnaryExpressionNode(operatorToken, operand);
        }
        else
        {
            left = ParsePrimaryExpression();
        }

        // Encadeamento de pÃ³s-fixados: IndexaÃ§Ã£o m[0, 1] ou membros/mÃ©todos m.transpose()
        while (true)
        {
            if (Current.Type == TokenType.OpenBracket)
            {
                Advance(); // consome '['
                var indices = new List<ExpressionNode>();
                if (Current.Type != TokenType.CloseBracket)
                {
                    while (true)
                    {
                        indices.Add(ParseExpression());
                        if (Current.Type == TokenType.Comma)
                        {
                            Advance();
                            continue;
                        }
                        break;
                    }
                }
                Match(TokenType.CloseBracket); // consome ']'
                left = new IndexAccessNode(left, indices);
                continue;
            }

            if (Current.Type == TokenType.Dot)
            {
                Advance(); // consome '.'
                var memberToken = Match(TokenType.Identifier);

                if (Current.Type == TokenType.OpenParen)
                {
                    Advance(); // consome '('
                    var args = new List<ExpressionNode>();
                    if (Current.Type != TokenType.CloseParen)
                    {
                        while (true)
                        {
                            args.Add(ParseExpression());
                            if (Current.Type == TokenType.Comma)
                            {
                                Advance();
                                continue;
                            }
                            break;
                        }
                    }
                    Match(TokenType.CloseParen); // consome ')'
                    left = new MethodCallNode(left, memberToken.Text, args);
                }
                else
                {
                    left = new MemberAccessNode(left, memberToken.Text);
                }
                continue;
            }

            break;
        }

        while (true)
        {
            // Verifica atribuiÃ§Ã£o (associatividade Ã  direita: a = b = c)
            if (Current.Type == TokenType.Assign || Current.Type == TokenType.PlusAssign ||
                Current.Type == TokenType.MinusAssign || Current.Type == TokenType.StarAssign ||
                Current.Type == TokenType.SlashAssign)
            {
                if (left is IdentifierNode idNode)
                {
                    var op = Advance();
                    var value = ParseExpression(PrecedenceHelper.GetBinaryOperatorPrecedence(op.Type));
                    left = new AssignmentExpressionNode(idNode, op, value);
                    continue;
                }
                else if (left is MemberAccessNode memberAccess)
                {
                    var op = Advance();
                    var value = ParseExpression(PrecedenceHelper.GetBinaryOperatorPrecedence(op.Type));
                    left = new MemberAssignmentExpressionNode(memberAccess.Target, memberAccess.MemberName, op, value);
                    continue;
                }
                else if (left is IndexAccessNode indexAccess)
                {
                    var op = Advance();
                    var value = ParseExpression(PrecedenceHelper.GetBinaryOperatorPrecedence(op.Type));
                    left = new IndexAssignmentExpressionNode(indexAccess.Target, indexAccess.Indices, op, value);
                    continue;
                }
            }

            int precedence = PrecedenceHelper.GetBinaryOperatorPrecedence(Current.Type);
            if (precedence == 0 || precedence <= parentPrecedence)
            {
                break;
            }

            var operatorToken = Advance();
            // Para exponenciaÃ§Ã£o (**), a associatividade costuma ser Ã  direita (2 ** 3 ** 2 = 2 ** 9)
            int nextPrecedence = (operatorToken.Type == TokenType.Exponent) ? precedence - 1 : precedence;
            var right = ParseExpression(nextPrecedence);
            left = new BinaryExpressionNode(left, operatorToken, right);
        }

        // Operador Ternário: condicao ? valorVerdadeiro : valorFalso
        if (Current.Type == TokenType.Question && parentPrecedence <= 1)
        {
            Advance(); // consome '?'
            var trueBranch = ParseExpression();
            Match(TokenType.Colon); // consome ':'
            var falseBranch = ParseExpression();
            left = new TernaryExpressionNode(left, trueBranch, falseBranch);
        }

        return left;
    }

    // Analisa termos primÃ¡rios (nÃºmeros, strings, booleanos, identificadores, parÃªnteses)
    private InterpolatedStringNode ParseInterpolatedString(string raw)
    {
        // raw é algo como $"Olá {nome}! Total: {a + b}"
        string content = raw;
        if (content.StartsWith("$\"")) content = content.Substring(2);
        if (content.EndsWith("\"")) content = content.Substring(0, content.Length - 1);

        var expressions = new List<ExpressionNode>();
        var stringParts = new List<string>();

        int pos = 0;
        var currentPart = new System.Text.StringBuilder();

        while (pos < content.Length)
        {
            if (content[pos] == '{' && (pos == 0 || content[pos - 1] != '\\'))
            {
                stringParts.Add(currentPart.ToString());
                currentPart.Clear();
                pos++; // pula '{'

                int braceCount = 1;
                int exprStart = pos;
                while (pos < content.Length && braceCount > 0)
                {
                    if (content[pos] == '{') braceCount++;
                    else if (content[pos] == '}') braceCount--;
                    pos++;
                }

                int exprEnd = braceCount == 0 ? pos - 1 : pos;
                string exprCode = content.Substring(exprStart, Math.Max(0, exprEnd - exprStart));

                var subParser = new Parser(exprCode);
                var parsedExpr = subParser.ParseExpression();
                expressions.Add(parsedExpr);
            }
            else
            {
                currentPart.Append(content[pos]);
                pos++;
            }
        }

        stringParts.Add(currentPart.ToString());
        return new InterpolatedStringNode(raw, expressions, stringParts);
    }

    private ExpressionNode ParsePrimaryExpression()
    {
        switch (Current.Type)
        {
            case TokenType.Number:
                var numToken = Advance();
                double.TryParse(numToken.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double numVal);
                return new NumberLiteralNode(numVal);

            case TokenType.InterpolatedString:
                var interpToken = Advance();
                return ParseInterpolatedString(interpToken.Text);

            case TokenType.String:
                var strToken = Advance();
                // Remove as aspas delimitadoras se existirem
                string text = strToken.Text;
                if (text.Length >= 2 && text.StartsWith("\"") && text.EndsWith("\""))
                    text = text.Substring(1, text.Length - 2);
                return new StringLiteralNode(text);

            case TokenType.At:
                Advance(); // consome '@'
                var atActionToken = Match(TokenType.Identifier);
                string atAction = atActionToken.Text.ToLower();
                string? atAlias = null;
                if (Current.Type == TokenType.Dot)
                {
                    Advance();
                    atAlias = Match(TokenType.Identifier).Text;
                }
                if (atAction == "query")
                {
                    return ParseDbQueryExpression(atAlias);
                }
                if (atAction == "exec")
                {
                    return ParseDbExecExpression(atAlias);
                }
                if (atAction == "spawn")
                {
                    // Caso 1B: @spawn.minhaFuncao(arg1, arg2)
                    string? targetFn = atAlias;
                    var spawnArgs = new List<ExpressionNode>();
                    BlockStatementNode? block = null;

                    if (Current.Type == TokenType.OpenParen)
                    {
                        Match(TokenType.OpenParen);
                        if (Current.Type != TokenType.CloseParen)
                        {
                            while (true)
                            {
                                spawnArgs.Add(ParseExpression());
                                if (Current.Type == TokenType.Comma)
                                {
                                    Advance();
                                    continue;
                                }
                                break;
                            }
                        }
                        Match(TokenType.CloseParen);
                    }
                    else if (Current.Type == TokenType.OpenBrace)
                    {
                        block = ParseBlockStatement();
                    }

                    return new SpawnExpressionNode(targetFn, spawnArgs, block);
                }

                if (atAction == "worker")
                {
                    // Caso 2B: @worker.pool(N)
                    string method = atAlias ?? "pool";
                    Match(TokenType.OpenParen);
                    var workerArgs = new List<ExpressionNode>();
                    if (Current.Type != TokenType.CloseParen)
                    {
                        while (true)
                        {
                            workerArgs.Add(ParseExpression());
                            if (Current.Type == TokenType.Comma)
                            {
                                Advance();
                                continue;
                            }
                            break;
                        }
                    }
                    Match(TokenType.CloseParen);
                    return new WorkerExpressionNode(method, workerArgs);
                }

                if (atAction == "realtime")
                {
                    // @realtime.servidor(...) ou @realtime.cliente(...)
                    string kind = (atAlias ?? "servidor").ToLower();
                    Match(TokenType.OpenParen);
                    var rtArgs = new List<ExpressionNode>();
                    if (Current.Type != TokenType.CloseParen)
                    {
                        while (true)
                        {
                            rtArgs.Add(ParseExpression());
                            if (Current.Type == TokenType.Comma)
                            {
                                Advance();
                                continue;
                            }
                            break;
                        }
                    }
                    Match(TokenType.CloseParen);
                    return new RealtimeExpressionNode(kind, rtArgs);
                }
                throw new Exception($"Diretiva @{atAction} nao e valida como expressao.");

            case TokenType.This:
                var thisToken = Advance();
                return new ThisExpressionNode(thisToken);

            case TokenType.True:
                Advance();
                return new BooleanLiteralNode(true);

            case TokenType.False:
                Advance();
                return new BooleanLiteralNode(false);

            case TokenType.Null:
                Advance();
                return new NullLiteralNode();

            case TokenType.Identifier:
                var idToken = Advance();
                var idNode = new IdentifierNode(idToken.Text);
                // Verifica se Ã© uma chamada de funÃ§Ã£o: nome(...)
                if (Current.Type == TokenType.OpenParen)
                {
                    Advance(); // consome '('
                    var arguments = new List<ExpressionNode>();
                    if (Current.Type != TokenType.CloseParen)
                    {
                        while (true)
                        {
                            arguments.Add(ParseExpression());
                            if (Current.Type == TokenType.Comma)
                            {
                                Advance();
                                continue;
                            }
                            break;
                        }
                    }
                    Match(TokenType.CloseParen);
                    return new CallExpressionNode(idNode, arguments);
                }
                return idNode;

            case TokenType.OpenParen:
                Advance(); // '('
                var innerExpr = ParseExpression();
                Match(TokenType.CloseParen); // ')'
                return new ParenthesizedExpressionNode(innerExpr);

            case TokenType.OpenBrace:
                Advance(); // '{'
                var mapEntries = new List<(ExpressionNode Key, ExpressionNode Value)>();
                if (Current.Type != TokenType.CloseBrace)
                {
                    while (true)
                    {
                        ExpressionNode keyExpr;
                        if (Current.Type == TokenType.Identifier)
                        {
                            var mapKeyId = Advance();
                            keyExpr = new StringLiteralNode(mapKeyId.Text);
                        }
                        else
                        {
                            keyExpr = ParseExpression();
                        }
                        Match(TokenType.Colon);
                        var valueExpr = ParseExpression();
                        mapEntries.Add((keyExpr, valueExpr));

                        if (Current.Type == TokenType.Comma)
                        {
                            Advance();
                            continue;
                        }
                        break;
                    }
                }
                Match(TokenType.CloseBrace); // '}'
                return new MapLiteralNode(mapEntries);

            case TokenType.OpenBracket:
                Advance(); // '['
                var elements = new List<ExpressionNode>();
                if (Current.Type != TokenType.CloseBracket)
                {
                    while (true)
                    {
                        elements.Add(ParseExpression());
                        if (Current.Type == TokenType.Comma)
                        {
                            Advance();
                            continue;
                        }
                        break;
                    }
                }
                Match(TokenType.CloseBracket); // ']'
                return new ArrayLiteralNode(elements);

            default:
                _diagnostics.Add($"Erro sintÃ¡tico: Token inesperado '{Current.Text}' do tipo '{Current.Type}'.");
                var bad = Advance();
                return new IdentifierNode(bad.Text);
        }
    }
}
