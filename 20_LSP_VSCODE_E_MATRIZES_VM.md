# 20 - Language Server Protocol (LSP) no VS Code & Matrizes na Stack VM

Este marco consolidou duas grandes frentes da linguagem **Yvira**:
1. **Inteligência de Código no VS Code (LSP v0.0.3)**.
2. **Matrizes e Álgebra Linear de Alta Performance na Máquina Virtual de Bytecode (Stack VM)**.

---

## 1. Extensão do VS Code com LSP Integrado (v0.0.3)

O pacote oficial da extensão (`yvira-language-0.0.3.vsix`) agora roda um serviço de inteligência em tempo real:

- **Diagnósticos em Tempo Real**: Valida balanceamento de delimitadores (`()`, `[]`, `{}`) e sublinha erros sintáticos com ondas vermelhas diretamente no editor.
- **Autocomplete Contextual**: Sugestões automáticas de palavras-chave (`fn`, `class`, `this`, `let`, `import`, `try`), módulos nativos (`fs`, `http`) e operações matriciais (`det()`, `inverse()`, `transpose()`, `solve()`).
- **Hover Tooltips**: Ao repousar o cursor sobre palavras-chave, módulos ou métodos, a documentação formatada com exemplos em Markdown é exibida instantaneamente.

### Instalação:
```bash
code --install-extension vscode-extension/yvira-language-0.0.3.vsix --force
```

---

## 2. Matrizes e Coleções Nativas na Bytecode Stack VM

O compilador e a Máquina Virtual de Bytecode agora suportam diretamente:
- **`OpMatrix`**: Alocação de matrizes bidimensionais densas na pilha.
- **`OpArray`**: Vetores e listas dinâmicas.
- **`OpGetIndex`**: Indexação unidimensional `lista[i]` e bidimensional `matriz[r, c]`.
- **`OpInvoke`**: Invocação de métodos e propriedades (`.det()`, `.inv()`, `.T`, `.rows`, `.length`) sem overhead de reflection.
- **Sobrecarga de Operadores no Bytecode**: `A + B`, `A - B`, `A * B` (multiplicação de matrizes) e multiplicação por escalar.

---

## 3. Benchmark: Álgebra Linear (AST vs Stack VM)

Executando [teste_matrizes_vm.yv](teste_matrizes_vm.yv):
```
=== RESULTADO DO BENCHMARK DE PERFORMANCE ===
[1] Interpretador AST (Tree-walk)  : 38 ms
[2] Compilador Bytecode & Stack VM  : 8 ms
Aceleração Obtida                   : 4.8x mais rápido!
```