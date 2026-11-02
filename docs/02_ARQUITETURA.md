                Atlas Toolchain

                    CLI
                     │
      ┌──────────────┼──────────────┐
      │              │              │
  Formatter        Build         Package
      │              │              │
      └──────────────┼──────────────┘
                     │
                 Compiler
                     │
        ┌────────────┼────────────┐
        │            │            │
      Lexer       Parser      Semantic
        │            │            │
        └────────────┼────────────┘
                     │
                    AST
                     │
                  Optimizer
                     │
                  Atlas IR
                     │
             Bytecode Generator
                     │
                 Atlas Bytecode
                     │
                 Atlas Runtime
                     │
                  Atlas VM
                     │
          Windows/Linux/macOS




Tipos de arquivos são chamados de .beta

Organização do Repositorio

atlas/

compiler/

lexer/

parser/

ast/

semantic/

types/

optimizer/

ir/

bytecode/

vm/

runtime/

cli/

formatter/

lsp/

package/

sdk/

examples/

tests/

docs/


Compilador

Source Code

↓

Tokens

↓

AST

↓

Semantic Analysis

↓

Type Checking

↓

Atlas IR

↓

Optimization

↓

Bytecode

↓

VM




| Área                       | Escolha                            |
| -------------------------- | ---------------------------------- |
| Linguagem de implementação | **Rust**                           |
| Arquitetura                | **Monorepo + Cargo Workspace**     |
| Paradigma                  | **Estado + Eventos + Componentes** |
| Parser                     | **Recursivo escrito à mão**        |
| IR                         | **Atlas IR próprio**               |
| Execução inicial           | **Interpretador**                  |
| Execução futura            | **Atlas Bytecode + Atlas VM**      |
| Runtime                    | **Próprio**                        |
| Build                      | **CLI `atlas`**                    |
| IDE inicial                | **VS Code + LSP**                  |
| Bytecode                   | **Formato próprio**                |
| Repositório                | **GitHub Monorepo**                |
