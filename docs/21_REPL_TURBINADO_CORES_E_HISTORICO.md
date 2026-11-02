# 21 - REPL Turbinado (v2.1) - Destaque ao Vivo, Histórico e Multi-linha

O terminal interativo da linguagem **Yvira** (`yvira repl`) foi totalmente reformulado para oferecer uma experiência moderna, fluida e com visual sofisticado.

---

## 1. Paleta de Cores Implementada

| Elemento Sintático | Exemplos | Cor no Terminal |
| :--- | :--- | :--- |
| **Prompt Primário** | `yvira> ` | **Ciano (`Cyan`)** |
| **Prompt Multi-linha** | `...   ` | **Cinza Escuro (`DarkGray`)** |
| **Palavras-chave de Controle** | `if`, `else`, `while`, `for`, `return`, `try`, `catch`, `throw` | **Magenta / Púrpura (`Magenta`)** |
| **Declarações** | `fn`, `class`, `let`, `const`, `import`, `as` | **Ciano Claro (`Cyan`)** |
| **Valores Especiais** | `this`, `true`, `false`, `null`, `and`, `or`, `not` | **Amarelo Dourado (`Yellow`)** |
| **Strings** | `"Olá"`, `'texto'` | **Verde Suave (`Green`)** |
| **Números** | `42`, `3.14` | **Amarelo Escuro (`DarkYellow`)** |
| **Comentários** | `# anotações...` | **Cinza Escuro (`DarkGray`)** |
| **Operadores e Delimitadores** | `+`, `-`, `*`, `( )`, `[ ]`, `{ }` | **Cinza Claro (`Gray`)** |
| **Resultado Avaliado** | `=> 42`, `=> <Pessoa { ... }>` | **Verde Brilhante (`Green`)** |
| **Erros** | `Erro sintático: ...` | **Vermelho Brilhante (`Red`)** |

---

## 2. Destaque de Sintaxe em Tempo Real

Conforme você digita no terminal, o leitor de linha ([`ReplLineReader.cs`](file:///C:/Users/santh/OneDrive/Área de Trabalho/ARQQUIVOS DRIVE/Beta OS/Yvira.Cli/ReplLineReader.cs)) re-renderiza o texto aplicando o analisador léxico ([`ReplHighlighter.cs`](file:///C:/Users/santh/OneDrive/Área de Trabalho/ARQQUIVOS DRIVE/Beta OS/Yvira.Cli/ReplHighlighter.cs)) caractere por caractere.

---

## 3. Navegação com Setas e Histórico de Comandos

- **Seta para Cima ($\uparrow$)**: Recupera comandos anteriores digitados na sessão.
- **Seta para Baixo ($\downarrow$)**: Avança para os comandos mais recentes do histórico.
- **Seta Esquerda ($\leftarrow$) / Direita ($\rightarrow$)**: Move o cursor pelo meio do texto para edições inline.
- **Home / End**: Salta diretamente para o início ou final da linha.
- **Backspace / Delete**: Apaga caracteres com re-renderização imediata das cores.

---

## 4. Blocos Multi-linha Inteligentes

Se você abrir chaves `{`, parênteses `(` ou colchetes `[` e pressionar `Enter`, o REPL detecta automaticamente que a instrução ainda não está completa e muda para o prompt de continuação `...`:

```yvira
yvira> fn quadrado(n) {
...        return n * n
...    }
=> <fn quadrado>

yvira> quadrado(12)
=> 144
```

---

## 5. Como Iniciar o REPL

No seu terminal:
```bash
yvira repl
# ou diretamente via dotnet:
dotnet run --project Yvira.Cli repl
```