# 📚 LIVRO DE RECEITAS & GUIA DEFINITIVO DO DESENVOLVEDOR YVIRA
> *"Para quem idealizou a linguagem e para quem quer construir coisas fodas com ela sem fritar a cabeça."*

Bem-vindo ao **Manual Prático do Yvira**! Este documento foi escrito para ser um **guia rápido de consulta e receitas práticas (Cheat Sheet / Cookbook)**. Sempre que você esquecer como cria uma janela, como conecta no banco, como sobe uma API ou como faz a tela atualizar em tempo real, **é aqui que você olha**.

---

## 📑 ÍNDICE RÁPIDO
1. [Sintaxe Básica, Variáveis e Funções](#1-sintaxe-básica-variáveis-e-funções)
2. [Criando Janelas Desktop com HTML/CSS (`criar_janela`)](#2-criando-janelas-desktop-com-htmlcss-criar_janela)
3. [A Mágica da Comunicação Yvira ↔ JavaScript (Sem JSON manual!)](#3-a-mágica-da-comunicação-yvira--javascript-sem-json-manual)
4. [Criando APIs REST sem Framework (`@http.servidor`)](#4-criando-apis-rest-sem-framework-httpservidor)
5. [Banco de Dados Nativo (`@db`, `@exec`, `@query`)](#5-banco-de-dados-nativo-db-exec-query)
6. [Temporizador em Segundo Plano sem Travar a Tela (`@time`)](#6-temporizador-em-segundo-plano-sem-travar-a-tela-time)
7. [Realtime Multiplayer e WebSockets (`@realtime`)](#7-realtime-multiplayer-e-websockets-realtime)
8. [Gerando Executável Independente (.exe) (`yvira build`)](#8-gerando-executável-independente-exe-yvira-build)

---

## 1. Sintaxe Básica, Variáveis e Funções

```yvira
# Comentário começa com cerquilha (#)
let nome = "Yvira";
let versao = 2.0;
let ativo = true;

# Listas e Mapas (Dicionários)
let frutas = ["Maçã", "Banana", "Uva"];
frutas.push("Morango");
frutas.insert(0, "Abacaxi"); # Insere no topo

let usuario = {
    "id": 1,
    "nome": "Santhyago",
    "saldo": 1500.50
};

# Funções
fn somar(a, b) {
    return a + b;
}

# Loops
let i = 0;
while (i < 3) {
    print("Contagem: " + i);
    i = i + 1;
}

# Interpolação de String moderna ($)
print($"Linguagem {nome} rodando na versão {versao}!");
```

---

## 2. Criando Janelas Desktop com HTML/CSS (`criar_janela`)

Para criar uma janela nativa com WebView2 de alta performance:

```yvira
let app = criar_janela({
    "titulo": "Meu App Incrível",
    "view": "views/index.html",     # Caminho relativo do HTML
    "largura": 1100,
    "altura": 700,
    "barra_customizada": true,      # Remove a barra feia do Windows
    "cor_fundo": "#0f172a"
});

# Exibe e entra no loop de eventos
app.exibir();
app.esperar();
```

---

## 3. A Mágica da Comunicação Yvira ↔ JavaScript (Sem JSON manual!)

Esqueça `json_encode`, `json_decode`, `fetch` e `JSON.parse`.

### ➡️ Do Yvira para a Janela (Yvira manda, JS recebe):
**No `main.yv`:**
```yvira
let dados = { "saldo": 500, "status": "Aprovado" };
app.emitir("receber_saldo", dados);
```
**No `views/index.html` (JavaScript):**
```javascript
window.receber_saldo = function(dados) {
    console.log(dados.saldo);  // 500 (Já chega como Objeto JS puro!)
    document.getElementById('valor').innerText = "R$ " + dados.saldo;
};
```

### ⬅️ Da Janela para o Yvira (JS clica, Yvira processa):
**No `views/index.html` (JavaScript):**
```javascript
// Dispara quando clica num botão HTML
function salvarPerfil() {
    window.yvira.emit("salvar_dados", { nome: "Alan", idade: 28 });
}
```
**No `main.yv`:**
```yvira
fn aoSalvar(dados) {
    print("Nome recebido da tela: " + dados.nome); # Já é um Map!
}
app.ao_evento("salvar_dados", aoSalvar);
```

---

## 4. Criando APIs REST sem Framework (`@http.servidor`)

Você não precisa de Express, Flask ou Laravel. O Yvira já é um servidor HTTP nativo com firewall e suporte a JSON automático:

```yvira
let api = @http.servidor(8080);

# Rota GET
fn listar(req) {
    return {
        "sucesso": true,
        "usuarios": ["Alice", "Bob", "Carlos"]
    };
}
api.ao("GET /usuarios", listar);

# Rota POST
fn criar(req) {
    # req.body já é um Map decodificado!
    let nome = req.body.nome;
    
    return {
        "status": 201,
        "mensagem": "Usuário " + nome + " cadastrado com sucesso!"
    };
}
api.ao("POST /usuarios", criar);

api.iniciar();
print("API ativa em http://localhost:8080");

# Mantém o processo vivo
while (true) { dormir(1000); }
```

---

## 5. Banco de Dados Nativo (`@db`, `@exec`, `@query`)

O banco SQLite já vem embutido na linguagem. Sem drivers externos:

```yvira
# Conecta ao arquivo ou usa ":memory:"
@db("sqlite://dados.db");

# Criar tabelas e inserir dados com proteção contra SQL Injection (?)
@exec("CREATE TABLE IF NOT EXISTS clientes (id INTEGER PRIMARY KEY, nome TEXT, saldo REAL);");
@exec("INSERT INTO clientes (nome, saldo) VALUES (?, ?);", ["Santhyago", 2500.0]);

# Consultar dados (Retorna uma Lista de Mapas!)
let lista = @query("SELECT id, nome, saldo FROM clientes;");

for (c in lista) {
    print($"Cliente: {c.nome} | Saldo: R$ {c.saldo}");
}
```

---

## 6. Temporizador em Segundo Plano sem Travar a Tela (`@time`)

Quer que um código rode a cada 1 segundo (tipo um painel de ações, bot trader ou dashboard) sem congelar a janela?

```yvira
fn atualizarPainel() {
    let cotacao = random(500, 600);
    app.emitir("nova_cotacao", { "valor": cotacao });
}

# Roda a função a cada 1.0 segundo em background!
@time.atualizarPainel(1.0);
```

---

## 7. Realtime Multiplayer e WebSockets (`@realtime`)

Para criar chats, jogos de clique ou ferramentas estilo Discord com criptografia militar AES-256:

### O Servidor:
```yvira
let servidor = @realtime.servidor({ "porta": 7070, "segredo": "CHAVE_SECRETA_2026" });

fn aoMensagem(cliente, dados) {
    # Manda para todo mundo na sala "geral"
    servidor.para("geral").emitir("nova_msg", dados);
}
servidor.ao("enviar_msg", aoMensagem);

servidor.iniciar();
```

### O Cliente:
```yvira
let rede = @realtime.cliente({ "endereco": "ws://127.0.0.1:7070", "segredo": "CHAVE_SECRETA_2026" });

rede.ao("nova_msg", fn(msg) {
    print("Mensagem recebida: " + msg.texto);
});

rede.conectar();
rede.emitir("enviar_msg", { "texto": "Salve galera!" });
```

---

## 8. Gerando Executável Independente (.exe) (`yvira build`)

Quer entregar seu app para um cliente ou amigo sem ele precisar instalar nada?

No terminal da pasta do seu projeto:
```powershell
yvira build main.yv --output MeuApp.exe
```

O compilador vai:
1. Empacotar o script `.yv` e a pasta `views/` inteira (HTML, CSS, imagens).
2. Carimbar o ícone oficial do Yvira (`.ico`) no executável.
3. Gerar um arquivo `.exe` único e portátil que roda no Windows com dois cliques!
