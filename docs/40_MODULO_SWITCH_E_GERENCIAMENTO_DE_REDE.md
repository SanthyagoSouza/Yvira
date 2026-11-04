# Módulo Switch & Gerenciamento de Rede Nativo (Yvira Network Engine)

O Yvira disponibiliza o módulo nativo **`switch`** (também importável como `rede` ou `network`), projetado para gerenciamento ativo de equipamentos de infraestrutura (Switches, Roteadores, Servidores, Firewalls) e diagnóstico de rede em tempo real.

---

## 1. Importação

O módulo é embutido na biblioteca padrão do runtime e pode ser importado diretamente:

```yvira
import "switch" as Switch
# ou
import "rede" as Rede
# ou
import "network" as Net
```

---

## 2. Diagnóstico e Inspeção de Conexões Locais

### 2.1 Monitoramento de Conexões TCP Ativas (estilo `netstat`)
Permite inspecionar todas as conexões TCP abertas no sistema operacional, descobrindo para onde o navegador, serviços ou aplicativos estão enviando dados.

```yvira
let conexoes = Switch.obter_conexoes_tcp()
print("Total de conexões TCP no PC: " + conexoes.tamanho())

for (let i = 0; i < conexoes.tamanho(); i = i + 1) {
    let c = conexoes[i]
    print(c.local_ip + ":" + c.local_porta + " <=> " + c.remoto_ip + ":" + c.remoto_porta + " [" + c.estado + "]")
}
```

Cada conexão retorna:
- `local_ip`: IP local da máquina
- `local_porta`: Porta local de saída
- `remoto_ip`: IP do servidor de destino
- `remoto_porta`: Porta do servidor de destino (ex: 80, 443, 8080)
- `estado`: Estado do socket TCP (`Established`, `TimeWait`, `CloseWait`, `Listening`, etc.)

### 2.2 Auditoria de Adaptadores de Rede e Consumo de Dados
```yvira
let adaptadores = Switch.obter_adaptadores()
for (let i = 0; i < adaptadores.tamanho(); i = i + 1) {
    let a = adaptadores[i]
    if a.status == "Up" {
        print("Interface: " + a.nome + " | Tipo: " + a.tipo)
        print("Velocidade: " + a.velocidade_mbps + " Mbps | MAC: " + a.mac)
        print("Tráfego: " + a.bytes_recebidos + " bytes RX / " + a.bytes_enviados + " bytes TX")
    }
}
```

---

## 3. Conectividade e Scanner de Portas

### 3.1 Teste de Ping e Latência
```yvira
let res = Switch.ping("1.1.1.1", 1500)
if res.sucesso {
    print("Host ativo! Latência: " + res.tempo_ms + "ms | TTL: " + res.ttl)
} else {
    print("Host inacessível: " + res.status)
}
```

### 3.2 Varredura Concorrente de Portas (Port Scanner)
Realiza conexões TCP assíncronas em paralelo para identificar serviços e portas abertas em qualquer IP ou domínio:

```yvira
let portas = [22, 53, 80, 443, 8080, 8291]
let scan = Switch.scan_portas("192.168.1.1", portas, 500)

for (let i = 0; i < scan.tamanho(); i = i + 1) {
    let p = scan[i]
    print("Porta " + p.porta + " -> " + (p.aberta ? "ABERTA" : "FECHADA"))
}
```

---

## 4. Gerenciamento e Controle de Equipamentos (Switches e Roteadores)

### 4.1 Controle via SSH (Cisco, MikroTik, Linux, Huawei)
Permite autenticar em switches gerenciáveis físicos e executar comandos de configuração ou diagnóstico remotamente:

```yvira
let sw = Switch.conectar({
    host: "192.168.88.1",
    usuario: "admin",
    senha: "sua_senha_secreta",
    porta: 22,
    timeout: 15
})

if sw.conectado() {
    print("Conectado com sucesso ao switch: " + sw.host)
    
    # Executa comando de terminal
    let resposta = sw.executar("/interface print detail")
    print(resposta)
    
    # Desativa ou reinicia uma porta remotamente
    # sw.executar("/interface ethernet disable ether2")

    sw.desconectar()
}
```

### 4.2 Métricas e Gestão por SNMP (v2c)
Consulta OIDs padrão de switches (status de portas, tráfego por porta, uso de CPU):

```yvira
# Leitura direta de OID (ex: sysDescr)
let info = Switch.snmp_get("192.168.1.10", "1.3.6.1.2.1.1.1.0", "public", 2000)
print("Descrição do Switch: " + info.valor)

# Caminhada recursiva (SNMP Walk) para listar todas as interfaces
let portasStatus = Switch.snmp_walk("192.168.1.10", "1.3.6.1.2.1.2.2.1.8", "public", 3000)
for (let i = 0; i < portasStatus.tamanho(); i = i + 1) {
    let item = portasStatus[i]
    print("OID: " + item.oid + " | Status Operacional: " + item.valor)
}
```

### 4.3 Wake-on-LAN (WoL)
Dispara um Magic Packet UDP para ligar computadores e servidores na rede local a partir do endereço MAC:

```yvira
Switch.wake_on_lan("00:11:22:33:44:55")
print("Sinal Wake-on-LAN enviado!")
```

---

## 5. Mapeamento e Descoberta de Dispositivos Locais (Network Discovery)

O módulo oferece mecanismos integrados para varrer a subrede local e mapear todos os dispositivos ativos (switches, roteadores, impressoras, computadores, celulares e servidores) via resolução ARP em camada 2 e ICMP em paralelo.

### 5.1 Varredura de Sub-rede (`switch.scan_rede`)
Varre uma sub-rede CIDR (ex: `192.168.0.0/24`), uma faixa (ex: `192.168.0.1-50`) ou a subrede detectada automaticamente:

```yvira
import "switch" as Switch

# Escaneia a faixa de 1 a 50 com timeout de 150ms por host
let dispositivos = Switch.scan_rede("192.168.0.1-50", 150, false)

print("Total de dispositivos encontrados: " + dispositivos.tamanho())
for (let i = 0; i < dispositivos.tamanho(); i = i + 1) {
    let d = dispositivos[i]
    print("-> IP: " + d.ip + " | MAC: " + d.mac + " | Latência: " + d.tempo_ms + "ms | Status: " + d.status)
}
```

#### Parâmetros de `switch.scan_rede(alvo, timeoutMs?, resolverHostname?)`:
- `alvo`: String com a subrede (`"192.168.0.0/24"`), faixa (`"192.168.0.1-100"`), IP único (`"192.168.0.1"`) ou string vazia `""` para autodetectar a subrede local da interface primária.
- `timeoutMs` *(opcional)*: Tempo limite em milissegundos por verificação (padrão: `400`).
- `resolverHostname` *(opcional)*: Booleano para realizar Reverse DNS lookup e obter o hostname de cada dispositivo (padrão: `false`).

#### Objeto Retornado por Host Descoberto:
- `ip`: Endereço IPv4 do dispositivo (ex: `"192.168.0.1"`)
- `mac`: Endereço físico MAC (ex: `"84:1E:A3:8E:63:7F"`)
- `tempo_ms`: Latência de resposta em ms
- `hostname`: Nome da máquina/dispositivo (se `resolverHostname` ativo)
- `status`: `"Ativo"`

### 5.2 Tabela de Cache ARP do Sistema (`switch.obter_arp_tabela`)
Recupera a tabela de mapeamento IP-para-MAC mantida na pilha de rede do sistema operacional:

```yvira
let tabelaArp = Switch.obter_arp_tabela()
for (let i = 0; i < tabelaArp.tamanho(); i = i + 1) {
    let entrada = tabelaArp[i]
    print("IP: " + entrada.ip + " => MAC: " + entrada.mac + " (" + entrada.tipo + ")")
}
```

---

## 6. Mapeamento Profundo e Auditoria de Dispositivos (`switch.mapear_rede`)

Para além da simples detecção de IP/MAC, o Yvira possui a função **`switch.mapear_rede`**, que realiza uma auditoria em camadas (L2 a L7) correlacionando:
1. **Fabricante Real de Hardware (IEEE OUI)** a partir do endereço MAC.
2. **Nome da Máquina e Grupo de Trabalho** via NetBIOS (UDP 137) e Reverse DNS.
3. **Detecção e Banners de Serviços** (SSH, HTTP/HTTPS, SMB, RDP, DNS, MikroTik Winbox, RAW Print).
4. **Fingerprinting e Classificação Automática** do papel do dispositivo na rede (Switch/Roteador, PC Windows, Servidor Linux, Celular/Mobile, Impressora, Câmera IP).
5. **Auditoria SNMP Profunda** (Uptime real, Firmware/SO e portas ativas do switch).

### 6.1 Como Usar na Linguagem Yvira:

```yvira
import "switch" as Switch

# Executa auditoria profunda na faixa da sua rede
let dispositivos = Switch.mapear_rede({
    alvo: "192.168.0.1-50",
    timeout_ms: 300,
    coletar_banners: true,
    identificar_nomes: true,
    tentar_snmp: true,
    snmp_community: "public"
})

for (let i = 0; i < dispositivos.tamanho(); i = i + 1) {
    let d = dispositivos[i]
    print("--------------------------------------------------")
    print("IP: " + d.ip + " | MAC: " + d.mac + " | Latência: " + d.latencia_ms + "ms")
    print("Fabricante : " + d.fabricante)
    print("Categoria  : " + d.categoria)
    if d.nome != "" {
        print("Nome/Host  : " + d.nome + (d.workgroup != "" ? " (" + d.workgroup + ")" : ""))
    }
    
    # Serviços abertos identificados
    let srvs = d.servicos
    for (let s = 0; s < srvs.tamanho(); s = s + 1) {
        print("  -> " + srvs[s].servico + " [Porta " + srvs[s].porta + "]" + (srvs[s].banner != "" ? " - " + srvs[s].banner : ""))
    }

    # Se for um switch/roteador com SNMP ativo
    if d.snmp_ativo {
        print("  [SNMP Info]")
        print("  Nome do Switch: " + d.snmp_nome)
        print("  Uptime        : " + d.snmp_uptime)
        print("  Portas Físicas: " + d.snmp_portas_ativas + " ativas de " + d.snmp_portas_total + " portas")
    }
}
```

### 6.2 Utilitários Individuais
Você também pode invocar os módulos isoladamente:
* **`switch.resolver_fabricante(mac)`**: Retorna o fabricante registrado (ex: `"TP-Link"`, `"Cisco"`, `"Intelbras"`, `"MikroTik"`, `"Samsung"`, `"Intel"`, `"Apple"`).
* **`switch.obter_netbios_nome(ip, timeoutMs?)`**: Faz uma consulta NetBIOS Node Status para extrair o nome do host e grupo de trabalho de estações Windows/Samba.

---

## 7. Exemplo de Saída Real Obtida na Rede Local

Abaixo está uma amostra real do resultado gerado pela auditoria do Yvira:

```text
[IP]          : 192.168.0.1 (Latência: 1ms)
[MAC]         : 84:1E:A3:8E:63:7F
[Fabricante]  : TP-Link
[Categoria]   : Roteador / Switch (TP-Link)
[Serviços]    : Porta 53 (DNS)

[IP]          : 192.168.0.13 (Latência: 1ms)
[MAC]         : 6C:FF:CE:88:5E:7E
[Fabricante]  : Samsung
[Categoria]   : Dispositivo Móvel (Samsung)

[IP]          : 192.168.0.35 (Latência: 1ms)
[MAC]         : 20:C1:9B:60:22:69
[Fabricante]  : Intel
[Categoria]   : Estação de Trabalho Windows
[Nome / Host] : Santhyago.home
[Serviços]    : Porta 445 (SMB (Compartilhamento Windows))

[IP]          : 192.168.0.43 (Latência: 1ms)
[MAC]         : F8:01:B4:41:31:4C
[Fabricante]  : Intelbras
[Categoria]   : Roteador / Switch (Intelbras)
```

---

## 8. Base Expandida de Fabricantes IEEE OUI (Kismet Database)

O Yvira agora integra indexação automática de arquivos OUI em padrão Kismet (`kismet_manuf.txt`), permitindo identificar com precisão **mais de 59.200 fabricantes** globais de hardware:

* **Carregamento O(1):** Cache hash em memória carregado de forma assíncrona e transparente.
* **Resolução Detalhada:** Placas-mãe, modens de operadora e dispositivos IoT que antes retornavam `"Outro"` agora são resolvidos com precisão (ex: `Biostar Microtech`, `Sagemcom`, `Espressif`, `Quectel`, etc.).

---

## 9. Aplicação Modelo: Yvira Pulse (Vue.js 3 + Automação `@time`)

O projeto [`exemplos/yvira_network_monitor`](file:///C:/Users/santh/OneDrive/Área%20de%20Trabalho/ARQQUIVOS%20DRIVE/Beta%20OS/exemplos/yvira_network_monitor) demonstra a integração de toda a stack de rede nativa com interface desktop moderna:

### Arquitetura:
```
exemplos/yvira_network_monitor/
├── main.yv              # Backend nativo com janela desktop e @time.sincronizarRede(5)
├── kismet_manuf.txt     # Base de 59.200 fabricantes IEEE OUI
├── iniciar.bat          # Launcher rápido
├── README.md            # Documentação da aplicação
└── views/
    └── index.html       # Front-end reativo Vue.js 3 com Abas e Canvas
```

### Funcionalidades do Yvira Pulse:
1. **Automação Nativa com `@time`:**
   - Varredura de sub-rede periódica sem travar a interface gráfica através de `@time.sincronizarRede(5)`.
2. **Abas de Navegação:**
   - **📊 Dashboard & Dispositivos:** Tabela em tempo real com busca instantânea, badges por tipo de aparelho, contadores de portas e conexões TCP ativas, e painel lateral de inspeção para testes de Ping e Wake-on-LAN.
   - **🕸️ Topologia em Grafo:** Visualização interativa em HTML5 Canvas com o gateway central no centro, nós satélites distribuídos por categorias, feixes luminosos de tráfego simulados e seleção por clique.
3. **Ponte IPC Bidirecional:**
   - Comunicação assíncrona entre o Vue.js e o runtime Yvira via `app.emitir()` e `window.yvira.emit()`.
