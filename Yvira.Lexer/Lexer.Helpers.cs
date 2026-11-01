namespace Yvira.Lexer;

public partial class Lexer
{
    // Retorna o caractere onde o "dedo" está apontando agora.
    private char Current => _position < _text.Length ? _text[_position] : '\0';

    // Dá uma "espiada" em um único caractere à frente...
    private char Peek(int offset = 1)
    {
        int index = _position + offset;
        return index < _text.Length ? _text[index] : '\0';
    }

    // A Evolução Dinâmica: Verifica strings inteiras sem alocar memória
    private bool Peek(string expectedText)
    {
        if (_position + 1 + expectedText.Length > _text.Length)
            return false;

        for (int i = 0; i < expectedText.Length; i++)
        {
            if (_text[_position + 1 + i] != expectedText[i])
            {
                return false;
            }
        }
        return true;
    }

    // Move o dedo para a próxima letra
    private void Next()
    {
        _position++;
    }
}