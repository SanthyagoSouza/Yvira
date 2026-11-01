# 29. Módulo Nativo de Cache em Memória (High-Throughput In-Memory Engine)

O módulo nativo `cache` do Yvira foi projetado para cenários de altíssimo volume e concorrência extrema (ex: ledger financeiro, contadores de métricas, controle de saldo PIX e sessões de alta velocidade), onde operações síncronas de escrita em bancos relacionais como SQLite se tornam um gargalo de concorrência (*lock contention*).

Com o modelo **In-Memory State + Batch Flush (Write-Behind)**, o sistema processa operações de débito, crédito e validações em microssegundos na RAM e realiza persistência assíncrona agrupada (`@transaction`) no banco de dados.

---

## 1. Importação e Métodos Básicos

```yvira
import "cache";

# Armazenamento simples
cache.set("cliente:1:saldo", 5000.0);

# Leitura com fallback default
let saldo = cache.get("cliente:1:saldo", 0.0);
print("Saldo atual: " + saldo);

# Verificação de existência
if (cache.has("cliente:1:saldo")) {
    print("Chave existe no cache!");
}

# Remoção
cache.remove("cliente:1:saldo"); # ou cache.delete(...)
```

---

## 2. Operações Aritméticas Atômicas Concorrentes

Para sistemas bancários ou contadores de requisições, o cache oferece incremento e decremento *thread-safe* sem locks manuais:

```yvira
import "cache";

# Inicializa saldo
cache.set("conta:100", 1500.0);

# Crédito atômico
let novoSaldo = cache.incr("conta:100", 250.0); # 1750.0

# Débito atômico
let saldoAposDebito = cache.decr("conta:100", 100.0); # 1650.0
```

---

## 3. TTL (Tempo de Expiração Automática)

É possível definir tempo de vida em segundos para chaves voláteis (ex: tokens de autenticação, OTPs ou rate-limiters):

```yvira
import "cache";

# Chave expira automaticamente após 60 segundos
cache.set("otp:usuario:42", "981245", 60);
```

---

## 4. Stores Isoladas (Namespaces / Buckets)

Para manter tabelas ou domínios separados em memória sem colisão de chaves:

```yvira
import "cache";

let ledgerCache = cache.store("ledger_pix");
let sessionCache = cache.store("sessions");

ledgerCache.set("tx:1", { "de": 1, "para": 2, "valor": 50.0 });
sessionCache.set("user:1", { "token": "abc", "role": "admin" });
```

---

## 5. Padrão Arquitetural para Volume Financeiro: Write-Behind

```yvira
import "http";
import "cache";

@db("sqlite://banco.db");

# 1. Carrega saldos do banco para o cache nativo no startup
let contas = @query("SELECT id, saldo FROM contas;");
foreach (c in contas) {
    cache.set("saldo:" + c["id"], c["saldo"]);
}

# 2. Handler HTTP processa instantaneamente em memória
fn api_pix(req) {
    let de = req.body["de"];
    let para = req.body["para"];
    let valor = req.body["valor"];

    if (cache.get("saldo:" + de) < valor) {
        return http.response(422, { "erro": "Saldo insuficiente" });
    }

    cache.decr("saldo:" + de, valor);
    cache.incr("saldo:" + para, valor);

    return { "sucesso": true };
}

http.serve(8080, api_pix, { "background": true });

# 3. Flush periódico ou final em uma única transação atômica
@transaction {
    @exec("UPDATE contas SET saldo = ? WHERE id = 1;", [cache.get("saldo:1")]);
    @exec("UPDATE contas SET saldo = ? WHERE id = 2;", [cache.get("saldo:2")]);
}
```
