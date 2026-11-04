namespace Yvira.Lexer;

public enum TokenType
{
    // ==========================================
    // 1. VALORES E IDENTIFICADORES
    // ==========================================
    Number,       // Ex: 10, 42, 3.14
    String,       // Ex: "OlÃ¡, Mundo!"
    Identifier,   // Nomes de variÃ¡veis/funÃ§Ãµes (ex: resultado, x, calcular)
    True,         // true  (Valores booleanos reais)
    False,        // false
    Null,         // null (Valores booleanos reais)

    // ==========================================
    // 2. OPERADORES ARITMÃ‰TICOS E ATRIBUIÃ‡ÃƒO
    // ==========================================
    Plus,         // + ok
    Minus,        // - ok
    Star,         // * ok
    Slash,        // / ok
    Modulo,       // % ok
    Percent,      // % ok
    Assign,       // = ok
    PlusAssign,   // += ok
    MinusAssign,  // -= ok
    StarAssign,   // *=
    SlashAssign,  // /=
    ModuloAssign, // %=
    Exponent,     // **
    PowerAssign,  // **=
    Increment,    // ++ ok 
    Decrement,    // -- ok
    Question,     // ? (Operador Ternário)




    // ==========================================
    // 3. OPERADORES DE COMPARAÃ‡ÃƒO E LÃ“GICA
    // EstratÃ©gia das "Duas Formas": SÃ­mbolos matemÃ¡ticos vs Palavras
    // ==========================================
    EqualsEquals, // == ok
    NotEquals,    // != ou <> (Movi o <> para cÃ¡, que Ã© o padrÃ£o para "diferente")
    LessThan,     // <
    GreaterThan,  // >
    LessOrEqual,  // <=
    GreaterOrEqual,// >=

    LogicalAnd,   // && (Forma sÃ­mbolo)
    KeywordAnd,   // and (Forma texto)

    LogicalOr,    // || (Forma sÃ­mbolo)
    KeywordOr,    // or (Forma texto)

    LogicalNot,   // ! (Forma sÃ­mbolo)
    KeywordNot,   // not (Forma texto)

    // ==========================================
    // 4. PALAVRAS-CHAVE (KEYWORDS)
    // ==========================================
    Let,          // let (VocÃª pode adicionar 'Var' ou 'Mut' no futuro se quiser)
    Const,        // const 
    If,           // if
    Else,         // else
    While,        // while
    For,          // for
    Foreach,      // foreach
    In,           // in
    Break,        // break
    Continue,     // continue
    Function,     // fn ou function
    Return,       // return
    Class,        // class
    This,         // this
    Import,       // import
    As,           // as
    Try,          // try
    Catch,        // catch
    Finally,      // finally
    Throw,        // throw
    Match,        // match
    Switch,       // switch
    Case,         // case
    Default,      // default
    Extends,      // extends
    Static,       // static
    Super,        // super
    Enum,         // enum
    Is,           // is
    InterpolatedString, // $"... {expr} ..."

    // ==========================================
    // 5. SÃMBOLOS E ESTRUTURA
    // ==========================================
    At,           // @
    Semicolon,    // ;
    Comma,        // ,
    Colon,        // :
    Dot,          // .
    Arrow,        // => ou -> (Muito Ãºtil para arrow functions ou retornos)
    OpenParen,    // (
    CloseParen,   // )
    OpenBrace,    // {
    CloseBrace,   // }
    OpenBracket,  // [
    CloseBracket, // ]

    // ==========================================
    // 6. COMENTÃRIOS DA LINGUAGEM
    // ==========================================
    // Vale ressaltar que os mesmos estÃ£o aqui apenas para conhecimento
    // Todos serÃ£o ignorados e existiram apenas como forma de ignorar bloco de codigo
    CommentLine,  // #
    CommentBlock, // #- ... -# 

    // ==========================================
    // 7. CONTROLE DO LEXER
    // ==========================================
    EndOfFile,    // Fim do arquivo / texto
    BadToken      // Caractere nÃ£o reconhecido / Erro de sintaxe
}
