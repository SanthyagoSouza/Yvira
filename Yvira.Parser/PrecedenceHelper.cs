using Yvira.Lexer;

namespace Yvira.Parser;

public static class PrecedenceHelper
{
    // Retorna a precedência do operador binário. Quanto maior o valor, mais prioritário ele é.
    public static int GetBinaryOperatorPrecedence(TokenType type)
    {
        return type switch
        {
            // Atribuição (menor precedência)
            TokenType.Assign or TokenType.PlusAssign or TokenType.MinusAssign or
            TokenType.StarAssign or TokenType.SlashAssign or TokenType.ModuloAssign or
            TokenType.PowerAssign => 1,

            // Ou Lógico
            TokenType.LogicalOr or TokenType.KeywordOr => 2,

            // E Lógico
            TokenType.LogicalAnd or TokenType.KeywordAnd => 3,

            // Comparações de Igualdade
            TokenType.EqualsEquals or TokenType.NotEquals => 4,

            // Comparações Relacionais e Type Check (is)
            TokenType.LessThan or TokenType.GreaterThan or
            TokenType.LessOrEqual or TokenType.GreaterOrEqual or
            TokenType.Is => 5,

            // Adição e Subtração
            TokenType.Plus or TokenType.Minus => 6,

            // Multiplicação, Divisão e Módulo
            TokenType.Star or TokenType.Slash or TokenType.Modulo or TokenType.Percent => 7,

            // Exponenciação (maior precedência de operadores binários)
            TokenType.Exponent => 8,

            // Não é operador binário
            _ => 0
        };
    }

    // Retorna a precedência de operador unário (+, -, !, not)
    public static int GetUnaryOperatorPrecedence(TokenType type)
    {
        return type switch
        {
            TokenType.Plus or TokenType.Minus or
            TokenType.LogicalNot or TokenType.KeywordNot => 9,

            _ => 0
        };
    }
}
