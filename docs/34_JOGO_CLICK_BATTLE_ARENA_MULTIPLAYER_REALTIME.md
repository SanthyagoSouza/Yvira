# 34. JOGO CLICK BATTLE ARENA - DUELO MULTIPLAYER REALTIME COM @REALTIME

Este documento registra a implementação completa, arquitetura de rede e regras do jogo **Click Battle Arena**, desenvolvido no ecossistema Yvira para testar e validar o superpoder nativo `@realtime` com Chaves Rotativas e Criptografia AES-256.

---

## 1. REGRAS E DINÂMICA DO JOGO

O **Click Battle Arena** é um jogo competitivo de velocidade de clique (*Clicks Per Second - CPS*) entre dois oponentes online:
1. **Identificação Obrigatória:** Cada jogador insere seu apelido de guerra para ingressar no Lobby global.
2. **Lobby de Rivais:** Lista em tempo real todos os guerreiros online, seu status atual (`livre` ou `em_batalha`).
3. **Desafio P2P:** Um jogador pode clicar em **"Desafiar"** em qualquer rival livre. O alvo recebe um popup modal instantâneo com opções de Aceitar ou Recusar.
4. **Arena dos 10 Segundos:**
   - Ao aceitar, o servidor isola os dois oponentes em uma sala de combate.
   - Um cronômetro sincronizado de 10 segundos é iniciado.
   - Cada clique no botão central gigante dispara um evento `@realtime`.
   - Uma barra de **Cabo de Guerra (Tug of War)** se move dinamicamente na tela dos dois jogadores mostrando quem está dominando o duelo.
5. **Decisão e Veredito:**
   - Ao zerar o tempo, o Servidor Yvira julga o resultado e coroa o vencedor com tela de vitória e estatísticas completas de cliques.

---

## 2. ESTRUTURA DO PROJETO

Localização: `exemplos/click_battle_game/`

```
click_battle_game/
├── server.yv                (Servidor Realtime dedicado com salas e matchmaking - 100% Yvira)
├── main.yv                  (Aplicação Desktop do jogador com toda a lógica de jogo - 100% Yvira)
├── views/
│   ├── index.html           (Marcação e eventos via window.yvira.emit - sem scripts JS auxiliares)
│   └── assets/
│       ├── css/style.css    (Tema Cyberpunk Arena Neon)
│       └── css/yvira-shell.css (Barra de título customizada Yvira)
└── dist/
    ├── ClickBattleArena.exe (Executável compilado independente)
    └── texto.txt            (Instruções de execução direta para LAN / Computador Local)
```

---

## 3. COMO EXECUTAR O JOGO

### 3.1. Iniciar o Servidor Multiplayer:
No terminal, na raiz do projeto:
```powershell
yvira run exemplos/click_battle_game/server.yv
```

### 3.2. Abrir o Jogo (2 Jogadores):
Você pode abrir duas janelas no mesmo computador ou em máquinas diferentes na rede:
```powershell
# Jogador 1:
yvira run exemplos/click_battle_game/main.yv

# Jogador 2 (em outro terminal):
yvira run exemplos/click_battle_game/main.yv
```
Ou simplesmente dar duplo-clique no executável gerado:
👉 [ClickBattleArena.exe](file:///C:/Users/santh/OneDrive/Área%20de%20Trabalho/ARQQUIVOS%20DRIVE/Beta%20OS/exemplos/click_battle_game/dist/ClickBattleArena.exe)

---

## 4. SEGURANÇA E PERFORMANCE AVALIADAS

- **Anti-Flood Sintonizado:** O limite de pacotes foi calibrado para `limitePacotes: 150` no servidor, permitindo taxas altíssimas de até 15 cliques/segundo sem falsos-positivos.
- **Criptografia E2E Ativa:** Todos os cliques e desafios trafegam cifrados via **AES-256-CBC**.
- **Handshake Protegido:** Conexão autenticada por chaves rotativas de 30 segundos.
