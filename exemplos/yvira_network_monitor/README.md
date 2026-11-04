# Yvira Pulse - Network Sentinel & Switch Monitor

Aplicação desktop nativa desenvolvida em **Yvira**, combinando:
- **Backend nativo**: Módulo `switch` com varredura L2-L7 profunda (ARP, NetBIOS, Port Scanner, OUI Manufacturer Resolution e telemetria SNMP).
- **Automação Nativa `@time`**: Atualização periódica e contínua do estado da rede em background sem travar a interface.
- **Front-end Moderno em Vue.js 3**: Interface dark responsiva, com cards de telemetria, tabela interativa, filtros instantâneos, painel de inspeção de host, disparo de Ping e pacotes Wake-on-LAN.

---

## 🚀 Como Executar

Dê um duplo clique no arquivo [`iniciar.bat`](file:///C:/Users/santh/OneDrive/Área%20de%20Trabalho/ARQQUIVOS%20DRIVE/Beta%20OS/exemplos/yvira_network_monitor/iniciar.bat) ou execute no terminal:

```powershell
cd "exemplos\yvira_network_monitor"
dotnet run --project "..\..\Yvira.Cli" -- "main.yv"
```

---

## 🛠️ Tecnologias e Funcionalidades

1. **Varredura Contínua com `@time`:**
   - `@time.sincronizarRede(5)`: Executa a auditoria a cada 5 segundos de forma assíncrona.
2. **Reatividade Vue.js 3:**
   - Recebe eventos emitidos via IPC pelo Yvira (`app.emitir("receber_dados_rede", ...)`) e atualiza reativamente os dados na tela.
3. **Inspeção de Host:**
   - Detecção do fabricante (TP-Link, Cisco, Intel, Samsung, Intelbras).
   - Nome do host e grupo de trabalho via NetBIOS (UDP 137).
   - Portas abertas e banners de serviços (SSH, SMB, DNS, HTTP).
   - Telemetria SNMP para switches e roteadores.
4. **Controle Remoto de Equipamentos:**
   - Botão para disparar testes de **Ping** instantâneo.
   - Botão para disparar pacote **Wake-on-LAN (Magic Packet)** para o MAC do dispositivo selecionado.
