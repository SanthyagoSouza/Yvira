# 14 - Tratamento de Erros e Exceções

A linguagem **Yvira** agora possui um sistema completo e flexível de tratamento de exceções com as palavras-chave `try`, `catch`, `finally` e `throw`.

---

## 1. Bloco `try` / `catch`

Permite capturar erros de tempo de execução (como divisões inválidas, acessos fora de limites em matrizes ou listas, variáveis inexistentes) sem interromper abruptamente o programa:

```yvira
try {
    let lista = [10, 20]
    print(lista[5]) # Índice fora dos limites
} catch (erro) {
    print("Ocorreu um erro:", erro)
}
```

> O parâmetro do `catch` é opcional. Se não precisar da mensagem ou do objeto do erro, use apenas `catch { ... }`.

---

## 2. Bloco `finally`

O bloco `finally` é opcional e garante a execução de limpeza de código, ocorrendo ou não um erro dentro do bloco `try`:

```yvira
try {
    let A = [[1, 2], [3, 4]]
    let inv = A.inverse()
} catch (e) {
    print("Falha ao inverter:", e)
} finally {
    print("Processamento finalizado!")
}
```

---

## 3. Lançamento de Erros Customizados (`throw`)

Você pode interromper o fluxo e disparar exceções próprias passando qualquer valor (strings, números ou objetos/mapas):

```yvira
fn transferir(saldo, valor) {
    if (valor <= 0) {
        throw "O valor da transferência deve ser positivo."
    }
    if (valor > saldo) {
        throw "Saldo insuficiente!"
    }
    return saldo - valor
}

try {
    let novoSaldo = transferir(100, 250)
} catch (falha) {
    print("Transferência rejeitada:", falha)
}
```
