# Yvira - Registro de Mudanças e Arquitetura do Parser (v0.1)

Data: 27 de Outubro de 2026  
Status: Implementado e Validado

---

## 1. O que foi adicionado

### 1.1. Expansão da AST (`Yvira.Ast`)
No arquivo `AstNodes.cs`, foram criados os nós fundamentais para suportar expressões, literais e declarações:
- **`StatementNode`**: Classe base para comandos e declarações.
- **Literais**:
  - `NumberLiteralNode`: Números inteiros e de ponto flutuante (`10`, `3.14`).
  - `StringLiteralNode`: Textos entre aspas (`"exemplo"`).
  - `BooleanLiteralNode`: Valores booleanos (`true`, `false`).
  - `IdentifierNode`: Nomes de variáveis e identificadores (`x`, `idade`).
  - `ParenthesizedExpressionNode`: Expressões agrupadas por parênteses `(...)`.
- **Operações e Atribuições**:
  - `UnaryExpressionNode`: Operações unárias prefixadas (`-x`, `!ativo`, `not pronto`).
  - `BinaryExpressionNode`: Operações com dois operandos (`a + b`, `x * y`, `a > b`, `a and b`).
  - `AssignmentExpressionNode`: Atribuições (`x = 10`, `total += 5`).
- **Declarações e Comandos**:
  - `VariableDeclarationNode`: Declarações de variáveis com `let` ou `const`, suportando inferência ou anotação de tipo (`let x = 10;`, `const idade: Int = 28;`).
  - `ExpressionStatementNode`: Expressão executada como comando terminado em ponto e vírgula.
  - `BlockStatementNode`: Bloco de comandos entre chaves `{ ... }`.
  - `ProgramNode`: Raiz da AST representando o arquivo de código fonte compilado.

---

### 1.2. Precedência de Operadores (`Yvira.Parser/PrecedenceHelper.cs`)
Implementada tabela matemática e lógica formal de precedência:

| Nível | Operadores | Tipo | Associatividade |
| :---: | :--- | :--- | :---: |
| **9** | `-` (unário), `+` (unário), `!` / `not` | Unário | Direita |
| **8** | `**` (exponenciação) | Binário | Direita (`2 ** 3 ** 2` = `2 ** 9`) |
| **7** | `*`, `/`, `%` | Multiplicativo | Esquerda |
| **6** | `+`, `-` | Aditivo | Esquerda |
| **5** | `<`, `>`, `<=`, `>=` | Relacional | Esquerda |
| **4** | `==`, `!=` | Igualdade | Esquerda |
| **3** | `&&`, `and` | E Lógico | Esquerda |
| **2** | `\|\|`, `or` | Ou Lógico | Esquerda |
| **1** | `=`, `+=`, `-=`, `*=`, `/=`, `%=`, `**=` | Atribuição | Direita |

---

### 1.3. O Analisador Sintático (`Yvira.Parser/Parser.cs`)
Implementado o analisador sintático utilizando o padrão **Pratt Parsing** (Precedence Climbing):
- Consumo e validação de tokens (`Match`, `Peek`, `Advance`).
- Coleta de diagnósticos e erros sintáticos amigáveis sem derrubar a aplicação.
- Parsing de expressões respeitando parênteses e prioridades de operadores.
- Suporte a declaração de variáveis com `let` e `const`, tipos opcionais (`: Tipo`) e valores iniciais.

---

### 1.4. Visualizador e Bateria de Testes (`Yvira.Cli/Program.cs`)
Criado um visualizador recursivo de AST (`PrintAst`) que valida cenários reais:
1. `2 + 3 * 4;` ➔ `2 + (3 * 4)` comprovado na árvore.
2. `(2 + 3) * 4;` ➔ `(2 + 3) * 4` com o nó `ParenExpr` forçando a prioridade.
3. `x > 10 and y <= 20;` ➔ Comparações aninhadas corretamente dentro do operador lógico.
4. `-5 + 10;` e `not ativo or pronto;` ➔ Operadores unários capturados com precisão.
5. `2 ** 3 ** 2;` ➔ Associatividade correta à direita.
6. `let nome = "Santhyago";` e `const idade: Int = 28;` ➔ Declarações validadas.
7. `x = y + 2;` e `total += 100;` ➔ Atribuições capturadas como expressões.
