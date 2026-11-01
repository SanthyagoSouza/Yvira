# Yvira - Registro de Mudanças: Entrada de Dados (Input) e Biblioteca Padrão (v0.6)

Data: 27 de Outubro de 2026  
Status: Implementado e Validado com Sucesso

---

## 1. O que foi adicionado

### 1.1. Entrada e Saída Interativa (`Yvira.Vm/Interpreter.cs`)
- **`input(prompt)`**:
  - Exibe opcionalmente uma mensagem de prompt no terminal sem quebra de linha.
  - Bloqueia e lê a entrada digitada pelo usuário até o pressionamento de `Enter`.
  - Retorna o texto capturado como `String`.
- **Injeção de I/O Configurável**:
  - `Interpreter` agora aceita delegados customizados `_outputHandler` e `_inputHandler`, permitindo que testes automatizados injetem entradas ou que GUIs redirecionem a entrada/saída.

---

### 1.2. Utilitários e Funções da Biblioteca Padrão (StdLib)
Foram adicionadas diretamente no runtime nativo da Yvira as seguintes funções globais:

| Função | Parâmetros | Retorno | Descrição |
| :--- | :--- | :---: | :--- |
| **`input(prompt)`** | `prompt: String?` | `String` | Lê linha do console do usuário. |
| **`print(...)` / `println(...)`** | `...args` | `null` | Exibe múltiplos valores formatados no console. |
| **`toNumber(str)` / `to_number(str)`** | `str: Any` | `Float` | Converte textos ou valores para número (suporta formatos com ponto e vírgula). |
| **`toString(val)` / `to_string(val)`** | `val: Any` | `String` | Converte qualquer tipo de dado em representação textual. |
| **`random(min, max)`** | `min: Int, max: Int` | `Float` | Gera números inteiros aleatórios no intervalo inclusivo `[min, max]`. |
| **`clock()` / `time()`** | *nenhum* | `Float` | Retorna o timestamp UTC atual em milissegundos (ideal para benchmark). |

---

### 1.3. Scripts de Teste e Demonstração Criados

1. **`teste_stdlib.yv`**:
   - Mede tempo de execução em milissegundos com `clock()`.
   - Gera número aleatório com `random(10, 50)`.
   - Converte para string e de volta para número, somando `+ 100`.
   - Execução:
     ```text
     Numero aleatorio gerado: 29
     Convertido para String: 29
     Convertido de volta para Numero: 29
     Soma com +100: 129
     Tempo de execucao (ms): 4
     Biblioteca padrao operacional com sucesso!
     ```

2. **`jogo.yv`**:
   - Um jogo interativo de adivinhação 100% escrito na sintaxe Yvira.
   - Pede o nome do jogador com `input("Qual o seu nome? ")`.
   - Sorteia um número secreto com `random(1, 10)`.
   - Executa loop `while (not acertou)` fornecendo dicas de *"MAIOR"* ou *"MENOR"* até o acerto.
