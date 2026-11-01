import os

path = r"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\Yvira.Parser\Parser.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

target = """                if (atAction == "exec")
                {
                    return ParseDbExecExpression(atAlias);
                }"""

replacement = """                if (atAction == "exec")
                {
                    return ParseDbExecExpression(atAlias);
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
                }"""

if target in c:
    c = c.replace(target, replacement)
    with open(path, "w", encoding="utf-8") as f:
        f.write(c)
    print("PARSER_PATCHED_OK")
else:
    print("PARSER_TARGET_NOT_FOUND")
