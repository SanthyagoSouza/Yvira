# 26 - Suporte Nativo a Banco de Dados e Multi-Conexões

O Yvira agora suporta operações de banco de dados diretamente na sintaxe da linguagem através de diretivas nativas com o operador `@`, eliminando a verbosidade de bibliotecas externas, instâncias de conexão e garantindo proteção automática contra **SQL Injection**.

---

## 1. Conexão Universal: `@db` e Multi-Bancos com `@db.<alias>`

Você registra as conexões no início ou em qualquer parte do script. Se tiver apenas um banco, usa `@db`. Se tiver múltiplos bancos (ex: produção e logs/analytics), adiciona o alias:

```yvira
# Conexão principal padrão:
@db("sqlite://app.db");

# Conexão em memória (útil para testes rápidos e cache):
@db("sqlite://:memory:");

# Conexão secundária nomeada:
@db.analytics("sqlite://analytics.db");
```

---

## 2. Execução DDL / DML com `@exec` e `@exec.<alias>`

Usado para `CREATE TABLE`, `INSERT`, `UPDATE`, `DELETE`.
Retorna o número de linhas afetadas ou o ID gerado da inserção:

```yvira
# Criação de tabela
@exec("CREATE TABLE usuarios (id INTEGER PRIMARY KEY, nome TEXT, email TEXT, saldo REAL);");

# Inserção segura com anti-SQL injection via lista de parâmetros:
fn criar_usuario(nome, email, saldo) {
    let id = @exec("INSERT INTO usuarios (nome, email, saldo) VALUES (?, ?, ?);", [nome, email, saldo]);
    return id;
}

let novo_id = criar_usuario("Santhyago", "santhyago@yvira.dev", 15000.0);
```

Executando no banco nomeado (`analytics`):
```yvira
@exec.analytics("INSERT INTO logs (evento, data) VALUES (?, ?);", ["Usuario criado", "2026-10-27"]);
```

---

## 3. Consultas Seguras com `@query` e `@query.<alias>`

Retorna uma lista de mapas/dicionários indexáveis por nome de coluna (`u["nome"]` ou `u.nome`):

```yvira
fn buscar_por_saldo(corte) {
    return @query("SELECT id, nome, email, saldo FROM usuarios WHERE saldo >= ? ORDER BY saldo DESC;", [corte]);
}

let vip = buscar_por_saldo(10000.0);
foreach (u in vip) {
    print("Cliente: " + u["nome"] + " | Saldo: R$ " + u["saldo"]);
}

# Consultando o banco secundário:
let relatorio = @query.analytics("SELECT * FROM logs;");
```

---

## 4. Proteção Nativa Anti-SQL Injection

A lista de parâmetros passada no segundo argumento é tratada via **Prepared Statements** pelo driver nativo:

```yvira
# Tentativa de injeção maliciosa:
let entrada_perigosa = "'; DROP TABLE usuarios; --";

# O Yvira sanitiza os dados sem concatenar strings na query:
let res = @query("SELECT * FROM usuarios WHERE nome = ?;", [entrada_perigosa]);
# Resultado: 0 registros retornados e a tabela permanece intacta!
```
