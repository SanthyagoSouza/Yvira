using System.Collections.Generic;

namespace Yvira.Lexer;

public static class SyntaxFacts
{
    // O dicionÃ¡rio mapeia o texto para o TokenType correto
    private static readonly Dictionary<string, TokenType> _keywords = new()
    {
        { "let", TokenType.Let },
        { "const", TokenType.Const },
        { "if", TokenType.If },
        { "else", TokenType.Else },
        { "while", TokenType.While },
        { "for", TokenType.For },
        { "foreach", TokenType.Foreach },
        { "in", TokenType.In },
        { "break", TokenType.Break },
        { "continue", TokenType.Continue },
        { "Break", TokenType.Break },
        { "Continue", TokenType.Continue },
        { "fn", TokenType.Function },
        { "return", TokenType.Return },
        { "import", TokenType.Import },
        { "as", TokenType.As },
        { "try", TokenType.Try },
        { "catch", TokenType.Catch },
        { "finally", TokenType.Finally },
        { "throw", TokenType.Throw },
        { "match", TokenType.Match },
        { "switch", TokenType.Switch },
        { "case", TokenType.Case },
        { "default", TokenType.Default },
        { "extends", TokenType.Extends },
        { "static", TokenType.Static },
        { "super", TokenType.Super },
        { "class", TokenType.Class },
        { "this", TokenType.This },
        { "true", TokenType.True },
        { "false", TokenType.False },
        { "null", TokenType.Null },
        { "not", TokenType.KeywordNot },
        { "and", TokenType.KeywordAnd },
        { "or", TokenType.KeywordOr },
 

    };

    // FunÃ§Ã£o que o Lexer vai chamar
    public static TokenType GetKeywordType(string text)
    {
        // Se a palavra existe no dicionÃ¡rio, retorna o tipo dela (ex: TokenType.If)
        if (_keywords.TryGetValue(text, out var type))
        {
            return type;
        }

        // Se nÃ£o existe, Ã© sÃ³ o nome de uma variÃ¡vel/funÃ§Ã£o normal!
        return TokenType.Identifier;
    }
}
