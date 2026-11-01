# Yvira Jira Task Manager - Sistema de Gestão de Tarefas Kanban

Sistema desktop moderno no estilo Jira/Kanban para gerenciamento de fluxo de trabalho e tarefas desenvolvido com a tecnologia Yvira.

---

## 🚀 Funcionalidades

- **Movimentação dos Cards via Drag and Drop**:
  - Arraste visual nativo entre colunas com efeito dinâmico de destaque (`.drag-over` e `.dragging`).
  - Evento de movimentação interceptado e processado no backend Yvira (`mover_card`).
- **Definição e Reordenação da Ordem das Colunas**:
  - Reordenação rápida diretamente no cabeçalho das colunas (botões `◀` e `▶`).
  - Painel no Modal de Etapas com reordenação vertical (`▲` e `▼`).
  - O motor Yvira recebe e gerencia a nova sequência via `reordenar_etapas`.
- **Animações Fluidas de Abertura e Fechamento dos Modais**:
  - Efeito suave estilo macOS/Apple com transição de escala (`scale 0.92 -> 1.0`), fade-in/fade-out e backdrop blur.
  - Fechamento com animação reversa ao clicar em Cancelar, no botão `✕` ou fora da caixa modal.
- **Checklist de Subtarefas por Card**:
  - Adição dinâmica de subtarefas dentro do modal.
  - Checkbox para marcar/desmarcar itens completados com efeito tachado.
  - Barra de progresso visual percentual em cada card do Kanban.
- **Quadro Kanban Dinâmico**:
  - Adição de novas etapas, renomeação em tempo real e migração automática de cards ao excluir colunas.
- **Busca e Filtro em Tempo Real**:
  - Campo de pesquisa rápida que filtra instantaneamente os cards no quadro.
- **Sem Dependência de Login ou WebSockets**:
  - Arquitetura local e direta via IPC com WebView2 nativo do motor Yvira.

---

## 🛠 Como Executar

A partir da raiz do projeto:

```powershell
dotnet run --project "Yvira.Cli" -- run "exemplos\jira_task_manager\main.yv"
```
