# Yvira - Registro de Mudanças: Interpretador e Runtime (v0.2)

Data: 27 de Outubro de 2026  
Status: Implementado e Validado

---

## 1. O que foi adicionado

### 1.1. Gerenciador de Escopo e Memória (`Yvira.Vm/Environment.cs`)
Implementado o sistema de ambiente de execução responsável pela tabela de símbolos dinâmica e ciclo de vida de variáveis:
- **Encadeamento de Escopos (Parent Scope)**: Suporte a escopos aninhados por blocos `{ ... }`, permitindo que variáveis de blocos internos acessem o escopo pai sem vazar para fora após o término do bloco.
- **Declaração (`Define`)**: Registra variáveis no escopo corrente e impede redeclarações acidentais no mesmo nível.
- **Consulta (`Get`)**: Percorre a hierarquia de escopos de baixo para cima buscando o valor.
- **Reatribuição (`Assign`)**: Altera o valor de uma variável existente.
- **Imutabilidade de Constantes**: Registra identificadores criados com `const` e bloqueia qualquer tentativa de reatribuição com mensagem de erro clara.

---

### 1.2. O Motor de Execução / Avaliador (`Yvira.Vm/Interpreter.cs`)
Implementado o interpretador Tree-Walk capaz de percorrer os nós da AST e executar a semântica da linguagem:

- **Expressões Literais**:
  - `NumberLiteralNode`: Avalia para `double`.
  - `StringLiteralNode`: Avalia para `string`.
  - `BooleanLiteralNode`: Avalia para `bool`.
  - `IdentifierNode`: Resolve o identificador na tabela de símbolos do `Environment`.
  - `ParenthesizedExpressionNode`: Avalia a expressão interna.

- **Operações Aritméticas e Especiais**:
  - `+`: Suporta tanto soma numérica quanto **concatenação de strings** (se qualquer lado for texto).
  - `-`, `*`, `/`, `%`: Operações matemáticas padrão com verificação explícita de **divisão por zero**.
  - `**`: Exponenciação com `Math.Pow`.

- **Operações Lógicas e Curto-Circuito**:
  - Suporte a operadores de curto-circuito: `and` / `&&` e `or` / `||`.
  - Avaliação de verdade com `IsTruthy` (valores numéricos diferentes de zero e strings não vazias são verdadeiros).
  - Operador unário de negação: `not` / `!`.

- **Comparações**:
  - `==`, `!=`: Comparação polimórfica que unifica tipos numéricos.
  - `>`, `<`, `>=`, `<=`: Comparações relacionais numéricas.

- **Atribuições Simples e Compostas**:
  - `=`, `+=`, `-=`, `*=`, `/=`, `**=`.

- **Declarações e Comandos**:
  - `VariableDeclarationNode`: Avalia a expressão inicializadora e armazena no escopo com a flag `isConst`.
  - `BlockStatementNode`: Cria novo escopo filho temporário e descarta ao concluir.
  - `ExpressionStatementNode`: Avalia a expressão contida.

---

### 1.3. Bateria de Testes Integrada (`Yvira.Cli/Program.cs`)
Executados com sucesso 9 cenários reais de ponta a ponta (Código Yvira ➔ Lexer ➔ Parser ➔ AST ➔ Interpretador):
1. **Precedência aritmética**: `2 + 3 * 4;` ➔ Resultado: `14`.
2. **Exponenciação e agrupamento**: `(2 + 3) ** 2;` ➔ Resultado: `25`.
3. **Variáveis e soma**: `let total = preco + taxa;` ➔ Resultado: `60`.
4. **Concatenação de strings**: `"Yvira" + " 1.0"` ➔ Resultado: `"Yvira 1.0"`.
5. **Lógica booleana e curto-circuito**: `ativo and (idade >= 18);` ➔ Resultado: `True`.
6. **Atribuição composta**: `pontos += 5; pontos *= 2;` ➔ Resultado: `30`.
7. **Escopo léxico**: Alteração de variável externa de dentro de um bloco `{ }` ➔ Resultado: `150`.
8. **Proteção de Constantes**: Bloqueio de `maximo = 2000;` gerando erro em tempo de execução.
9. **Segurança de Runtime**: `10 / 0;` gerando exceção controlada de divisão por zero.
