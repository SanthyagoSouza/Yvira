# Yvira - Registro de Mudanças: Execução de Arquivos .yv, CLI e Saída com Print (v0.3)

Data: 27 de Outubro de 2026  
Status: Implementado e Validado com Sucesso

---

## 1. O que foi adicionado

### 1.1. Chamadas de Função na AST e Parser (`CallExpressionNode`)
- **AST (`Yvira.Ast/AstNodes.cs`)**:
  - Criado o nó `CallExpressionNode(IdentifierNode Callee, IReadOnlyList<ExpressionNode> Arguments)`.
- **Parser (`Yvira.Parser/Parser.cs`)**:
  - Detecção sintática de chamadas de função após identificadores: `nome(arg1, arg2, ...)`.
  - Tratamento de argumentos separados por vírgula e parênteses vazios `()`.

---

### 1.2. Função Nativa `print(...)` no Interpretador (`Yvira.Vm/Interpreter.cs`)
- Adicionada avaliação de chamadas de função no interpretador.
- Implementação da função global nativa `print(...)` (e `println(...)`):
  - Concatena todos os argumentos passados com espaço entre eles.
  - Imprime no fluxo de saída padrão configurado (console).
  - Suporta qualquer tipo de dado (números, strings, booleanos, expressões complexas).

---

### 1.3. Suporte a Números com Ponto Flutuante no Lexer (`Yvira.Lexer/Lexer.Tokens.cs`)
- Atualizado o reconhecedor de números para suportar casas decimais e floats (ex: `0.10`, `3.1415`).

---

### 1.4. Reformulação da Interface CLI (`Yvira.Cli/Program.cs`)
O CLI agora suporta 3 modos de operação completos:
1. **Executar arquivo**:
   ```bash
   dotnet run --project Yvira.Cli/Yvira.Cli.csproj -- run exemplo.yv
   ```
2. **Modo Interativo (REPL / Terminal Yvira)**:
   ```bash
   dotnet run --project Yvira.Cli/Yvira.Cli.csproj -- repl
   ```
3. **Bateria de testes interna**:
   ```bash
   dotnet run --project Yvira.Cli/Yvira.Cli.csproj -- test
   ```

---

### 1.5. Arquivo de Demonstração Criado (`exemplo.yv`)
Criado o primeiro arquivo fonte real da linguagem Yvira contendo:
- Saídas formatadas com `print(...)`.
- Declarações de variáveis (`let criador`, `let salarioBase`, etc.).
- Expressões matemáticas com parênteses e números decimais (`salarioLiquido = (salarioBase + bonus) - (salarioBase * imposto)`).
- Comparações lógicas e operador `and`.
- Comentários de linha usando `#`.

**Resultado da Execução Real:**
```text
========================================
   BEM-VINDO AO PROGRAMA FONTE YVIRA!   
========================================
Criado por: Santhyago
Ano: 2026
Versao: 0.2 Alpha

--- TESTE DE MATEMATICA E PRECEDENCIA ---
Salario Base: 3500
Bonus: 500
Salario Liquido Calculado: 3650

--- TESTE DE STRINGS E BOOLEANOS ---
Status aprovado? True

Fim da execucao com 100% de sucesso!
```
