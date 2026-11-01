# Yvira - Registro de Mudanças: Sistema Nativo de Matrizes e Álgebra Linear (v0.7)

Data: 27 de Outubro de 2026  
Status: Implementado e Validado com Sucesso

---

## 1. O que foi adicionado

### 1.1. Novos Nós na AST (`Yvira.Ast/AstNodes.cs`)
- **`ArrayLiteralNode`**: Declaração literal de vetores ou matrizes aninhadas (`[1, 2, 3]` ou `[[1, 2], [3, 4]]`).
- **`IndexAccessNode`**: Acesso direto por índices simples (`v[0]`), linha completa (`m[0]`) ou bidimensional (`m[0, 1]`).
- **`MemberAccessNode`**: Acesso a propriedades da matriz como `.rows`, `.cols`, `.length`, `.T`.
- **`MethodCallNode`**: Chamada de métodos integrados como `.row(i)`, `.col(j)`, `.transpose()`, `.inverse()`.

---

### 1.2. Regras Gramaticais no Parser (`Yvira.Parser/Parser.cs`)
- Reconhecimento de colchetes `[` e `]` para declaração de listas e matrizes.
- Suporte a indexação encadeada e multidimensional separada por vírgula: `alvo[i, j]`.
- Suporte ao operador de acesso a membro `.` (ponto) para propriedades e métodos.

---

### 1.3. O Motor de Matrizes de Alta Performance (`Yvira.Vm/YviraMatrix.cs`)
Implementado o tipo de primeira classe `YviraMatrix` com recursos científicos e matemáticos:

- **Declaração e Fatiamento**:
  - Vetores 1D ou matrizes 2D multidimensionais.
  - Extração de linha inteira sem `for`: `m.row(0)`
  - Extração de coluna inteira sem `for`: `m.col(1)`
- **Álgebra Linear e Operações**:
  - **Transposição**: `m.transpose()` ou `m.T` (inverte linhas por colunas instantaneamente).
  - **Inversão de Matriz Quadrada**: `m.inverse()` ou `m.inv()` implementada com algoritmo de **Eliminação de Gauss-Jordan com pivoteamento parcial**.
  - **Multiplicação Matricial (Dot Product)**: `A * B` (multiplica linhas por colunas conforme as regras de álgebra linear).
  - **Operações com Escalares**: `m * 2`, `m + 10`, `m - 5` aplicadas a todas as células sem necessidade de laços.
- **Visualização Formatada**:
  - Sobrescrita de `ToString()` que desenha matrizes alinhadas em colunas visuais no console.

---

### 1.4. Validação Prática em `matrizes.yv`
Executado via CLI com 100% de precisão:
```bash
dotnet run --project Yvira.Cli/Yvira.Cli.csproj -- run matrizes.yv
```

**Resultados do Teste:**
1. Declaração de vetor: `v = [10, 20, 30, 40, 50]` com `v.length == 5`.
2. Acesso bidimensional: `m[0, 1] == 2` e `m[1, 2] == 6`.
3. Fatias sem `for`: `m.row(0)` e `m.col(0)`.
4. Transposição `(2x3)` ➔ `(3x2)` correta.
5. Matemática por escalar: `m * 2` e `m + 10`.
6. Multiplicação matricial `m * transposta` ➔ Matriz `(2x2)`:
   ```text
   [ 14, 32 ]
   [ 32, 77 ]
   ```
7. Inversão comprovada com a matriz identidade: `quad * quad.inverse() == Identidade [[1, 0], [0, 1]]`.
