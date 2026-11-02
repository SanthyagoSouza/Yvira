# Yvira - Registro de Mudanças: Super Motor Matricial & Científico (v0.8)

Data: 27 de Outubro de 2026  
Status: Implementado e Validado com Sucesso

---

## 1. O que foi adicionado

### 1.1. Geradores Rápidos de Matrizes (Funções Globais)
Eliminam a necessidade de laços para alocação e inicialização:
- **`zeros(linhas, colunas)` / `zeros(tamanho)`**: Cria matriz preenchida com zeros.
- **`ones(linhas, colunas)` / `ones(tamanho)`**: Cria matriz preenchida com uns.
- **`eye(tamanho)` / `identity(tamanho)`**: Cria a matriz identidade com diagonal principal igual a 1.
- **`randMat(linhas, colunas, [min], [max])`**: Gera matriz com números aleatórios dentro do intervalo desejado.

---

### 1.2. Álgebra Linear Avançada e Sistemas de Equações
- **Cálculo de Determinante (`m.det()` ou `m.determinant()`)**:
  - Implementado algoritmo de eliminação gaussiana com pivoteamento e inversão de sinal por troca de linhas.
- **Traço da Matriz (`m.trace`)**:
  - Retorna a soma direta dos elementos da diagonal principal.
- **Resolução de Sistemas Lineares (`solve(A, b)` ou `A.solve(b)`)**:
  - Resolve equações do tipo $A \cdot x = b$.
  - Converte automaticamente vetores linha em vetores coluna e calcula o vetor $x$ exato por multiplicação da inversa.

---

### 1.3. Estatísticas e Reduções sem Laços (`FOR`-less)
Propriedades e métodos diretos para agregação de dados em matrizes e vetores:
- **`m.sum`**: Soma de todos os elementos.
- **`m.mean`**: Média aritmética de todos os elementos.
- **`m.max`**: Maior valor da matriz/vetor.
- **`m.min`**: Menor valor da matriz/vetor.

---

### 1.4. Validação Prática em `matrizes_turbinadas.yv`
Executado via CLI com 100% de precisão matemática:
```bash
dotnet run --project Yvira.Cli/Yvira.Cli.csproj -- run matrizes_turbinadas.yv
```

**Resultados dos Cálculos:**
1. **Geradores**: `zeros(2, 3)`, `ones(3, 3)`, `eye(3)` e `randMat(...)` alocados instantaneamente.
2. **Determinante**: Matriz 3x3 com determinante calculado exatamente em `-306`.
3. **Traço**: Soma da diagonal principal igual a `11` ($6 + (-2) + 7$).
4. **Estatísticas**: Em matriz de dados `[10, 20, 30; 40, 50, 60]` ➔ Soma: `210`, Média: `35`, Max: `60`, Min: `10`.
5. **Sistema Linear Resolvido**:
   $$\begin{cases} 2x + y = 8 \\ x - 3y = -10 \end{cases}$$
   Yvira calculou exatamente $x = 2$ e $y = 4$, com comprovação matricial exata.
