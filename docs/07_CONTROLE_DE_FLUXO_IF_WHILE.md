# Yvira - Registro de Mudanças: Controle de Fluxo com If/Else e While (v0.4)

Data: 27 de Outubro de 2026  
Status: Implementado e Validado com Sucesso

---

## 1. O que foi adicionado

### 1.1. Novos Nós na Árvore Sintática (`Yvira.Ast/AstNodes.cs`)
- **`IfStatementNode`**:
  - `Condition`: Expressão a ser avaliada.
  - `ThenBranch`: Bloco ou comando executado caso a condição seja verdadeira.
  - `ElseBranch`: Bloco ou comando executado caso a condição seja falsa (opcional).
- **`WhileStatementNode`**:
  - `Condition`: Expressão de guarda do laço.
  - `Body`: Bloco ou comando a ser repetido enquanto a condição for verdadeira.

---

### 1.2. Regras Gramaticais no Parser (`Yvira.Parser/Parser.cs`)
- **`ParseIfStatement`**:
  - Reconhece a palavra-chave `if`, com parênteses opcionais ou obrigatórios na condição `if (condicao) { ... } else { ... }`.
  - Encadeamento de `else` opcional.
- **`ParseWhileStatement`**:
  - Reconhece a palavra-chave `while`, condição e corpo.
- **`ParseStatementBlockOrSingle`**:
  - Suporta tanto blocos entre chaves `{ ... }` quanto comandos de instrução única.

---

### 1.3. Execução no Interpretador (`Yvira.Vm/Interpreter.cs`)
- **`ExecuteIfStatement`**:
  - Avalia a condição usando semântica `IsTruthy`.
  - Desvia para o ramo `then` ou ramo `else`.
- **`ExecuteWhileStatement`**:
  - Executa repetidamente o corpo do loop até que a condição resulte em falso, mantendo as atualizações de variáveis e escopo a cada iteração.

---

### 1.4. Validação Prática em `exemplo.yv`
Executado com sucesso via CLI:
```bash
dotnet run --project Yvira.Cli/Yvira.Cli.csproj -- run exemplo.yv
```

**Resultado:**
- Tomada de decisão com `if (idade >= 18)` e `if (saldo >= precoItem) ... else ...`.
- Laço de repetição `while (contador <= 5)` executando 5 iterações ordenadas.
- Acumulador em loop somando de 1 a 10 e resultando no valor matemático correto `55`.
