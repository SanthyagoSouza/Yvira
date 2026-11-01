# 📘 Guia Oficial & Tutorial Completo da Linguagem Yvira
**Aprenda a Programar em Yvira do Zero ao Avançado**

**Autor & Criador:** Santhyago Souza Ponciano 🇧🇷  
**Versão Atual:** 2.1.0  
**Propósito:** Backend Moderno, Inteligência Artificial, APIs, Dados e Automação Nativa.

---

## 📑 Sumário do Tutorial

1. [Introdução: O Que é e Por Que o Yvira Existe?](#1-introdução-o-que-é-e-por-que-o-yvira-existe)
2. [Instalação e Primeiros Passos com o CLI](#2-instalação-e-primeiros-passos-com-o-cli)
3. [Fundamentos: Variáveis, Tipos e Operadores](#3-fundamentos-variáveis-tipos-e-operadores)
4. [Controle de Fluxo: Decisões e Laços](#4-controle-de-fluxo-decisões-e-laços)
5. [Funções, Parâmetros e Recursão](#5-funções-parâmetros-e-recursão)
6. [Coleções: Listas e Mapas (Dicionários)](#6-coleções-listas-e-mapas-dicionários)
7. [Orientação a Objetos: Classes, Construtor e Métodos](#7-orientação-a-objetos-classes-construtor-e-métodos)
8. [Banco de Dados Nativo com `@db`, `@exec` e `@query`](#8-banco-de-dados-nativo-com-db-exec-e-query)
9. [Automação Temporal sem Cron: `@time` e `@stop`](#9-automação-temporal-sem-cron-time-e-stop)
10. [Super Motor Matricial e Álgebra Linear (IA & Dados)](#10-super-motor-matricial-e-álgebra-linear-ia--dados)
11. [Módulos do Sistema e Rede: `fs` e `http`](#11-módulos-do-sistema-e-rede-fs-e-http)
12. [Importações Nativas: `.yv`, `.env` e `.json`](#12-importações-nativas-yv-env-e-json)
13. [Tratamento de Exceções: `try`, `catch`, `finally` e `throw`](#13-tratamento-de-exceções-try-catch-finally-e-throw)
14. [Interfaces Gráficas e Desktop Híbrido (`criar_janela` e WebView2)](#14-interfaces-gráficas-e-desktop-híbrido)
15. [Superpoder Realtime Multiplayer (`@realtime`)](#15-superpoder-realtime-multiplayer-realtime)
16. [Compilador de Executáveis Standalone (`yvira build`)](#16-compilador-de-executáveis-standalone-yvira-build)
17. [Projeto Completo: Criando uma API de Backend com Banco e Bot](#17-projeto-completo-criando-uma-api-de-backend-com-banco-e-bot)

---

## 1. Introdução: O Que é e Por Que o Yvira Existe?

### ❓ O Problema do Ecossistema Atual
No desenvolvimento moderno de backend, rotinas de dados e IA, os programadores enfrentam um dilema clássico:
* **Node.js / Python:** Exigem instalar dezenas de pacotes (`npm install express mysql2 axios dotenv node-cron`, `pip install numpy psycopg2 requests`) apenas para levantar um serviço simples com banco e rotina periódica.
* **C++ / Rust / C#:** São extremamente rápidos, mas o código é verboso e burocrático para prototipar pipelines e rotinas de automação.

### 💡 A Solução Yvira
O Yvira foi desenhado para eliminar a "fadiga de pacotes". Ele já traz no seu **núcleo de linguagem**:
* Consultas SQL parametrizadas nativas (`@query` e `@exec`).
* Agendamentos temporais reativos sem cron (`@time`).
* Matrizes e tensores nativos na sintaxe para machine learning.
* Leitura de arquivos `.env` e `.json` sem bibliotecas externas.

---

## 2. Instalação e Primeiros Passos com o CLI

O Yvira funciona como um binário nativo global no terminal (`yvira.exe`).

### Comandos Essenciais do Terminal:

```powershell
# 1. Verificar a versão instalada
yvira info

# 2. Abrir o console interativo com coloração ao vivo
yvira repl

# 3. Inicializar um novo projeto organizado
yvira init meu_backend
cd meu_backend

# 4. Rodar o projeto
yvira run

# 5. Atualizar o Yvira automaticamente
yvira upgrade
```

---

## 3. Fundamentos: Variáveis, Tipos e Operadores

### Comentários
No Yvira usamos `#` para linha única e `#- ... -#` para blocos:
```yvira
# Este é um comentário de linha única
#-
   Este é um bloco
   de comentário longo
-#
```

### Variáveis (`let`) e Constantes (`const`)
```yvira
# Variável mutável (pode mudar de valor depois)
let total = 100;
total = total + 50;

# Constante (não pode ser alterada)
const VERSAO_API = "v1.0.0";
const TAXA_PADRAO = 0.05;
```

### Tipos de Dados Primitivos
* **Números:** `let idade = 25; let pi = 3.14159;`
* **Textos (Strings):** `let usuario = "Santhyago";`
* **Booleanos:** `let autenticado = true; let pendente = false;`
* **Nulo:** `let resposta = null;`

### Operadores Aritméticos
```yvira
let a = 10;
let b = 3;

let soma = a + b;       # 13
let sub  = a - b;       # 7
let mult = a * b;       # 30
let div  = a / b;       # 3.333333...
let mod  = a % b;       # 1 (resto)
let exp  = a ** b;      # 1000 (potência)

# Atribuição abreviada e incremento
a += 5;                 # 15
a++;                    # 16
```

### Operadores de Comparação e Lógica
O Yvira oferece a **estratégia das duas formas**: você pode usar símbolos matemáticos (`&&`, `||`, `!`) ou palavras legíveis em inglês (`and`, `or`, `not`):
```yvira
let x = 15;
let y = 8;

# Comparação
print(x == y);          # false
print(x != y);          # true (ou use <>)
print(x > y);           # true
print(x <= 20);         # true

# Lógica com palavras (estilo Python/clean code)
if (x > 10 and y < 10) {
    print("Atende com 'and'");
}

# Lógica com símbolos (estilo C/Java/JS)
if (x > 10 && y < 10) {
    print("Atende com '&&'");
}

if (not false or true) {
    print("Suporte flexível!");
}
```

---

## 4. Controle de Fluxo: Decisões e Laços

### Condicionais (`if`, `else if`, `else`)
```yvira
let saldo = 1250.0;

if (saldo >= 5000.0) {
    print("Cliente VIP");
} else if (saldo > 0.0) {
    print("Cliente Padrão");
} else {
    print("Conta Negativa");
}
```

### Laço `while` (Enquanto)
```yvira
let tentativas = 0;
while (tentativas < 3) {
    print("Tentativa #" + tentativas);
    tentativas = tentativas + 1;
}
```

### Laço `for` (Contador)
```yvira
for (let i = 0; i < 5; i = i + 1) {
    if (i == 2) {
        continue; # pula para a próxima iteração
    }
    if (i == 4) {
        break;    # interrompe o laço
    }
    print("Passo: " + i);
}
```

### Laço `foreach` (Iterador de Coleções)
```yvira
let linguagens = ["Yvira", "Python", "Rust", "C#"];

foreach (lang in linguagens) {
    print("Tecnologia: " + lang);
}
```

---

## 5. Funções, Parâmetros e Recursão

Funções no Yvira são cidadãs de primeira classe, declaradas com `fn`:

```yvira
# Função simples com retorno
fn calcular_juros(valor, taxa) {
    return valor * (1.0 + taxa);
}

let montante = calcular_juros(1000.0, 0.10);
print("Montante Final: R$ " + montante);

# Recursão elegante (Fibonacci)
fn fibonacci(n) {
    if (n <= 1) {
        return n;
    }
    return fibonacci(n - 1) + fibonacci(n - 2);
}

print("Fibonacci(10) = " + fibonacci(10)); # 55
```

---

## 6. Coleções: Listas e Mapas (Dicionários)

### Listas (Vetores Dinâmicos)
```yvira
let frutas = ["Manga", "Banana", "Uva"];

# Adicionar item
frutas.add("Morango");

# Tamanho da lista (.length)
print("Total de frutas: " + frutas.length());

# Acesso por índice (base 0)
print("Primeira fruta: " + frutas[0]);

# Remover do final (.pop)
let removido = frutas.pop();
print("Removido: " + removido);
```

### Mapas / Dicionários de Chave-Valor
```yvira
let config_servidor = {
    "host": "127.0.0.1",
    "porta": 8080,
    "producao": true
};

# Acesso por colchetes ou ponto
print("Conectando em " + config_servidor["host"] + ":" + config_servidor["porta"]);
```

---

## 7. Orientação a Objetos: Classes, Construtor e Métodos

O Yvira adota OOP moderno e explícito com `class`, método construtor `fn init` e palavra-chave `this`:

```yvira
class ContaBancaria {
    # Construtor: chamado ao instanciar o objeto
    fn init(titular, saldo_inicial) {
        this.titular = titular;
        this.saldo = saldo_inicial;
    }

    fn depositar(quantia) {
        this.saldo = this.saldo + quantia;
        print("[DEPOSITO] " + this.titular + " recebeu R$ " + quantia);
    }

    fn sacar(quantia) {
        if (quantia > this.saldo) {
            print("[ERRO] Saldo insuficiente para saque!");
            return false;
        }
        this.saldo = this.saldo - quantia;
        print("[SAQUE] Saque aprovado. Saldo restante: R$ " + this.saldo);
        return true;
    }
}

# Criando instâncias da classe
let conta = ContaBancaria("Santhyago", 15000.0);
conta.depositar(2000.0);
conta.sacar(500.0);
```

---

## 8. Banco de Dados Nativo com `@db`, `@exec` e `@query`

O Yvira não necessita de drivers de terceiros. As operações de banco de dados são **diretivas nativas do compilador**.

### Conexão com `@db`
```yvira
# Conexão padrão SQLite em arquivo:
@db("sqlite://meu_banco.db");

# Conexão em memória (ótima para testes e cache):
@db("sqlite://:memory:");

# Multi-Bancos simultâneos no mesmo código:
@db.analytics("sqlite://analytics.db");
```

### Comandos de Alteração com `@exec` (DDL / DML)
Retorna o número de linhas afetadas ou o ID gerado da inserção.
```yvira
@exec("CREATE TABLE IF NOT EXISTS clientes (id INTEGER PRIMARY KEY, nome TEXT, saldo REAL);");

# Inserção 100% segura com anti-SQL injection:
fn salvar_cliente(nome, saldo) {
    let id = @exec("INSERT INTO clientes (nome, saldo) VALUES (?, ?);", [nome, saldo]);
    return id;
}

salvar_cliente("Alice", 9000.0);
```

### Consultas com `@query`
Retorna uma lista de mapas indexáveis pelo nome das colunas:
```yvira
let ricos = @query("SELECT id, nome, saldo FROM clientes WHERE saldo >= ? ORDER BY saldo DESC;", [5000.0]);

print("Total encontrados: " + ricos.length());

foreach (c in ricos) {
    print("Cliente: " + c["nome"] + " | Saldo: R$ " + c["saldo"]);
}

# Consultando o banco secundário:
let logs = @query.analytics("SELECT * FROM auditoria;");
```

---

## 9. Automação Temporal sem Cron: `@time` e `@stop`

Um dos recursos mais inovadores do Yvira: **transforme qualquer função em um gatilho periódico** diretamente no código, sem criar arquivos de cron no Linux ou agendador no Windows.

```yvira
let ticks = 0;

# 1. Defina a função normalmente
fn monitor_servidor(servidor) {
    ticks = ticks + 1;
    print("[BOT TICK #" + ticks + "] Verificando saude de " + servidor);

    # 2. Interrompa quando quiser
    if (ticks >= 3) {
        print("Ciclo concluido! Encerrando com @stop...");
        @stop.monitor_servidor();
    }
}

# 3. Dispare a automação de onde quiser:
# Executa a função a cada 2 segundos passando argumentos
@time.monitor_servidor(2, "srv-producao-01");

print("Fluxo principal livre. O Yvira mantém o motor ativo em background!");
```

---

## 10. Super Motor Matricial e Álgebra Linear (IA & Dados)

Matrizes são tipos primitivos no Yvira. O operador `@` executa multiplicação matricial com alta performance:

```yvira
# Matriz 2x2
let A = [
    [4, 7],
    [2, 6]
];

let B = [
    [1, 2],
    [3, 4]
];

# Multiplicação Matricial com @
let C = A @ B;
print("A @ B:");
print(C);

# Transposta (.transposta() ou .T)
let At = A.transposta();
print(At);

# Inversa e Determinante
let detA = A.det();
let A_inv = A.inv();
print("Det: " + detA);
print("Inversa:");
print(A_inv);

# Propriedades de dimensão
print("Linhas: " + A.linhas + " | Colunas: " + A.colunas);
```

---

## 11. Módulos do Sistema e Rede: `fs` e `http`

### Módulo `fs` (File System)
```yvira
import "fs" as fs;

# Escrever e ler arquivos de texto
fs.write("registro.log", "Sistema iniciado com sucesso.\n");
fs.append("registro.log", "Nova requisicao processada.\n");

let conteudo = fs.read("registro.log");
print(conteudo);

# Verificar se existe
if (fs.exists("registro.log")) {
    print("Arquivo validado!");
}
```

### Módulo `http` (Requisições Web / REST)
```yvira
import "http" as http;

# Requisição GET com deserialização JSON instantânea
let resposta = http.get("https://jsonplaceholder.typicode.com/todos/1");
print("Status Code: " + resposta.status);

let dados = resposta.json();
print("ID: " + dados["id"]);
print("Titulo: " + dados["title"]);
```

---

## 12. Importações Nativas: `.yv`, `.env` e `.json`

No Yvira você não precisa de `dotenv` ou bibliotecas de JSON:
```yvira
# 1. Importar outro arquivo de código Yvira
import "utilitarios.yv" as utils;
utils.minha_funcao();

# 2. Importar arquivo de ambiente (.env) nativamente
import ".env" as env;
print("Host do Banco: " + env.DB_HOST);
print("Chave API     : " + env.API_KEY);

# 3. Importar arquivo JSON estruturado
import "parametros.json" as config;
print("Taxa de Aprendizado: " + config.learning_rate);
```

---

## 13. Tratamento de Exceções: `try`, `catch`, `finally` e `throw`

```yvira
fn transferir(saldo, valor) {
    if (valor <= 0) {
        throw "O valor da transferencia deve ser positivo!";
    }
    if (valor > saldo) {
        throw "Saldo insuficiente para transferir!";
    }
    return saldo - valor;
}

try {
    let saldo_atual = transferir(500.0, 1000.0);
    print("Novo Saldo: R$ " + saldo_atual);
} catch (erro) {
    print("[ERRO CAPTURADO]: " + erro);
} finally {
    print("[CLEANUP] Encerrando conexao com seguranca.");
}
```

---

## 14. Interfaces Gráficas e Desktop Híbrido (`criar_janela` e WebView2)

O Yvira permite criar aplicações desktop modernas com interface acelerada por hardware via WebView2 integrada ao motor de execução:

```yvira
let app = criar_janela({
    "titulo": "Meu Painel Yvira",
    "view": "views/index.html",
    "largura": 1000,
    "altura": 700,
    "barra_customizada": true,
    "redimensionavel": true
});

# Callback de evento disparado pelo HTML (window.yvira.emit)
fn aoReceberComando(dados) {
    print("[Ação]: " + dados.acao);
    app.emitir("resposta", { "status": "ok" });
}

app.ao_evento("comando_usuario", aoReceberComando);

app.exibir();
app.esperar();
```

---

## 15. Superpoder Realtime Multiplayer (`@realtime`)

O Yvira possui motor de comunicação realtime nativo via WebSockets de alta frequência em sockets TCP (sem depender de permissões de administrador no Windows):

### Servidor Realtime:
```yvira
let servidor = @realtime.servidor({
    "porta": 7070,
    "segredo": "CHAVE_AES_256",
    "rotacao": 30,
    "limitePacotes": 100
});

fn aoEntrar(cliente, dados) {
    cliente.entrar("lobby");
    servidor.para("lobby").emitir("novo_membro", { "id": cliente.id });
}

servidor.ao("entrar", aoEntrar);
servidor.iniciar();
```

### Cliente Realtime:
```yvira
let cliente = @realtime.cliente({
    "endereco": "ws://192.168.0.35:7070",
    "segredo": "CHAVE_AES_256"
});

fn aoMembroEntrar(dados) {
    print("Membro online: " + dados.id);
}

cliente.ao("novo_membro", aoMembroEntrar);
cliente.conectar();
```

---

## 16. Compilador de Executáveis Standalone (`yvira build`)

Transforme qualquer script ou projeto Yvira em um executável nativo autônomo para distribuição direta em máquinas Windows:

```powershell
# Empacota em pasta com dependencias e views:
yvira build -o MeuJogo

# Gera executavel 100% single-file:
yvira build --single-file -o MeuJogoSingle
```

---

## 17. Projeto Completo: Criando uma API de Backend com Banco e Bot

Para consolidar tudo o que você aprendeu, veja este exemplo de microsserviço completo combinando **Ambiente, Banco de Dados, Orientação a Objetos e Bot Temporal**:

```yvira
# =======================================================
# Microsserviço de Backend com Banco de Dados e Monitor
# =======================================================

import "fs" as fs;

# 1. Configurando Conexão de Banco de Dados
@db("sqlite://loja.db");

# 2. Criando Tabela
@exec("CREATE TABLE IF NOT EXISTS pedidos (id INTEGER PRIMARY KEY, cliente TEXT, total REAL, status TEXT);");

# 3. Classe de Domínio do Backend
class GerenciadorPedidos {
    fn criar_pedido(cliente, total) {
        let id = @exec("INSERT INTO pedidos (cliente, total, status) VALUES (?, ?, ?);", [cliente, total, "PENDENTE"]);
        print("[NOVO PEDIDO] #" + id + " para " + cliente + " (R$ " + total + ")");
        return id;
    }

    fn faturar_pendentes() {
        let alterados = @exec("UPDATE pedidos SET status = 'FATURADO' WHERE status = 'PENDENTE';");
        return alterados;
    }

    fn listar_faturados() {
        return @query("SELECT id, cliente, total FROM pedidos WHERE status = 'FATURADO';");
    }
}

let backend = GerenciadorPedidos();
backend.criar_pedido("Santhyago", 450.0);
backend.criar_pedido("Mariana", 1200.0);

# 4. Bot de Processamento Periódico em Background com @time
let ciclos = 0;

fn worker_processamento() {
    ciclos = ciclos + 1;
    let faturados = backend.faturar_pendentes();
    print("[WORKER #" + ciclos + "] Pedidos faturados no ciclo: " + faturados);

    if (ciclos >= 2) {
        print("[WORKER] Turno finalizado. Encerrando bot com @stop...");
        @stop.worker_processamento();
    }
}

# Roda o worker automaticamente a cada 2 segundos
@time.worker_processamento(2);

print("Serviço em execução contínua no Yvira...");
```

---

<div align="center">
Desenvolvido com paixão e orgulho por <b>Santhyago Souza Ponciano</b> 🇧🇷<br>
<i>Yvira: A simplicidade que o backend moderno sempre mereceu.</i>
</div>
