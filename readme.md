<div align="center">

```
 __     __      _             
 \ \   / /     (_)            
  \ \_/ /__   ___ _ __ __ _   
   \   / \ \ / / | '__/ _` |  
    | |   \ V /| | | | (_| |  
    |_|    \_/ |_|_|  \__,_|  
```

# Yvira Programming Language
**Linguagem de Backend Moderna com Ênfase em IA, APIs, Dados e Automação Nativa**

[![Versão](https://img.shields.io/badge/versão-2.1.0-blue.svg)](file:///C:/Users/santh/OneDrive/Área%20de%20Trabalho/ARQQUIVOS%20DRIVE/Beta%20OS/readme.md)
[![Feito no Brasil](https://img.shields.io/badge/desenvolvido%20no-Brasil%20🇧🇷-green.svg)](#-origem-e-criador)
[![Licença](https://img.shields.io/badge/licença-MIT-brightgreen.svg)](#-licença)
[![Plataforma](https://img.shields.io/badge/plataforma-Windows%20x64%20(Nativo)-purple.svg)](#)

</div>

---

## 📑 Índice Geral

0. [📘 Tutorial Completo da Linguagem (Do Zero ao Avançado)](TUTORIAL_YVIRA.md)
1. [Origem e Criador](#-origem-e-criador)
2. [A Ideia e Propósito da Linguagem](#-a-ideia-e-propósito-da-linguagem)
3. [Diferenciais em Relação ao Mercado](#-diferenciais-em-relação-ao-mercado)
4. [Instalação e CLI](#-instalação-e-comandos-da-cli)
5. [Manual e Documentação Completa da Linguagem](#-manual-e-documentação-completa-da-linguagem)
   - [5.1 Variáveis e Tipos Primitivos](#51-variáveis-e-tipos-primitivos)
   - [5.2 Operadores Aritméticos, Comparação e Lógica](#52-operadores-aritméticos-comparação-e-lógica)
   - [5.3 Controle de Fluxo (if, while, for, foreach)](#53-controle-de-fluxo-if-while-for-foreach)
   - [5.4 Funções e Escopo](#54-funções-e-escopo)
   - [5.5 Orientação a Objetos (Classes, Construtor e Métodos)](#55-orientação-a-objetos-classes-construtor-e-métodos)
   - [5.6 Super Motor Matricial e Álgebra Linear](#56-super-motor-matricial-e-álgebra-linear)
   - [5.7 Automação Temporal sem Cron (@time e @stop)](#57-automação-temporal-sem-cron-time-e-stop)
   - [5.8 Módulos Nativos do Sistema (fs e http)](#58-módulos-nativos-do-sistema-fs-e-http)
   - [5.9 Importação Nativa (.yv, .env e .json)](#59-importação-nativa-yv-env-e-json)
   - [5.10 Banco de Dados Nativo e Multi-Conexões (@db, @exec, @query)](#510-banco-de-dados-nativo-e-multi-conexões-db-exec-query)
   - [5.11 Tratamento de Exceções (try / catch / finally / throw)](#511-tratamento-de-exceções-try--catch--finally--throw)
6. [Projetos Oficiais de Exemplo no Repositório](#-projetos-oficiais-de-exemplo-no-repositório)
7. [Arquitetura Interna do Compilador e VM](#-arquitetura-interna-do-compilador-e-vm)
8. [Como Compilar e Contribuir](#-como-compilar-e-contribuir)
9. [Licença](#-licença)

---

## 🇧🇷 Origem e Criador

> *"Muitas linguagens modernas tentam abraçar o mundo inteiro e acabam exigindo ecossistemas pesados, dependências complexas e dezenas de ferramentas externas apenas para fazer o básico de análise de dados ou automação contínua. O Yvira nasceu da necessidade real de ter uma ferramenta ágil, independente e desenhada sob medida para resolver esses desafios com elegância."*

O **Yvira** foi idealizado e desenvolvido por **Santhyago Souza Ponciano**, desenvolvedor e pesquisador brasileiro.

O projeto nasceu de uma dor prática: no ecossistema atual de desenvolvimento, quem desenvolve aplicações de backend, APIs, rotinas com IA, processamento de dados e automações em tempo real (como monitoramento de APIs financeiras, trading, análise quantitativa e processamento matricial) frequentemente se depara com a mesma encruzilhada:

* Ou utiliza linguagens dinâmicas onde a sintaxe matemática depende de bibliotecas externas pesadas e onde rotinas de tempo exigem dependências de cron, daemons de SO e bibliotecas inchadas;
* Ou recorre a linguagens de baixo nível com sintaxe extremamente verbosa e ciclo de desenvolvimento lento.

O Yvira foi construído artesanalmente como uma **linguagem independente desenvolvida por um brasileiro**, criada para atender necessidades próprias e entregar em uma única ferramenta nativa: **backend produtivo, banco de dados sem boilerplate, automações temporais sem cron e super motor de dados/matrizes para IA**.

---

## 💡 A Ideia e Propósito da Linguagem

O Yvira é uma linguagem científica, interpretada e compilada em bytecode, projetada com três pilares centrais:

1. **Matemática e Álgebra Linear Nativas:** Matrizes, vetores e operações algébricas (`@`, `transposta()`, `inv()`, `det()`) não são bibliotecas importadas; são primitivas da própria sintaxe do compilador.
2. **Automações Temporais Integradas (`@time`):** Agendamento periódico de funções direto na linguagem, permitindo transformar qualquer rotina em um gatilho temporal reativo sem bibliotecas externas e sem cron de sistema operacional.
3. **Independência Operacional Completa:** O Yvira roda como um binário nativo global (`yvira.exe`), sem precisar de SDKs pesados instalados pelo usuário final.

### Inspiração e Base Tecnológica
O Yvira combina a legibilidade, expressividade e clareza de sintaxe inspirada em **Python** e **Rust**, com a robustez de engenharia de compiladores e uma Máquina Virtual de Pilha (Stack VM) inspirada nas arquiteturas modernas de runtimes como o **V8** e o **LuaJIT**, construído sob a infraestrutura de alto desempenho do **.NET 10**.

---

## 🚀 Diferenciais em Relação ao Mercado

| Funcionalidade | Python / Node / Outras | **Yvira (v2.1.0)** |
| :--- | :--- | :--- |
| **Operações com Matrizes** | Exige `numpy` ou bibliotecas C | **Nativo na sintaxe** com operador `@` e funções de álgebra |
| **Rotinas Periódicas / Cron** | Exige `cron`, `celery`, `node-cron` | **Nativo na linguagem** com sintaxe `@time` e `@stop` |
| **Arquivos de Configuração** | Exige módulos para ler `.env` e `.json` | **Importação nativa** com `import ".env"` e `import "config.json"` |
| **Execução sem Runtime Externo** | Exige interpretador / Node / JVM | **Binário único standalone** (`yvira.exe`) pronto para rodar |
| **Atualização do Ambiente** | Gestores complexos de pacotes | **Comando nativo** `yvira upgrade` e script local |
| **REPL Interativo** | REPLs padrão monocromáticos | **Destaque de sintaxe ao vivo com cores ANSI** e histórico |

---

## 🛠️ Instalação e Comandos da CLI

O executável nativo do Yvira pode ser utilizado globalmente em qualquer terminal Windows.

### Comandos Disponíveis:

```powershell
# Exibe a ajuda e comandos disponíveis
yvira

# Inicializa um novo projeto organizado com yvira.json, .env e main.yv
yvira init meu_projeto

# Executa o projeto atual (busca entrada em yvira.json) ou um arquivo específico
yvira run
yvira run scripts/analise.yv

# Executa via Compilador de Bytecode & Stack Virtual Machine
yvira vm script.yv

# Executa o benchmark comparativo de performance (AST vs Stack VM)
yvira bench teste_benchmark.yv

# Abre o console interativo com coloração de sintaxe em tempo real e histórico de setas
yvira repl

# Exibe informações completas da versão, runtime e manifesto do projeto ativo
yvira info

# Executa a bateria de testes internos da linguagem
yvira test

# Verifica atualizações e faz o auto-upgrade do executável
yvira upgrade
```

---

## 📖 Manual e Documentação Completa da Linguagem

### 5.1 Variáveis e Tipos Primitivos

O Yvira suporta números (inteiros e ponto flutuante de 64 bits), strings, booleanos, listas, mapas e tipos especiais como matrizes.

```yvira
# Variáveis mutáveis com let
let nome = "Yvira";
let versao = 2.1;
let ativo = true;

# Constantes (imutáveis) com const
const PI = 3.14159265359;
const LIMITE_MAXIMO = 1000;

# Listas / Vetores
let numeros = [10, 20, 30, 40];
print(numeros[0]); # 10

# Mapas / Dicionários de Chave-Valor
let usuario = {
    "nome": "Santhyago",
    "pais": "Brasil",
    "cargo": "Arquiteto"
};
print(usuario["nome"]); # Santhyago
```

---

### 5.2 Operadores Aritméticos, Comparação e Lógica

O Yvira suporta duas formas de operadores lógicos: símbolos matemáticos ou palavras-chave em inglês, à escolha do desenvolvedor.

```yvira
let a = 10;
let b = 3;

# Operadores Aritméticos
let soma = a + b;       # 13
let sub = a - b;        # 7
let mult = a * b;       # 30
let div = a / b;        # 3.3333...
let mod = a % b;        # 1
let pot = a ** b;       # 1000

# Atribuição Composta e Incremento
a += 5;                 # 15
a++;                    # 16

# Operadores de Comparação
print(a == b);          # false
print(a != b);          # true (ou use <>)
print(a > b);           # true
print(a <= 20);         # true

# Operadores Lógicos (Forma Símbolo ou Texto)
if (a > 10 && b < 5) {
    print("Condicao satisfeita via &&");
}

if (a > 10 and b < 5) {
    print("Condicao satisfeita via 'and'");
}

if (not false or true) {
    print("Suporte a 'or' e 'not'");
}
```

---

### 5.3 Controle de Fluxo (if, while, for, foreach)

```yvira
# 1. Condicional if / else
let nota = 8.5;
if (nota >= 9.0) {
    print("Excelente!");
} else if (nota >= 7.0) {
    print("Aprovado!");
} else {
    print("Recuperacao");
}

# 2. Laço while
let contador = 0;
while (contador < 3) {
    print("Contador: " + contador);
    contador++;
}

# 3. Laço for clássico
for (let i = 0; i < 5; i++) {
    if (i == 2) {
        continue; # pula a iteracao 2
    }
    print("Indice i = " + i);
}

# 4. Laço foreach sobre listas
let linguagens = ["Yvira", "Python", "Rust", "C#"];
foreach (let lang in linguagens) {
    print("Linguagem: " + lang);
}
```

---

### 5.4 Funções e Escopo

Funções são declaradas com a palavra-chave `fn` e suportam recursão, múltiplos parâmetros e retorno de valores:

```yvira
fn saudacao(nome) {
    return "Ola, " + nome + "! Bem-vindo ao Yvira.";
}

print(saudacao("Dev"));

# Função recursiva de Fibonacci
fn fibonacci(n) {
    if (n <= 1) {
        return n;
    }
    return fibonacci(n - 1) + fibonacci(n - 2);
}

print("Fibonacci(10) = " + fibonacci(10)); # 55
```

---

### 5.5 Orientação a Objetos (Classes, Construtor e Métodos)

O Yvira implementa Orientação a Objetos limpa e direta com `class`, método construtor `init` e palavra-chave `this`:

```yvira
class ContaBancaria {
    fn init(titular, saldo_inicial) {
        this.titular = titular;
        this.saldo = saldo_inicial;
    }

    fn depositar(valor) {
        this.saldo += valor;
        print("[DEPOSITO] Novo saldo de " + this.titular + ": R$ " + this.saldo);
    }

    fn sacar(valor) {
        if (valor > this.saldo) {
            print("[ERRO] Saldo insuficiente para " + this.titular);
            return false;
        }
        this.saldo -= valor;
        print("[SAQUE] Saldo restante: R$ " + this.saldo);
        return true;
    }
}

let conta = ContaBancaria("Santhyago", 1000.0);
conta.depositar(500.0);
conta.sacar(200.0);
```

---

### 5.6 Super Motor Matricial e Álgebra Linear

Matrizes são **tipos primitivos nativos** no Yvira. Você não precisa importar bibliotecas externas para realizar cálculos matemáticos avançados.

* O operador `@` realiza multiplicação matricial direta ($A \cdot B$).
* Funções e métodos embutidos realizam inversão, transposição, determinante e resolução de sistemas.

```yvira
# Definição literal de matrizes 2D
let A = [
    [4, 7],
    [2, 6]
];

let B = [
    [1, 0],
    [0, 1]
];

# 1. Multiplicação Matricial com o operador @
let C = A @ B;
print("Resultado de A @ B:");
print(C);

# 2. Transposta
let At = A.transposta();
print("Transposta de A:");
print(At);

# 3. Determinante e Inversa
let detA = A.det();
print("Determinante de A: " + detA);

let A_inv = A.inv();
print("Inversa de A:");
print(A_inv);

# 4. Verificação de Identidade (A @ A^-1 = I)
let Identidade = A @ A_inv;
print("Identidade:");
print(Identidade);
```

---

### 5.7 Automação Temporal sem Cron (`@time` e `@stop`)

Um dos maiores diferenciais do Yvira: **qualquer função pode se transformar em um cron nativo com agendamento temporal**, sem precisar criar arquivos de cron no Linux, sem configurar agendador de tarefas no Windows e sem bibliotecas externas:

```yvira
# 1. Uma função normal
let ticks = 0;

fn coletar_metricas(servidor) {
    ticks++;
    print("[METRICA #" + ticks + "] Monitorando servidor: " + servidor);

    # Para a execução programada quando atingir a condição
    if (ticks >= 4) {
        print("Limite de 4 coletas atingido! Encerrando rotina...");
        @stop.coletar_metricas();
    }
}

# 2. Ativação do agendamento temporal em qualquer parte do código:
# Roda a função 'coletar_metricas' a cada 2 segundos passando parâmetros
@time.coletar_metricas(2, "srv-cluster-01");

print("Fluxo principal livre. O Yvira mantém o motor de eventos ativo em background.");
```

---

### 5.8 Módulos Nativos do Sistema (`fs` e `http`)

O Yvira traz utilitários essenciais embutidos para operações de disco e requisições de rede.

#### Módulo `fs` (File System):
```yvira
# Escrever e ler arquivos
fs.write("dados.txt", "Cotacao BTC=95000\nCotacao ETH=3200");
let conteudo = fs.read("dados.txt");
print(conteudo);

# Verificar existência e remover
if (fs.exists("dados.txt")) {
    print("O arquivo existe no disco!");
}
```

#### Módulo `http` (Networking):
```yvira
# Requisição GET com parsing JSON automático
let resposta = http.get("https://jsonplaceholder.typicode.com/todos/1");
print("Status Code: " + resposta.status);

let json = resposta.json();
print("ID: " + json.id);
print("Titulo da Tarefa: " + json.title);
```

---

### 5.9 Importação Nativa (`.yv`, `.env` e `.json`)

O comando `import` carrega módulos Yvira, arquivos de ambiente e dados JSON diretamente no escopo como dicionários:

```yvira
# Importar outro script Yvira
import "matematica.yv" as mat;
let res = mat.calcular(10);

# Importar variáveis de ambiente (.env) nativamente
import ".env" as config;
print("Banco de Dados: " + config.DB_HOST);
print("Porta: " + config.DB_PORT);

# Importar arquivos JSON estruturados
import "parametros.json" as params;
print("Taxa de Aprendizado: " + params.learning_rate);
```

---

### 5.10 Tratamento de Exceções (`try` / `catch` / `finally` / `throw`)

```yvira
fn dividir(a, b) {
    if (b == 0) {
        throw "Divisao por zero nao permitida!";
    }
    return a / b;
}

try {
    let resultado = dividir(10, 0);
    print("Resultado: " + resultado);
} catch (erro) {
    print("[FALHA CAPTURADA]: " + erro);
} finally {
    print("[FINALLY] Rotina de limpeza executada.");
}
```

---

### 5.11 Novos Recursos de Base: `match`, Interpolação, Herança e Debuggers (`@die` / `@sleep`)

```yvira
# 1. Interpolação de Strings ($"...")
let guerreiro = "Thor";
let dano = 250;
print($"O herói {guerreiro} causou {dano * 2} de dano crítico!");

# 2. Pattern Matching / Switch Moderno (match)
match evento {
    "login" => print("Entrando no sistema..."),
    "batalha" => prepararArena(),
    _ => print("Evento desconhecido")
}

# 3. Herança e Métodos Estáticos (extends, static)
class Entidade {
    fn constructor(nome) { this.nome = nome; }
    static fn versao() { return "v2.1"; }
}

class Heroi extends Entidade {
    fn saudar() { return $"Sou o herói {this.nome}!"; }
}

# 4. Constantes Rigorosas (const)
const PI = 3.14159;
# PI = 3.15; # Dispara exceção imediata de bloqueio!

# 5. Diretivas Reativas de Debugging (@die e @sleep)
@sleep(0.5); # Pausa por meio segundo
# @die("Inspecionando variáveis:", guerreiro, dano); # Dump e parada imediata!
```

---

## 🚀 Projetos Oficiais de Exemplo no Repositório

O repositório acompanha projetos completos demonstrando a versatilidade do Yvira em cenários reais de backend, dados e IA:

| Projeto | Caminho | O Que Demonstra |
| :--- | :--- | :--- |
| **Pipeline Backend & IA** | [exemplos/api_pipeline_ia](file:///exemplos/api_pipeline_ia) | Configurações .env, banco @db, tensores matriciais, treinamento de **Perceptron / Classificador Linear de Fraude**, inferência e relatório s. |
| **Click Battle Arena Multiplayer** | [exemplos/click_battle_game](file:///C:/Users/santh/OneDrive/Área%20de%20Trabalho/ARQQUIVOS%20DRIVE/Beta%20OS/exemplos/click_battle_game) | Jogo desktop competitivo em tempo real 100% Yvira. Utiliza `@realtime` nativo em `0.0.0.0:7070`, WebView2 desacoplado, sincronização em LAN e executável compilado via `yvira build`. |
| **Microserviço Auth & Bot** | [exemplos/microservico_bot_auth](file:///C:/Users/santh/OneDrive/Área%20de%20Trabalho/ARQQUIVOS%20DRIVE/Beta%20OS/exemplos/microservico_bot_auth) | Orientação a Objetos (class), multi-conexões simultâneas (@db e @db.analytics), sanitização anti-SQL injection e **bot de monitoramento temporal com @time**. |
| **Monitor Cripto em Tempo Real** | [exemplos/monitor_cripto](file:///exemplos/monitor_cripto) | Consumo de API REST pública (http.get), manipulação de JSON, matrizes para estatística e persistência em log. |
| **Regressão Linear Científica** | [exemplos/regressao_linear](file:///exemplos/regressao_linear) | Leitura de CSV, álgebra linear pura (^T X)^{-1} X^T Y$, inversão de matriz e coeficientes OLS. |

Para executar qualquer um dos projetos:
`powershell
cd exemplos/api_pipeline_ia
yvira run
`

---

## 🏗️ Arquitetura Interna do Compilador e VM

A engenharia do Yvira é dividida em subsistemas desacoplados com alto foco em performance:

```
Beta OS/
├── Yvira.Lexer/          # Tokenizador determinístico de alto desempenho
├── Yvira.Ast/            # Modelagem de nós sintáticos fortemente tipados
├── Yvira.Parser/         # Parser recursivo descendente com precedência Pratt
├── Yvira.Semantic/       # Análise semântica e verificação de escopo
├── Yvira.Vm/             # Motores de Execução Duplos:
│   ├── Bytecode/         # Compilador de Opcodes e Máquina Virtual baseada em Pilha
│   ├── Scheduler.cs      # Motor de agendamento reativo (@time / @stop)
│   ├── FileSystemModule  # Integração com I/O nativo
│   ├── HttpModule        # Cliente HTTP moderno baseado em HttpClient
│   └── YviraMatrix.cs    # Motor nativo de álgebra linear e matrizes
├── Yvira.Cli/            # Interface de linha de comando com REPL colorido
├── vscode-extension/     # Extensão oficial do Visual Studio Code (.vsix)
└── dist/bin/             # Executável único nativo standalone (yvira.exe)
```

---

## 🛠️ Como Compilar e Contribuir

O Yvira é código aberto! Qualquer desenvolvedor pode clonar o repositório, adicionar novas funcionalidades e contribuir com o projeto.

### Pré-requisitos para Compilar o Código-Fonte:
* [.NET 10 SDK](https://dotnet.microsoft.com/download)
* Git

### Compilando a Solução:
```powershell
# Restaurar e compilar todos os projetos
dotnet build -c Release
```

### Publicando o Executável Nativo Localmente:
Para compilar todas as alterações e atualizar automaticamente o binário `yvira.exe` no seu `PATH`, execute:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish_local.ps1
```

---

## 📄 Licença

Distribuído sob a licença **MIT**. Você é livre para usar, estudar, modificar e distribuir este projeto comercialmente ou para pesquisa científica.

---

<div align="center">
Desenvolvido com dedicação e paixão por <b>Santhyago Souza Ponciano</b> 🇧🇷<br>
<i>Levando a engenharia de software e a computação científica brasileira para o mundo.</i>
</div>
