namespace Yvira.Lexer;

public partial class Lexer
{
    private readonly string _text;
    private int _position;

    public Lexer(string text)
    {
        // Se text for null, ele salva string.Empty (texto em branco)
        _text = text ?? string.Empty;
        _position = 0;
    }
}