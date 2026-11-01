# 19 - Funções e Recursão na Máquina Virtual de Bytecode (Call Frames)

A Máquina Virtual de Bytecode (Stack VM) da linguagem **Yvira** agora possui suporte completo a **Call Frames**, permitindo compilar e executar funções de usuário, passagem de parâmetros por valor e recursão de profundidade.

---

## 1. Arquitetura de Execução de Funções (`CallFrame`)

Cada chamada de função aloca um quadro de ativação leve (`CallFrame`) sobre a pilha contígua de dados da VM:

```
  Pilha da VM (_stack):
  [ ... escopo global ... ]
  [ <fn fatorial> ]        <-- frame.StackOffset
  [ arg: n = 5    ]        <-- slot 1 (variável local)
  [ ... temporários ... ]  <-- topo da pilha (_sp)
```

### Opcodes de Chamada e Retorno:
- `OpCall`:
  1. Verifica se o topo da pilha é uma função compilada (`CompiledFunction`).
  2. Valida a aridade (número de parâmetros esperados vs recebidos).
  3. Cria o novo `CallFrame` com offset apontando para os argumentos já empilhados.
- `OpReturn`:
  1. Desempilha o valor de retorno.
  2. Desaloca todos os slots de memória do frame anterior (`_sp = frame.StackOffset`).
  3. Empilha o valor de retorno no topo e restaura o frame chamador.

---

## 2. Exemplo Prático em Yvira

```yvira
fn somar(a, b) {
    return a + b
}

fn fatorial(n) {
    if (n <= 1) {
        return 1
    }
    return n * fatorial(n - 1)
}

let res = somar(10, 20)
print("Soma:", res)

let fat = fatorial(6)
print("Fatorial de 6:", fat) # 720
```

---

## 3. Benchmark de Execução (AST vs Stack VM)

Executando o script recursivo com `yvira bench teste_funcoes_vm.yv`:

```
=== RESULTADO DO BENCHMARK DE PERFORMANCE ===
[1] Interpretador AST (Tree-walk)  : 9 ms
[2] Compilador Bytecode & Stack VM  : 2 ms
Aceleração Obtida                   : 4.5x mais rápido!
```