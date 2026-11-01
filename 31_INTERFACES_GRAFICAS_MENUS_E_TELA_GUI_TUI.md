# 31. Interfaces Gráficas, Menus e Motor de Telas (TUI & GUI Desktop)

O Yvira suporta o desenvolvimento de interfaces de usuário tanto no terminal (TUI - Terminal User Interface) quanto em janelas desktop nativas ultraleves com renderização moderna baseada em WebView2 e estilização completa via HTML5/CSS3.

Este ecossistema permite criar desde **menus interativos de comando** até **jogos de estratégia completos (estilo Brasfoot, Football Manager clássico e Tycoons)** de forma limpa, desacoplada e performática.

---

## 1. Padrão de Arquitetura em Camadas (MVC-Game)

Para evitar desorganização em projetos complexos, o Yvira adota uma separação rigorosa de responsabilidades:

```
meu_jogo_ou_sistema/
│
├── main.yv                     # Ponto de entrada (cria e gerencia o ciclo da janela)
│
├── views/                      # Camada Visual (Apresentação HTML/CSS)
│   ├── index.html              # Estrutura DOM, titlebar customizada e containers
│   └── assets/
│       └── css/
│           ├── global.css      # Variáveis, fontes, bordas e botões da janela
│           └── jogo.css        # Cards, tabelas, grids e estilos específicos
│
└── src/                        # Camada de Regras e Lógica de Negócio (100% YVIRA)
    ├── core/
    │   └── partida.yv          # Motores de simulação matemática, regras e dados
    └── controllers/
        └── jogo_controller.yv  # Ponte bidirecional entre cliques na tela e o Yvira
```

---

## 2. Camada 1: Elementos Gráficos no Terminal (TUI)

O interpretador e o CLI do Yvira disponibilizam primitivas de console ricas e interativas:

| Função Built-in | Descrição | Exemplo |
| :--- | :--- | :--- |
| `menu(titulo, opcoes)` | Menu com setas do teclado `↑` / `↓` e `Enter`. Retorna o índice escolhido. | `let opt = menu("Opções:", ["Iniciar", "Sair"]);` |
| `confirmar(texto)` | Confirmação rápida `[S/n]` no teclado. | `let ok = confirmar("Salvar alterações?");` |
| `caixa(titulo, texto)` | Desenha caixas estilizadas com bordas unicode (`┌─┐`, `│`, `└─┘`). | `caixa("SISTEMA", "Online");` |
| `tabela(headers, linhas)` | Renderiza tabelas formatadas com ajuste automático de colunas. | `tabela(["Item", "Preço"], [["Ouro", "2730"]]);` |
| `barra_progresso(atual, total, txt)` | Barra de progresso animada (`[████░░] 60%`). | `barra_progresso(i, 100, "Carregando:");` |
| `limpar_tela()` | Limpa a tela do terminal. | `limpar_tela();` |
| `dormir(ms)` | Pausa a execução em milissegundos para animações. | `dormir(50);` |

### Menu Central do CLI:
Ao digitar apenas `yvira` ou `yvira menu` no terminal, o Yvira abre uma central de comandos interativa para rodar scripts, abrir o REPL ou gerenciar projetos.

---

## 3. Camada 2: Motor de Janelas Desktop (Yvira GUI Engine)

Para interfaces visuais, o Yvira cria janelas nativas aceleradas por hardware com bridge bidirecional via IPC:

### A. Criação da Janela no Yvira
```yv
let app = criar_janela({
    "titulo": "Brasfoot Yvira 2026",
    "view": "views/index.html",
    "largura": 1100,
    "altura": 720,
    "min_largura": 900,
    "min_altura": 600,
    "barra_customizada": true,     # Remove a borda padrão do Windows
    "redimensionavel": true,
    "cor_fundo": "#121418"
});

app.exibir();
app.esperar();
```

### B. Barra de Título Customizada (Arrastável)
No HTML/CSS, qualquer elemento com `data-arrastavel="true"` ou a classe `.titlebar` pode ser clicado e arrastado pelo mouse para mover a janela:

```html
<header class="titlebar" data-arrastavel="true">
    <span>Meu Jogo</span>
    <div>
        <button onclick="window.yvira.minimizar()">─</button>
        <button onclick="window.yvira.maximizar()">□</button>
        <button onclick="window.yvira.fechar()">✕</button>
    </div>
</header>
```

### C. Comunicação Bidirecional (Yvira ⇄ Tela)

1. **A Tela dispara eventos para o Yvira:**
   ```javascript
   window.yvira.emit("simular_partida", { rodada: 14 });
   ```

2. **O Yvira processa e devolve os dados em tempo real:**
   ```yvira
   fn aoSimularPartida(dados) {
       let resultado = motor.simular();
       app.emitir("placar_atualizado", resultado);
   }

   app.ao_evento("simular_partida", aoSimularPartida);
   ```

---

## 4. Como Executar os Exemplos

### Exemplo 1: Menu Interativo de Terminal (TUI)
```powershell
yvira run exemplos\menu_bot.yv
```

### Exemplo 2: Jogo com Janela Gráfica Estilo Brasfoot (GUI)
```powershell
yvira run exemplos\brasfoot_game\main.yv
```
