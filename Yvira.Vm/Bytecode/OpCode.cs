namespace Yvira.Vm.Bytecode;

public enum OpCode : byte
{
    // Constantes e Literais
    OpConstant,        // Carrega constante da pool
    OpNull,            // Empurra null
    OpTrue,            // Empurra true
    OpFalse,           // Empurra false

    // Operações Aritméticas e Lógicas
    OpAdd,             // +
    OpSubtract,        // -
    OpMultiply,        // *
    OpDivide,          // /
    OpModulo,          // %
    OpExponent,        // **
    OpNegate,          // - (unário)
    OpNot,             // ! ou not

    // Comparações
    OpEqual,           // ==
    OpNotEqual,        // !=
    OpGreater,         // >
    OpGreaterEqual,    // >=
    OpLess,            // <
    OpLessEqual,       // <=

    // Variáveis Globais
    OpDefineGlobal,    // define variável global com nome
    OpGetGlobal,       // lê variável global
    OpSetGlobal,       // atribui variável global

    // Variáveis Locais (Stack offsets para alta performance)
    OpGetLocal,        // lê da pilha local (índice relativo)
    OpSetLocal,        // atribui na pilha local

    // Controle de Fluxo
    OpJump,            // salto incondicional
    OpJumpIfFalse,     // salta se o topo da pilha for falso
    OpLoop,            // salta para trás (loops)

    // Funções e Retorno
    OpCall,            // invoca função ou callable
    OpReturn,          // retorna da função atual

    // Estruturas e Operações Especiais
    OpArray,           // monta array/matriz literal com N elementos
    OpMatrix,          // monta matriz nativa (rows, cols) a partir dos elementos na pilha
    OpGetIndex,        // a[i] ou a[r, c]
    OpSetIndex,        // a[i] = v ou a[r, c] = v
    OpInvoke,          // invoca método nativo em objeto/matriz com argumentos
    OpPop,             // descarta o topo da pilha (para instruções/statements)
    OpPrint            // instrução nativa rápida para impressão
}