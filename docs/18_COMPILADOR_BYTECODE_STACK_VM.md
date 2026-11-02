# 18 - Compilador de Bytecode & Máquina Virtual Baseada em Pilha (Stack VM)

A partir da versão 2.1, a linguagem **Yvira** passa a contar com uma arquitetura de execução dual:
1. **Modo Tradicional (AST Tree-Walking)**: ideal para prototipagem rápida e desenvolvimento interativo (`yvira run arquivo.yv`).
2. **Modo Máquina Virtual (Bytecode Stack VM)**: compila o código para um fluxo denso de bytes executados em uma pilha de alta performance (`yvira vm arquivo.yv`).

---

## 1. Como Funciona a Compilação em Bytecode

O pipeline transforma a Árvore Sintática (AST) em instruções atômicas de 8 bits (`OpCode`):

```mermaid
flowchart LR
    Fonte[Código .yv] --> Parser[Parser AST]
    Parser --> Compilador[Compilador de Bytecode]
    Compilador --> Chunk[Chunk de Bytecode + Pool de Constantes]
    Chunk --> VM[Máquina Virtual Baseada em Pilha]
```

### Conjunto Principal de Opcodes (`OpCode.cs`):
- **Constantes & Literais**: `OpConstant`, `OpNull`, `OpTrue`, `OpFalse`.
- **Aritmética & Lógica**: `OpAdd`, `OpSubtract`, `OpMultiply`, `OpDivide`, `OpModulo`, `OpExponent`, `OpNegate`, `OpNot`.
- **Comparações**: `OpEqual`, `OpNotEqual`, `OpGreater`, `OpGreaterEqual`, `OpLess`, `OpLessEqual`.
- **Variáveis**:
  - `OpDefineGlobal`, `OpGetGlobal`, `OpSetGlobal`.
  - `OpGetLocal`, `OpSetLocal` (acesso direto a slots de array com custo $O(1)$ sem overhead de hash table).
- **Controle de Fluxo**: `OpJump`, `OpJumpIfFalse`, `OpLoop`.
- **Instruções Especiais**: `OpPrint`, `OpPop`, `OpReturn`.

---

## 2. Comandos no CLI

### Executando com a Máquina Virtual de Bytecode:
```bash
yvira vm teste_benchmark.yv
```

### Comparando a Performance em Tempo Real (`bench`):
O comando `yvira bench` executa o mesmo código em ambos os motores e exibe o tempo e a taxa de aceleração:
```bash
yvira bench teste_benchmark.yv
```

**Exemplo de saída:**
```
=== RESULTADO DO BENCHMARK DE PERFORMANCE ===
[1] Interpretador AST (Tree-walk)  : 858 ms
[2] Compilador Bytecode & Stack VM  : 429 ms
Aceleração Obtida                   : 2.0x mais rápido!
```