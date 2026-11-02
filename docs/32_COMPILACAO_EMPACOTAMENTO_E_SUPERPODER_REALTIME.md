# 33. SISTEMA DE COMPILAÇÃO, EMPACOTAMENTO E SUPERPODER `@realtime` CRIPTOGRAFADO

Este documento registra a especificação completa de duas grandes inovações no ecossistema Yvira:
1. **O Motor de Empacotamento de Binários (`yvira build`)**: Geração de executáveis `.exe` portáteis e single-file selados com payload embutido.
2. **A Diretiva Nativa `@realtime` com Blindagem Criptográfica E2E e Anti-Flood**: Infraestrutura de rede realtime sem JavaScript com eventos nomeados, salas, validação por Chaves Rotativas (Rolling Keys HMAC-SHA256), Criptografia AES-256 e Rate Limiter nativo.

---

## PARTE 1: COMPILAÇÃO E EMPACOTAMENTO DE APLICAÇÕES (`yvira build`)

Para resolver a necessidade de distribuir jogos e aplicações Yvira para máquinas de terceiros sem exigir instalação prévia da linguagem ou do runtime, foram criadas duas modalidades de empacotamento:

### 1.1. Modo Portátil (`Standalone + Diretório`)
Ideal para jogos modulares, engines e softwares com grandes árvores de assets.
- **Comando:**
  ```powershell
  yvira build -o MeuJogo
  ```
- **Estrutura gerada na pasta `dist/`:**
  ```
  dist/
  ├── MeuJogo.exe          (Executável nativo com runtime Yvira embutido)
  ├── views/               (Templates HTML, CSS e scripts visuais)
  ├── src/                 (Códigos-fonte e módulos Yvira)
  ├── dados/               (Bases de dados, saves e configurações)
  └── yvira.json           (Manifesto do projeto)
  ```
- **Execução:** O usuário executa `MeuJogo.exe` diretamente. Ele localiza o entrypoint (`main.yv` ou manifesto) e sobe o motor sem necessidade de parâmetros de terminal.

### 1.2. Modo Single-File Selado (`1 Único Arquivo .exe`)
Ideal para demos, ferramentas utilitárias e distribuição ágil (WhatsApp, Pen Drive, Google Drive).
- **Comando:**
  ```powershell
  yvira build --single-file -o MeuJogoSingle
  ```
- **Como funciona por baixo dos panos:**
  1. O compilador compacta em memória todo o projeto (`.yv`, views, assets, dados) em formato ZIP otimizado.
  2. Clona o binário executável do runtime Yvira e anexa o payload compactado ao final do arquivo PE.
  3. Adiciona um rodapé de segurança contendo:
     - 8 bytes com o tamanho exato do payload (`long`).
     - Assinatura mágica de verificação: `YVIRA_BUNDLE_V1!`.
  4. Ao dar duplo-clique no `.exe`:
     - O bootstrap lê a assinatura mágica no próprio arquivo.
     - Extrai silenciosamente para o cache isolado da máquina: `%LOCALAPPDATA%\Yvira\AppsCache\<NomeApp>`.
     - Executa o aplicativo com máxima performance sem poluir a pasta do usuário.

---

## PARTE 2: BLINDAGEM DE SEGURANÇA DO SUPERPODER `@realtime` (Nível 8.5/10)

Para transformar a comunicação realtime em padrão corporativo e anti-cheat militar para jogos, implementamos 3 camadas de defesa ativa:

```
 ┌─────────────────────────────────────────────────────────────────┐
 │                   SERVIDOR YVIRA (Host)                         │
 │  let srv = @realtime.servidor({                                 │
 │      porta: 5060,                                               │
 │      segredo: "CHAVE_AES_256",                                  │
 │      rotacao: 15,                                               │
 │      limitePacotes: 30                                          │
 │  });                                                            │
 └───────────────────────────────┬─────────────────────────────────┘
                                 │
                 ┌───────────────┴───────────────┐
                 ▼                               ▼
       [CAMADA 1: ROLLING KEY]          [CAMADA 2: E2E CRYPTO]
       HMAC-SHA256 Time-Based          AES-256-CBC com PBKDF2
       Expira a cada 15/30s            Toda mensagem cifrada
                 │                               │
                 └───────────────┬───────────────┘
                                 ▼
                     [CAMADA 3: ANTI-FLOOD]
                     Rate Limiter Nativo por Socket
                     Bloqueio automático de DoS/Spam
                                 │
                 ┌───────────────┴───────────────┐
                 ▼                               ▼
  ┌─────────────────────────────┐  ┌─────────────────────────────┐
  │      CLIENTE 1 (Yvira)      │  │      CLIENTE 2 (Yvira)      │
  │ let c = @realtime.cliente() │  │ let c = @realtime.cliente() │
  └─────────────────────────────┘  └─────────────────────────────┘
```

### 2.1. Camada 1: Handshake com Chaves Rotativas (Rolling Keys HMAC-SHA256)
- O token muda dinamicamente no tempo ($\text{Timestamp} / \text{Janela}$).
- Se um invasor capturar um token, em 15 ou 30 segundos ele já não serve para mais nada.
- Requisições sem o token válido da janela sofrem rejeição imediata no handshake.

### 2.2. Camada 2: Criptografia Ponta a Ponta de Pacotes (AES-256-CBC)
- Diferente de WebSockets comuns que trafegam JSON em texto plano, com o segredo ativo todo pacote é cifrado com `AES-256-CBC` e chave derivada com `PBKDF2/SHA-256` e Salt dinâmico.
- Ninguém com Wireshark ou proxy consegue ler as mensagens ou jogadas.

### 2.3. Camada 3: Rate Limiting & Anti-Flood Nativo
- Limite de pacotes por segundo configurável por cliente (ex: `limitePacotes: 30`).
- Se um bot ou script malicioso tentar floodar o servidor com 1.000 requisições/segundo, o servidor dispara o evento `flood_detectado` e desconecta a conexão abusiva na hora.

---

## PARTE 3: GUIA DE CÓDIGO DA DIRETIVA `@realtime`

### 3.1. Servidor Realtime Blindado (`server.yv`)
```yv
# Instancia o servidor realtime com criptografia e anti-flood
let servidor = @realtime.servidor({
    "porta": 5060,
    "segredo": "CHAVE_SECRETA_MILITAR_2026",
    "rotacao": 15,          # Chave rotativa expira a cada 15 segundos
    "limitePacotes": 30     # Maximo de 30 pacotes por segundo por cliente
});

fn aoIniciar(info) {
    print("[Servidor Online]: Escutando na porta " + info.porta);
    print("  -> Criptografia: " + info.criptografia);
    print("  -> Anti-Flood Limite: " + info.antiFloodLimite + " pacotes/s");
}

servidor.ao("iniciado", aoIniciar);

fn aoConectar(cliente) {
    print("[Jogador Autenticado]: ID " + cliente.id + " | IP: " + cliente.ip);
    cliente.entrar("mesa_01");
    cliente.transmitirPara("mesa_01", "novo_oponente", { "id": cliente.id });
}

servidor.ao("conectar", aoConectar);

fn aoJogada(cliente, dados) {
    print("Jogada validada de " + cliente.id + ": " + dados.origem + " -> " + dados.destino);
    cliente.transmitirPara("mesa_01", "jogada_oponente", dados);
}

servidor.ao("jogada", aoJogada);

fn aoDetectarFlood(alerta) {
    print("[ALERTA]: Flood detectado do cliente " + alerta.id + " (" + alerta.ip + "). Conexao encerrada!");
}

servidor.ao("flood_detectado", aoDetectarFlood);

servidor.iniciar();
```

---

### 3.2. Cliente Realtime Seguro (`main.yv`)
```yv
# Conecta ao servidor oficial com criptografia E2E automática
let rede = @realtime.cliente({
    "endereco": "ws://meujogo.com:5060",
    "segredo": "CHAVE_SECRETA_MILITAR_2026",
    "rotacao": 15
});

fn aoConectar() {
    print("[Multiplayer]: Conexao criptografada estabelecida com sucesso!");
}

rede.ao("conectar", aoConectar);

fn aoReceberJogada(jogada) {
    print("Oponente moveu: " + jogada.origem + " para " + jogada.destino);
    tabuleiro.mover(jogada.origem, jogada.destino);
    app.emitir("renderizar_tabuleiro", tabuleiro.obterMatriz());
}

rede.ao("jogada_oponente", aoReceberJogada);

rede.conectar();
```

---

## PARTE 4: TABELA DE MÉTODOS E EVENTOS

### Servidor (`@realtime.servidor`)
| Parâmetro / Opção | Tipo | Padrão | Descrição |
| :--- | :--- | :--- | :--- |
| `porta` | Número | `5000` | Porta de escuta do servidor |
| `segredo` | Texto | `""` | Segredo para HMAC e criptografia AES-256 |
| `rotacao` | Número | `30` | Intervalo de rotação do token em segundos |
| `limitePacotes` | Número | `60` | Limite de pacotes/segundo contra DoS |

| Evento do Servidor | Argumentos | Descrição |
| :--- | :--- | :--- |
| `"iniciado"` | `(info)` | Disparado quando o listener sobe com sucesso |
| `"conectar"` | `(cliente)` | Cliente autenticado e conectado |
| `"desconectar"` | `(cliente)` | Cliente finalizou a conexão |
| `"rejeitado"` | `(alerta)` | Tentativa não autorizada ou token expirado |
| `"flood_detectado"` | `(alerta)` | Cliente ultrapassou o limite de pacotes |
