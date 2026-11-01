# ⚡ Concorrência Nativa em Yvira: `@spawn` e `@worker`

O ecossistema Yvira conta com suporte nativo de primeira classe para **concorrência assíncrona** e **paralelismo multicore**, aproveitando o poder da infraestrutura multithreading do runtime com isolamento de ambiente (State Isolation) entre tarefas.

---

## 1. `@spawn` — Tarefas Assíncronas Não-Bloqueantes

O `@spawn` permite disparar tarefas em background sem travar a thread principal da aplicação, devolvendo um **Task Handle** controlável.

### Sintaxe Básica
```yvira
fn tarefaDemorada(id) {
    sleep(1000)
    return "Processo " + id + " finalizado!"
}

# Dispara a execução assíncrona
let tarefa = @spawn.tarefaDemorada(42)

# Verifica se terminou sem bloquear
if (!tarefa.isDone()) {
    print("A tarefa continua executando em background...")
}

# Aguarda o resultado e recupera o valor
let resultado = tarefa.wait()
print(resultado)
```

### Métodos Disponíveis no Objeto `tarefa`
| Método | Descrição |
| :--- | :--- |
| `tarefa.wait()` | Bloqueia a execução até que a tarefa assíncrona termine e retorna o resultado. |
| `tarefa.wait(segundos)` | Aguarda até o tempo limite (ex: `0.5` para 500ms). Se estourar o tempo, lança exceção capturável via `try/catch`. |
| `tarefa.isDone()` | Retorna `true` se a tarefa já foi concluída, caso contrário `false`. |
| `tarefa.cancel()` | Cancela a execução da tarefa assíncrona cooperativamente via `CancellationToken`. |

### Exemplo com Timeout:
```yvira
let tarefa = @spawn.operacaoLenta()

try {
    # Aguarda no máximo 2 segundos
    let dados = tarefa.wait(2.0)
    print("Dados recebidos:", dados)
} catch (erro) {
    print("Tempo esgotado! Cancelando tarefa:", erro)
    tarefa.cancel()
}
```

---

## 2. `@worker` — Pool de Processamento Paralelo Multicore

O `@worker` provê processamento massivo distribuído em múltiplos núcleos de CPU utilizando partições e cópias isoladas de interpretadores, evitando contenção de locks ou deadlocks globais.

### Sintaxe Básica
```yvira
# Cria um pool com 8 workers (núcleos de execução paralela)
let pool = @worker.pool(8)

print("Workers ativos:", pool.concurrency)

fn calcular(item) {
    return item * 10
}

let dados = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]

# Mapeia a lista inteira distribuindo entre os workers
let resultados = pool.map(dados, calcular)
print("Resultados:", resultados)

# Finaliza os workers liberando recursos
pool.close()
```

### Propriedades e Métodos do `pool`
| Membro | Descrição |
| :--- | :--- |
| `pool.map(colecao, funcao)` | Divide a coleção (Listas ou Matrizes) entre os workers em paralelo e devolve uma nova `YviraList` com os itens processados ordenados. |
| `pool.concurrency` | Número de workers em paralelo alocados no pool. Se não especificado em `@worker.pool()`, utiliza automaticamente `ProcessorCount` da máquina. |
| `pool.close()` | Encerra o pool de workers e libera os recursos alocados. |

---

## 3. Isolamento de Estado (State Isolation)

Tanto o `@spawn` quanto os workers gerados pelo `@worker.pool` operam sobre clones isolados de ambiente (`CreateWorkerClone()`). Isso garante que:
- Variáveis locais não sofrem corrupção de memória ou race conditions cruzadas.
- O acesso a conexões de bancos de dados gerenciadas (`DatabaseManager`) é thread-safe.
- A legibilidade e a semântica declarativa do Yvira permanecem limpas e elegantes.
