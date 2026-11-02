# 💬 Yvira Channels - Sistema de Chat e Salas Estilo Discord

Sistema completo de salas/canais em tempo real construído com **Yvira Realtime** (`@realtime`), interface Desktop moderna inspirada no Discord e criptografia ponta a ponta.

---

## 🚀 Como Executar

### Opção 1: Automático (Servidor + 2 Clientes em 1 clique)
Dê um duplo clique no arquivo:
```cmd
iniciar.bat
```

### Opção 2: Manual via Terminal
1. **Inicie o Servidor:**
   ```powershell
   ..\..\dist\bin\yvira.exe run server.yv
   ```
2. **Abra quantos clientes quiser:**
   ```powershell
   ..\..\dist\bin\yvira.exe run main.yv
   ```

---

## ✨ Funcionalidades

- **Canais Dinâmicos de Texto:**
  - Salas pré-configuradas: `#geral`, `#desenvolvimento` e `#jogos`.
  - Botão **`+`** para qualquer usuário criar um novo canal instantaneamente para toda a rede.
- **Isolamento de Canais:**
  - As mensagens são distribuídas apenas para os membros presentes naquele canal.
- **Lista de Membros Ativos:**
  - Painel lateral direito exibindo quem está online e presente no canal ativo.
- **Feedback em Tempo Real:**
  - Indicador animado de *"Fulano está digitando..."*.
  - Notificação de entrada e saída de usuários no canal.
- **Design Autêntico Discord:**
  - Barra de servidores, canais de texto, feed de mensagens e rodapé de perfil de usuário com avatar e tag.
