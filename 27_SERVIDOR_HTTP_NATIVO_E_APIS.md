# 27 - Servidor HTTP Nativo e Micro-APIs Ultra-Minimalistas

O Yvira agora possui suporte nativo a servidores HTTP embutidos através do módulo `http`, permitindo criar microserviços e APIs RESTful em pouquíssimas linhas, sem frameworks externos, com serialização JSON automática, suporte a CORS e integração direta com o banco de dados nativo (`@db`, `@exec`, `@query`).

---

## 1. O Servidor Minimalista Direto

Com `http.serve(porta, handler)`, qualquer retorno de mapa ou lista no Yvira é automaticamente serializado como `JSON` com código `200 OK`:

```yvira
import "http";

fn handler(req) {
    return { "mensagem": "Olá mundo do Yvira!", "status": "online" };
}

# Inicia o servidor HTTP bloqueante na porta 8080 (Ctrl+C encerra)
http.serve(8080, handler);
```

---

## 2. Estrutura do Objeto `req`

O parâmetro `req` recebido pelo handler é um mapa contendo todas as informações tratadas:

* `req.method`: Verbo HTTP em maiúsculas (`"GET"`, `"POST"`, `"PUT"`, `"DELETE"`).
* `req.path`: Caminho da requisição (ex: `"/usuarios"`, `"/api/v1/dados"`).
* `req.body`: Corpo da requisição. Se o cliente enviou `JSON`, ele já chega parseado como mapa ou lista nativa do Yvira. Se for texto puro, chega como string.
* `req.query`: Mapa com as query strings da URL (ex: `/busca?filtro=ia` vira `req.query["filtro"]`).
* `req.headers`: Mapa com todos os cabeçalhos HTTP recebidos em minúsculo.

---

## 3. Respostas Customizadas com `http.response(codigo, corpo)`

Para retornar códigos de status específicos (ex: `201 Created`, `404 Not Found`, `400 Bad Request`):

```yvira
import "http";

fn handler(req) {
    if (req.method == "GET" && req.path == "/segredo") {
        return http.response(401, { "erro": "Acesso não autorizado" });
    }

    if (req.method == "GET" && req.path == "/saudacao") {
        return "<h1>Bem-vindo ao Yvira Server!</h1>"; # Retorna HTML nativo
    }

    return http.response(404, { "erro": "Rota inexistente" });
}

http.serve(8080, handler);
```

---

## 4. API REST Completa com Banco de Dados Nativo

Veja a sinergia entre o banco de dados nativo (`@query` / `@exec`) e o servidor HTTP em menos de 25 linhas:

```yvira
import "http";

@db("sqlite://loja.db");
@exec("CREATE TABLE IF NOT EXISTS clientes (id INTEGER PRIMARY KEY AUTOINCREMENT, nome TEXT, saldo REAL);");

fn api(req) {
    # GET /clientes -> Retorna todos os clientes do banco
    if (req.method == "GET" && req.path == "/clientes") {
        return @query("SELECT * FROM clientes;");
    }

    # POST /clientes -> Insere novo cliente seguro contra SQL Injection
    if (req.method == "POST" && req.path == "/clientes") {
        let b = req.body;
        let id = @exec("INSERT INTO clientes (nome, saldo) VALUES (?, ?);", [b["nome"], b["saldo"]]);
        return http.response(201, { "id": id, "status": "criado com sucesso" });
    }

    return http.response(404, { "erro": "Endpoint não encontrado" });
}

http.serve(3000, api);
```

---

## 5. Modo Background e Concorrência com Automação `@time`

O terceiro argumento de `http.serve` aceita opções como `{ "background": true }`.
Isso permite que o servidor HTTP rode em paralelo no mesmo processo com automações periódicas (`@time`):

```yvira
import "http";

let metricas = { "requisicoes": 0 };

fn handler(req) {
    metricas["requisicoes"] = metricas["requisicoes"] + 1;
    return metricas;
}

# Servidor rodando em segundo plano
let server = http.serve(8080, handler, { "background": true });

# Automação periódica que executa a cada 10 segundos
fn salvar_metricas() {
    print("[CRON YVIRA] Total atendido até agora: " + metricas["requisicoes"]);
}
@time.salvar_metricas(10);
```
