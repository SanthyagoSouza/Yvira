namespace Yvira.Lexer;

public partial class Lexer
{
    // O método principal: lê e devolve o PRÓXIMO Token
    public Token NextToken()
    {
        // 1. Ignora espaços em branco e quebras de linha
        while (char.IsWhiteSpace(Current))
        {
            Next();
        }

        // 2. Se chegou ao fim do arquivo
        if (_position >= _text.Length)
        {
            return new Token(TokenType.EndOfFile, "\0");
        }

        // 3. Se for um NÚMERO (ex: 10, 42, 500)
        if (char.IsDigit(Current))
        {
            int start = _position;
            while (char.IsDigit(Current))
            {
                Next();
            }
            if (Current == '.' && char.IsDigit(Peek()))
            {
                Next();
                while (char.IsDigit(Current))
                {
                    Next();
                }
            }
            string numberText = _text[start.._position];
            return new Token(TokenType.Number, numberText);
        }

        // Se for STRING INTERPOLADA ($"texto {expr}")
        if (Current == '$' && Peek() == '"')
        {
            int start = _position;
            Next(); // Pula '$'
            Next(); // Pula as aspas de abertura '"'

            while (Current != '"' && Current != '\0')
            {
                if (Current == '\\' && Peek() != '\0')
                {
                    Next(); // Pula caractere de escape
                }
                Next();
            }

            if (Current == '"')
            {
                Next(); // Pula as aspas de fechamento
            }

            string stringText = _text[start.._position];
            return new Token(TokenType.InterpolatedString, stringText);
        }

        // Se for uma STRING ("texto")
        if (Current == '"')
        {
            int start = _position;
            Next(); // Pula as aspas de abertura

            while (Current != '"' && Current != '\0')
            {
                if (Current == '\\' && Peek() != '\0')
                {
                    Next(); // Pula caractere de escape
                }
                Next();
            }

            if (Current == '"')
            {
                Next(); // Pula as aspas de fechamento
            }

            string stringText = _text[start.._position];
            return new Token(TokenType.String, stringText);
        }

        // Se for uma PALAVRA (Identificador ou Keyword: letras ou _)
        if (char.IsLetter(Current) || Current == '_')
        {
            int start = _position;

            while (char.IsLetterOrDigit(Current) || Current == '_')
            {
                Next();
            }

            string text = _text[start.._position];
            TokenType type = SyntaxFacts.GetKeywordType(text);
            return new Token(type, text);
        }

        // 4. Se for um OPERADOR, SÍMBOLO ou COMENTÁRIO
        switch (Current)
        {
            // ==========================================
            // COMENTÁRIOS DA YVIRA
            // ==========================================
            case '#':
                int commentStart = _position;

                // Comentário de Bloco: #- ... -#
                if (Peek("-"))
                {
                    Next(); // Consome '#'
                    Next(); // Consome '-'

                    while (Current != '\0')
                    {
                        if (Current == '-' && Peek("#"))
                        {
                            Next(); // Consome '-'
                            Next(); // Consome '#'
                            break;
                        }
                        Next();
                    }
                    string blockText = _text[commentStart.._position];
                    return new Token(TokenType.CommentBlock, blockText);
                }
                else
                {
                    // Comentário de Linha: # ...
                    Next(); // Consome '#'
                    while (Current != '\r' && Current != '\n' && Current != '\0')
                    {
                        Next();
                    }
                    string lineText = _text[commentStart.._position];
                    return new Token(TokenType.CommentLine, lineText);
                }

            // ==========================================
            // SÍMBOLOS ESTRUTURAIS (Todos inclusos!)
            // ==========================================
            case '(': Next(); return new Token(TokenType.OpenParen, "(");
            case ')': Next(); return new Token(TokenType.CloseParen, ")");
            case '{': Next(); return new Token(TokenType.OpenBrace, "{");
            case '}': Next(); return new Token(TokenType.CloseBrace, "}");
            case '[': Next(); return new Token(TokenType.OpenBracket, "[");
            case ']': Next(); return new Token(TokenType.CloseBracket, "]");
            case ';': Next(); return new Token(TokenType.Semicolon, ";");
            case ':': Next(); return new Token(TokenType.Colon, ":");
            case '?': Next(); return new Token(TokenType.Question, "?");
            case ',': Next(); return new Token(TokenType.Comma, ",");
            case '.': Next(); return new Token(TokenType.Dot, ".");
            case '@': Next(); return new Token(TokenType.At, "@");

            // ==========================================
            // OPERADORES LÓGICOS (&&, ||, !)
            // ==========================================
            case '&':
                if (Peek("&"))
                {
                    Next(); Next();
                    return new Token(TokenType.LogicalAnd, "&&");
                }
                break; // Se for só '&' sozinho, cai no BadToken abaixo

            case '|':
                if (Peek("|"))
                {
                    Next(); Next();
                    return new Token(TokenType.LogicalOr, "||");
                }
                break; // Se for só '|' sozinho, cai no BadToken abaixo

            case '!':
                if (Peek("="))
                {
                    Next(); Next();
                    return new Token(TokenType.NotEquals, "!=");
                }
                Next();
                return new Token(TokenType.LogicalNot, "!");

            // ==========================================
            // OPERADORES DE ATRIBUIÇÃO E ARROW
            // ==========================================
            case '=':
                if (Peek("="))
                {
                    Next(); Next();
                    return new Token(TokenType.EqualsEquals, "==");
                }
                if (Peek(">")) // Arrow function (=>)
                {
                    Next(); Next();
                    return new Token(TokenType.Arrow, "=>");
                }
                Next();
                return new Token(TokenType.Assign, "=");

            case '+':
                if (Peek("+"))
                {
                    Next(); Next();
                    return new Token(TokenType.Increment, "++");
                }
                if (Peek("="))
                {
                    Next(); Next();
                    return new Token(TokenType.PlusAssign, "+=");
                }
                Next();
                return new Token(TokenType.Plus, "+");

            case '-':
                if (Peek("-"))
                {
                    Next(); Next();
                    return new Token(TokenType.Decrement, "--");
                }
                if (Peek("="))
                {
                    Next(); Next();
                    return new Token(TokenType.MinusAssign, "-=");
                }
                if (Peek(">")) // Arrow function (->)
                {
                    Next(); Next();
                    return new Token(TokenType.Arrow, "->");
                }
                Next();
                return new Token(TokenType.Minus, "-");

            case '*':
                if (Peek("*="))
                {
                    Next(); Next(); Next();
                    return new Token(TokenType.PowerAssign, "**=");
                }
                if (Peek("*"))
                {
                    Next(); Next();
                    return new Token(TokenType.Exponent, "**");
                }
                if (Peek("="))
                {
                    Next(); Next();
                    return new Token(TokenType.StarAssign, "*=");
                }
                Next();
                return new Token(TokenType.Star, "*");

            case '/':
                if (Peek("="))
                {
                    Next(); Next();
                    return new Token(TokenType.SlashAssign, "/=");
                }
                Next();
                return new Token(TokenType.Slash, "/");

            case '%':
                if (Peek("="))
                {
                    Next(); Next();
                    return new Token(TokenType.ModuloAssign, "%=");
                }
                Next();
                return new Token(TokenType.Percent, "%");

            // ==========================================
            // COMPARAÇÕES (Maior, Menor, Diferente)
            // ==========================================
            case '>':
                if (Peek("="))
                {
                    Next(); Next();
                    return new Token(TokenType.GreaterOrEqual, ">=");
                }
                Next();
                return new Token(TokenType.GreaterThan, ">");

            case '<':
                if (Peek("="))
                {
                    Next(); Next();
                    return new Token(TokenType.LessOrEqual, "<=");
                }
                if (Peek(">")) // Suporte ao '<>' para NotEquals!
                {
                    Next(); Next();
                    return new Token(TokenType.NotEquals, "<>");
                }
                Next();
                return new Token(TokenType.LessThan, "<");
        }

        // 5. Se nenhum dos casos acima corresponder, é um caractere inválido.
        // Avança a posição e retorna um BadToken.
        Next();
        return new Token(TokenType.BadToken, _text.Substring(_position - 1, 1));
    }
}
