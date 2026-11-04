using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

namespace Yvira.Vm;

/// <summary>
/// Módulo nativo 'switch' e 'rede' da linguagem Yvira.
/// Fornece controle de switches e roteadores via SSH/Telnet, leitura/escrita SNMP,
/// scanner de portas e rede, ping, wake-on-lan e métricas de adaptadores em tempo real.
/// </summary>
public static class SwitchModule
{
    public static YviraMap CreateModule(Interpreter? interpreter = null)
    {
        var methods = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // 1. switch.conectar({ host, usuario, senha, porta?, timeout? }) -> Instância de Switch Remoto
        methods["conectar"] = new HostFunction("switch.conectar", (interp, args) =>
        {
            if (args.Count == 0 || args[0] is not YviraMap config)
                throw new Exception("switch.conectar requer um mapa com as configurações { host, usuario, senha, [porta] }.");

            string host = config.Get("host")?.ToString() ?? throw new Exception("host é obrigatório em switch.conectar.");
            string usuario = config.Get("usuario")?.ToString() ?? "admin";
            string senha = config.Get("senha")?.ToString() ?? "";
            int porta = config.Get("porta") != null && int.TryParse(config.Get("porta")!.ToString(), out int p) ? p : 22;
            int timeoutSec = config.Get("timeout") != null && int.TryParse(config.Get("timeout")!.ToString(), out int t) ? t : 15;

            var connInfo = new ConnectionInfo(host, porta, usuario,
                new PasswordAuthenticationMethod(usuario, senha))
            {
                Timeout = TimeSpan.FromSeconds(timeoutSec)
            };

            var client = new SshClient(connInfo);
            try
            {
                client.Connect();
            }
            catch (Exception ex)
            {
                throw new Exception($"Falha ao conectar no switch {host}:{porta} via SSH: {ex.Message}");
            }

            return CreateSwitchInstance(client, host, usuario, porta);
        });

        // 2. switch.ping(host, timeoutMs?) -> { sucesso, host, tempo_ms, status }
        methods["ping"] = new HostFunction("switch.ping", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("switch.ping requer o endereço IP ou hostname como primeiro argumento.");

            string host = args[0]!.ToString()!;
            int timeout = args.Count > 1 && args[1] != null && int.TryParse(args[1]!.ToString(), out int t) ? t : 2000;

            using var pinger = new Ping();
            try
            {
                var reply = pinger.Send(host, timeout);
                var res = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["sucesso"] = reply.Status == IPStatus.Success,
                    ["host"] = host,
                    ["ip"] = reply.Address?.ToString() ?? host,
                    ["tempo_ms"] = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1,
                    ["status"] = reply.Status.ToString(),
                    ["ttl"] = reply.Options != null ? reply.Options.Ttl : 0
                };
                return new YviraMap(res);
            }
            catch (Exception ex)
            {
                return new YviraMap(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["sucesso"] = false,
                    ["host"] = host,
                    ["tempo_ms"] = -1,
                    ["status"] = "Erro: " + ex.Message
                });
            }
        });

        // 3. switch.scan_portas(host, portas_lista, timeoutMs?) -> [ { porta, aberta } ]
        methods["scan_portas"] = new HostFunction("switch.scan_portas", (interp, args) =>
        {
            if (args.Count < 2)
                throw new Exception("switch.scan_portas requer (host, portas_lista, [timeoutMs]).");

            string host = args[0]?.ToString() ?? "127.0.0.1";
            var portObj = args[1];
            int timeout = args.Count > 2 && args[2] != null && int.TryParse(args[2]!.ToString(), out int t) ? t : 800;

            var targetPorts = new List<int>();
            if (portObj is YviraList yList)
            {
                foreach (var item in yList.Items)
                {
                    if (item != null && int.TryParse(item.ToString(), out int p)) targetPorts.Add(p);
                }
            }
            else if (portObj != null && int.TryParse(portObj.ToString(), out int singlePort))
            {
                targetPorts.Add(singlePort);
            }

            var results = new List<object?>();
            Parallel.ForEach(targetPorts, port =>
            {
                bool isOpen = false;
                try
                {
                    using var tcpClient = new TcpClient();
                    var connectTask = tcpClient.ConnectAsync(host, port);
                    if (connectTask.Wait(timeout))
                    {
                        isOpen = tcpClient.Connected;
                    }
                }
                catch { }

                lock (results)
                {
                    results.Add(new YviraMap(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["porta"] = port,
                        ["aberta"] = isOpen
                    }));
                }
            });

            return new YviraList(results);
        });

        // 4. switch.snmp_get(host, oid, community?, timeoutMs?) -> { oid, valor, tipo }
        methods["snmp_get"] = new HostFunction("switch.snmp_get", (interp, args) =>
        {
            if (args.Count < 2)
                throw new Exception("switch.snmp_get requer (host, oid, [community='public'], [timeoutMs=2000]).");

            string host = args[0]?.ToString() ?? "";
            string oidStr = args[1]?.ToString() ?? "";
            string community = args.Count > 2 && args[2] != null ? args[2]!.ToString()! : "public";
            int timeout = args.Count > 3 && args[3] != null && int.TryParse(args[3]!.ToString(), out int t) ? t : 2000;

            try
            {
                if (!IPAddress.TryParse(host, out var ip))
                {
                    var addresses = Dns.GetHostAddresses(host);
                    if (addresses.Length > 0) ip = addresses[0];
                    else throw new Exception($"Não foi possível resolver o IP para o host: {host}");
                }

                var endpoint = new IPEndPoint(ip, 161);
                var vList = new List<Variable> { new Variable(new ObjectIdentifier(oidStr)) };
                var result = Messenger.Get(VersionCode.V2, endpoint, new OctetString(community), vList, timeout);

                if (result.Count > 0)
                {
                    var v = result[0];
                    return new YviraMap(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["oid"] = v.Id.ToString(),
                        ["valor"] = v.Data.ToString(),
                        ["tipo"] = v.Data.GetType().Name
                    });
                }
                throw new Exception("Nenhum dado retornado pela consulta SNMP.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro SNMP Get em {host}: {ex.Message}");
            }
        });

        // 5. switch.snmp_walk(host, baseOid, community?, timeoutMs?) -> Lista de { oid, valor }
        methods["snmp_walk"] = new HostFunction("switch.snmp_walk", (interp, args) =>
        {
            if (args.Count < 2)
                throw new Exception("switch.snmp_walk requer (host, baseOid, [community='public'], [timeoutMs=3000]).");

            string host = args[0]?.ToString() ?? "";
            string oidStr = args[1]?.ToString() ?? "";
            string community = args.Count > 2 && args[2] != null ? args[2]!.ToString()! : "public";
            int timeout = args.Count > 3 && args[3] != null && int.TryParse(args[3]!.ToString(), out int t) ? t : 3000;

            try
            {
                if (!IPAddress.TryParse(host, out var ip))
                {
                    var addresses = Dns.GetHostAddresses(host);
                    if (addresses.Length > 0) ip = addresses[0];
                    else throw new Exception($"Não foi possível resolver o IP para o host: {host}");
                }

                var endpoint = new IPEndPoint(ip, 161);
                var results = new List<object?>();
                var targetOid = new ObjectIdentifier(oidStr);

                var walkList = new List<Variable>();
                Messenger.Walk(VersionCode.V2, endpoint, new OctetString(community), targetOid, walkList, timeout, WalkMode.WithinSubtree);

                foreach (var v in walkList)
                {
                    results.Add(new YviraMap(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["oid"] = v.Id.ToString(),
                        ["valor"] = v.Data.ToString()
                    }));
                }

                return new YviraList(results);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro SNMP Walk em {host}: {ex.Message}");
            }
        });

        // 6. switch.wake_on_lan(macAddress) -> Envia Magic Packet UDP
        methods["wake_on_lan"] = new HostFunction("switch.wake_on_lan", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("switch.wake_on_lan requer o endereço MAC (ex: '00:11:22:33:44:55' ou '00-11-22-33-44-55').");

            string mac = args[0]!.ToString()!.Replace(":", "").Replace("-", "").Trim();
            if (mac.Length != 12)
                throw new Exception("Endereço MAC inválido para Wake-on-LAN. Deve conter 12 caracteres hexadecimais.");

            byte[] macBytes = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                macBytes[i] = Convert.ToByte(mac.Substring(i * 2, 2), 16);
            }

            // Pacote mágico: 6 bytes 0xFF seguidos de 16 repetições do MAC
            byte[] packet = new byte[102];
            for (int i = 0; i < 6; i++) packet[i] = 0xFF;
            for (int i = 1; i <= 16; i++)
            {
                Buffer.BlockCopy(macBytes, 0, packet, i * 6, 6);
            }

            using var client = new UdpClient();
            client.EnableBroadcast = true;
            client.Send(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 9));

            return true;
        });

        // 7. switch.obter_adaptadores() -> Lista de adaptadores e IPs da máquina local
        methods["obter_adaptadores"] = new HostFunction("switch.obter_adaptadores", (interp, args) =>
        {
            var ifaces = NetworkInterface.GetAllNetworkInterfaces();
            var list = new List<object?>();

            foreach (var iface in ifaces)
            {
                var ipProps = iface.GetIPProperties();
                var ips = new List<object?>();
                foreach (var u in ipProps.UnicastAddresses)
                {
                    if (u.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ips.Add(u.Address.ToString());
                    }
                }

                var stats = iface.GetIPStatistics();
                string ipPrincipal = ips.Count > 0 ? ips[0]?.ToString() ?? "" : "";
                var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["id"] = iface.Id,
                    ["nome"] = iface.Name,
                    ["descricao"] = iface.Description,
                    ["tipo"] = iface.NetworkInterfaceType.ToString(),
                    ["status"] = iface.OperationalStatus.ToString(),
                    ["mac"] = iface.GetPhysicalAddress().ToString(),
                    ["ip"] = ipPrincipal,
                    ["velocidade_mbps"] = iface.Speed / 1_000_000,
                    ["ips"] = new YviraList(ips),
                    ["bytes_recebidos"] = stats.BytesReceived,
                    ["bytes_enviados"] = stats.BytesSent
                };
                list.Add(new YviraMap(map));
            }

            return new YviraList(list);
        });

        // 8. switch.obter_conexoes_tcp() -> Lista de conexões ativas do sistema operacional
        methods["obter_conexoes_tcp"] = new HostFunction("switch.obter_conexoes_tcp", (interp, args) =>
        {
            var ipProps = IPGlobalProperties.GetIPGlobalProperties();
            var conns = ipProps.GetActiveTcpConnections();
            var list = new List<object?>();

            foreach (var c in conns)
            {
                list.Add(new YviraMap(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["local_ip"] = c.LocalEndPoint.Address.ToString(),
                    ["local_porta"] = c.LocalEndPoint.Port,
                    ["remoto_ip"] = c.RemoteEndPoint.Address.ToString(),
                    ["remoto_porta"] = c.RemoteEndPoint.Port,
                    ["estado"] = c.State.ToString()
                }));
            }

            return new YviraList(list);
        });

        // 9. switch.scan_rede(subredeOuIp, timeoutMs?, resolverHostname?) -> Mapeia e descobre dispositivos na LAN
        methods["scan_rede"] = new HostFunction("switch.scan_rede", (interp, args) =>
        {
            string alvo = "";
            if (args.Count > 0 && args[0] != null)
            {
                alvo = args[0]!.ToString()!.Trim();
            }
            if (string.IsNullOrEmpty(alvo))
            {
                alvo = ObterSubredePadrao();
            }

            int timeout = args.Count > 1 && args[1] != null && int.TryParse(args[1]!.ToString(), out int t) ? t : 400;
            bool resolverHost = args.Count > 2 && args[2] is bool b ? b : false;

            var ips = GerarListaDeIps(alvo);
            var ativos = new ConcurrentBag<YviraMap>();

            Parallel.ForEach(ips, new ParallelOptions { MaxDegreeOfParallelism = 64 }, ipStr =>
            {
                var info = SondarHost(ipStr, timeout, resolverHost);
                if (info != null)
                {
                    ativos.Add(info);
                }
            });

            var listaFinal = new List<object?>();
            foreach (var dev in ativos)
            {
                listaFinal.Add(dev);
            }

            // Ordena os resultados por IP
            listaFinal.Sort((a, b) =>
            {
                if (a is YviraMap ma && b is YviraMap mb)
                {
                    return string.Compare(ma.Get("ip")?.ToString(), mb.Get("ip")?.ToString(), StringComparison.Ordinal);
                }
                return 0;
            });

            return new YviraList(listaFinal);
        });

        // 10. switch.obter_arp_tabela() -> Retorna o cache ARP e mapeamento IP -> MAC
        methods["obter_arp_tabela"] = new HostFunction("switch.obter_arp_tabela", (interp, args) =>
        {
            return new YviraList(ObterTabelaArp());
        });

        // 11. switch.mapear_rede(configMapa) -> Descoberta profunda L2-L7 (OUI, NetBIOS, Banner, Fingerprint, SNMP)
        methods["mapear_rede"] = new HostFunction("switch.mapear_rede", (interp, args) =>
        {
            string alvo = "";
            int timeoutMs = 350;
            bool banners = true;
            bool nomes = true;
            bool snmp = true;
            string snmpCommunity = "public";

            if (args.Count > 0 && args[0] is YviraMap cfg)
            {
                alvo = cfg.Get("alvo")?.ToString()?.Trim() ?? "";
                if (cfg.Get("timeout_ms") != null && int.TryParse(cfg.Get("timeout_ms")!.ToString(), out int t))
                    timeoutMs = t;
                if (cfg.Get("coletar_banners") is bool b)
                    banners = b;
                if (cfg.Get("identificar_nomes") is bool n)
                    nomes = n;
                if (cfg.Get("tentar_snmp") is bool s)
                    snmp = s;
                if (cfg.Get("snmp_community") != null)
                    snmpCommunity = cfg.Get("snmp_community")!.ToString()!;
            }
            else if (args.Count > 0 && args[0] is string str)
            {
                alvo = str;
            }

            if (string.IsNullOrEmpty(alvo))
            {
                alvo = ObterSubredePadrao();
            }

            var ips = GerarListaDeIps(alvo);
            var ativos = new ConcurrentBag<YviraMap>();

            // Sondagem L2 primeiro para filtrar apenas hosts ligados
            Parallel.ForEach(ips, new ParallelOptions { MaxDegreeOfParallelism = 64 }, ipStr =>
            {
                var l2 = SondarHost(ipStr, timeoutMs, false);
                if (l2 != null)
                {
                    ativos.Add(l2);
                }
            });

            // Agora realiza a auditoria aprofundada (L3-L7) nos hosts ativos
            var resultados = new ConcurrentBag<YviraMap>();
            Parallel.ForEach(ativos, new ParallelOptions { MaxDegreeOfParallelism = 16 }, dev =>
            {
                string ip = dev.Get("ip")?.ToString() ?? "";
                string mac = dev.Get("mac")?.ToString() ?? "";
                long latencia = dev.Get("tempo_ms") != null && long.TryParse(dev.Get("tempo_ms")!.ToString(), out long lat) ? lat : 1;

                var deep = AuditoriaProfundaHost(ip, mac, latencia, timeoutMs, banners, nomes, snmp, snmpCommunity);
                resultados.Add(deep);
            });

            var listaFinal = new List<object?>();
            foreach (var r in resultados)
            {
                listaFinal.Add(r);
            }

            listaFinal.Sort((a, b) =>
            {
                if (a is YviraMap ma && b is YviraMap mb)
                {
                    return string.Compare(ma.Get("ip")?.ToString(), mb.Get("ip")?.ToString(), StringComparison.Ordinal);
                }
                return 0;
            });

            return new YviraList(listaFinal);
        });

        // 12. switch.resolver_fabricante(mac) -> Nome do Fabricante IEEE
        methods["resolver_fabricante"] = new HostFunction("switch.resolver_fabricante", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null) return "Desconhecido";
            return ResolverFabricanteOUI(args[0]!.ToString()!);
        });

        // 13. switch.obter_netbios_nome(ip, timeoutMs?) -> Nome NetBIOS / Workgroup
        methods["obter_netbios_nome"] = new HostFunction("switch.obter_netbios_nome", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null) return "";
            string ip = args[0]!.ToString()!;
            int timeout = args.Count > 1 && args[1] != null && int.TryParse(args[1]!.ToString(), out int t) ? t : 800;
            return ObterNomeNetBios(ip, timeout);
        });

        return new YviraMap(methods);
    }

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref uint PhyAddrLen);

    private static string ObterSubredePadrao()
    {
        try
        {
            // Procura primeiro por interfaces Ethernet ou Wireless operacionais
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.OperationalStatus != OperationalStatus.Up ||
                    iface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                // Evitar interfaces virtuais vEthernet prioritariamente se houver interface física
                if (iface.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
                    iface.Name.Contains("vEthernet", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var unicast in iface.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var bytes = unicast.Address.GetAddressBytes();
                        // Ignorar endereços APIPA 169.254.x.x
                        if (bytes[0] == 169 && bytes[1] == 254) continue;
                        return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.0/24";
                    }
                }
            }

            // Fallback: qualquer interface Up com IPv4
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.OperationalStatus != OperationalStatus.Up ||
                    iface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                foreach (var unicast in iface.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var bytes = unicast.Address.GetAddressBytes();
                        if (bytes[0] == 169 && bytes[1] == 254) continue;
                        return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.0/24";
                    }
                }
            }
        }
        catch { }
        return "192.168.0.0/24";
    }

    private static List<string> GerarListaDeIps(string alvo)
    {
        var lista = new List<string>();
        alvo = alvo.Trim();

        // Trata notação CIDR, ex: 192.168.1.0/24
        if (alvo.Contains('/'))
        {
            var partes = alvo.Split('/');
            string baseIpStr = partes[0];
            int prefixo = int.TryParse(partes[1], out int p) ? p : 24;

            if (IPAddress.TryParse(baseIpStr, out var baseIp) && baseIp.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] b = baseIp.GetAddressBytes();
                if (prefixo == 24)
                {
                    for (int i = 1; i <= 254; i++)
                    {
                        lista.Add($"{b[0]}.{b[1]}.{b[2]}.{i}");
                    }
                    return lista;
                }
            }
        }

        // Trata formato de faixa, ex: 192.168.1.1-254
        if (alvo.Contains('-'))
        {
            var partes = alvo.Split('-');
            string inicioStr = partes[0];
            if (IPAddress.TryParse(inicioStr, out var inicioIp))
            {
                byte[] b = inicioIp.GetAddressBytes();
                int fim = int.TryParse(partes[1], out int f) ? f : 254;
                for (int i = b[3]; i <= fim && i <= 254; i++)
                {
                    lista.Add($"{b[0]}.{b[1]}.{b[2]}.{i}");
                }
                return lista;
            }
        }

        // Se for um IP individual ou base (ex: 192.168.1.0 ou 192.168.1.1)
        if (IPAddress.TryParse(alvo, out var singleIp))
        {
            byte[] b = singleIp.GetAddressBytes();
            if (b[3] == 0)
            {
                // Varre a subrede /24 se passou terminado em .0
                for (int i = 1; i <= 254; i++)
                {
                    lista.Add($"{b[0]}.{b[1]}.{b[2]}.{i}");
                }
                return lista;
            }
            lista.Add(alvo);
            return lista;
        }

        // Fallback
        lista.Add(alvo);
        return lista;
    }

    private static YviraMap? SondarHost(string ipStr, int timeoutMs, bool resolverHostname)
    {
        if (!IPAddress.TryParse(ipStr, out var ip))
            return null;

        string macStr = "";
        bool ativo = false;
        long tempoMs = -1;

        // 1. Tentar ARP direto (extremamente rápido e detecta mesmo se o firewall do Windows bloquear ICMP)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && ip.AddressFamily == AddressFamily.InterNetwork)
        {
            try
            {
                byte[] macBytes = new byte[6];
                uint len = (uint)macBytes.Length;
                byte[] ipBytes = ip.GetAddressBytes();
                int destIpInt = BitConverter.ToInt32(ipBytes, 0);

                int arpRes = SendARP(destIpInt, 0, macBytes, ref len);
                if (arpRes == 0 && len == 6)
                {
                    // Ignora MAC zerado
                    bool todosZeros = true;
                    for (int i = 0; i < 6; i++)
                    {
                        if (macBytes[i] != 0) { todosZeros = false; break; }
                    }

                    if (!todosZeros)
                    {
                        macStr = BitConverter.ToString(macBytes, 0, 6).Replace("-", ":");
                        ativo = true;
                        tempoMs = 1; // Resposta imediata local via camada L2
                    }
                }
            }
            catch { }
        }

        // 2. Se ARP não deu resposta (ou não-Windows), faz Ping ICMP apenas se timeoutMs > 0
        // Em LAN, se SendARP falhou em Windows, o host provavelmente não existe na subrede.
        // Mas se timeoutMs for informado e SendARP não tiver sido chamado, usa ICMP.
        if (!ativo && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var pinger = new Ping();
            try
            {
                var reply = pinger.Send(ip, timeoutMs);
                if (reply.Status == IPStatus.Success)
                {
                    ativo = true;
                    tempoMs = reply.RoundtripTime;
                }
            }
            catch { }
        }

        if (!ativo)
            return null;

        string hostName = "";
        if (resolverHostname)
        {
            try
            {
                var hostEntry = Dns.GetHostEntry(ip);
                hostName = hostEntry.HostName ?? "";
            }
            catch { }
        }

        var dev = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ip"] = ipStr,
            ["mac"] = macStr,
            ["tempo_ms"] = tempoMs,
            ["hostname"] = hostName,
            ["status"] = "Ativo"
        };

        return new YviraMap(dev);
    }

    private static List<object?> ObterTabelaArp()
    {
        var list = new List<object?>();
        try
        {
            var p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "arp";
            p.StartInfo.Arguments = "-a";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length >= 3 && IPAddress.TryParse(tokens[0], out _))
                {
                    list.Add(new YviraMap(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["ip"] = tokens[0],
                        ["mac"] = tokens[1],
                        ["tipo"] = tokens[2]
                    }));
                }
            }
        }
        catch { }
        return list;
    }

    private static YviraMap AuditoriaProfundaHost(
        string ip, string mac, long latenciaMs, int timeoutMs,
        bool coletarBanners, bool identificarNomes, bool testarSnmp, string snmpCommunity)
    {
        var res = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        res["ip"] = ip;
        res["mac"] = mac;
        res["latencia_ms"] = latenciaMs;
        res["status"] = "Ativo";

        // 1. Fabricante pelo MAC (IEEE OUI)
        string fabricante = ResolverFabricanteOUI(mac);
        res["fabricante"] = fabricante;

        // 2. Resolução de Nomes (NetBIOS / DNS)
        string nomeHost = "";
        string workgroup = "";

        if (identificarNomes)
        {
            try
            {
                var nb = ObterNetBiosDetalhes(ip, Math.Min(timeoutMs, 400));
                if (!string.IsNullOrEmpty(nb.nome))
                {
                    nomeHost = nb.nome;
                    workgroup = nb.workgroup;
                }
            }
            catch { }

            if (string.IsNullOrEmpty(nomeHost))
            {
                try
                {
                    var dnsEntry = Dns.GetHostEntry(ip);
                    nomeHost = dnsEntry.HostName ?? "";
                }
                catch { }
            }
        }
        res["nome"] = nomeHost;
        res["workgroup"] = workgroup;

        // 3. Portas e Banners de Serviços
        var portasInteresse = new[] { 21, 22, 23, 53, 80, 443, 445, 3389, 8080, 8291, 9100 };
        var servicosAbertos = new List<object?>();
        var portasAbertas = new List<int>();
        var bannersColetados = new Dictionary<string, string>();

        foreach (var p in portasInteresse)
        {
            if (TestarPortaAberta(ip, p, Math.Min(timeoutMs, 250), out string banner))
            {
                portasAbertas.Add(p);
                var sMap = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["porta"] = p,
                    ["servico"] = NomeServicoPorta(p),
                    ["banner"] = banner
                };
                servicosAbertos.Add(new YviraMap(sMap));
                if (!string.IsNullOrEmpty(banner))
                {
                    bannersColetados[p.ToString()] = banner;
                }
            }
        }
        res["servicos"] = new YviraList(servicosAbertos);

        // 4. Classificação de Categoria / Tipo de Dispositivo (Fingerprint)
        string categoria = ClassificarDispositivo(fabricante, portasAbertas, bannersColetados, nomeHost);
        res["categoria"] = categoria;

        // 5. Auditoria SNMP Profunda (se habilitada)
        bool snmpAtivo = false;
        string snmpDescricao = "";
        string snmpNome = "";
        string snmpUptime = "";
        int snmpPortasUp = 0;
        int snmpPortasTotal = 0;

        if (testarSnmp)
        {
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(ip), 161);
                var comm = new OctetString(snmpCommunity);
                var oids = new List<Variable>
                {
                    new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.1.0")), // sysDescr
                    new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.5.0")), // sysName
                    new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.3.0"))  // sysUpTime
                };

                var snmpResult = Messenger.Get(VersionCode.V2, endpoint, comm, oids, Math.Min(timeoutMs, 500));
                if (snmpResult != null && snmpResult.Count > 0)
                {
                    snmpAtivo = true;
                    foreach (var v in snmpResult)
                    {
                        var oidStr = v.Id.ToString();
                        if (oidStr == "1.3.6.1.2.1.1.1.0") snmpDescricao = v.Data.ToString();
                        else if (oidStr == "1.3.6.1.2.1.1.5.0") snmpNome = v.Data.ToString();
                        else if (oidStr == "1.3.6.1.2.1.1.3.0") snmpUptime = v.Data.ToString();
                    }

                    // Tentar contar portas Up/Down se for Switch/Roteador
                    try
                    {
                        var ifacesResult = new List<Variable>();
                        Messenger.Walk(VersionCode.V2, endpoint, comm, new ObjectIdentifier("1.3.6.1.2.1.2.2.1.8"), ifacesResult, Math.Min(timeoutMs, 600), WalkMode.WithinSubtree);
                        snmpPortasTotal = ifacesResult.Count;
                        foreach (var iface in ifacesResult)
                        {
                            if (iface.Data.ToString() == "1") // 1 = Up
                                snmpPortasUp++;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        res["snmp_ativo"] = snmpAtivo;
        res["snmp_nome"] = snmpNome;
        res["snmp_descricao"] = snmpDescricao;
        res["snmp_uptime"] = snmpUptime;
        res["snmp_portas_ativas"] = snmpPortasUp;
        res["snmp_portas_total"] = snmpPortasTotal;

        return new YviraMap(res);
    }

    private static bool TestarPortaAberta(string host, int porta, int timeoutMs, out string banner)
    {
        banner = "";
        try
        {
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync(host, porta);
            if (!connectTask.Wait(timeoutMs))
                return false;

            if (!tcp.Connected)
                return false;

            // Se conectou, tenta ler banner com timeout curto
            try
            {
                var stream = tcp.GetStream();
                stream.ReadTimeout = 250;

                // Para HTTP/HTTPS envia HEAD simples
                if (porta == 80 || porta == 8080)
                {
                    byte[] req = Encoding.ASCII.GetBytes("HEAD / HTTP/1.0\r\n\r\n");
                    stream.Write(req, 0, req.Length);
                }

                byte[] buf = new byte[512];
                int read = stream.Read(buf, 0, buf.Length);
                if (read > 0)
                {
                    string raw = Encoding.UTF8.GetString(buf, 0, read).Trim();
                    // Extrai primeira linha ou header de Server
                    var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var l in lines)
                    {
                        if (l.StartsWith("SSH-", StringComparison.OrdinalIgnoreCase) ||
                            l.StartsWith("Server:", StringComparison.OrdinalIgnoreCase) ||
                            l.StartsWith("220", StringComparison.Ordinal))
                        {
                            banner = l.Trim();
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(banner) && lines.Length > 0)
                        banner = lines[0].Trim();
                }
            }
            catch { }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ClassificarDispositivo(
        string fabricante, List<int> portas, Dictionary<string, string> banners, string hostname)
    {
        string fLower = fabricante.ToLowerInvariant();
        string hLower = hostname.ToLowerInvariant();

        // 1. MikroTik
        if (portas.Contains(8291) || fLower.Contains("mikrotik") ||
            (banners.TryGetValue("22", out var b22) && b22.Contains("ROUTEROS", StringComparison.OrdinalIgnoreCase)))
        {
            return "Roteador / Switch MikroTik";
        }

        // 2. Cisco
        if (fLower.Contains("cisco") || (banners.TryGetValue("22", out var bc) && bc.Contains("Cisco", StringComparison.OrdinalIgnoreCase)))
        {
            return "Switch / Roteador Cisco";
        }

        // 3. Ubiquiti
        if (fLower.Contains("ubiquiti") || fLower.Contains("unifi"))
        {
            return "Switch / AP Ubiquiti UniFi";
        }

        // 4. Huawei / TP-Link / Intelbras / D-Link
        if (fLower.Contains("tp-link") || fLower.Contains("intelbras") || fLower.Contains("d-link") || fLower.Contains("huawei"))
        {
            return "Roteador / Switch (" + fabricante + ")";
        }

        // 5. Impressora
        if (portas.Contains(9100) || portas.Contains(515) || portas.Contains(631) ||
            hLower.Contains("print") || fLower.Contains("epson") || fLower.Contains("hp") || fLower.Contains("brother"))
        {
            return "Impressora de Rede";
        }

        // 6. Windows PC / Server
        if (portas.Contains(445) || portas.Contains(3389) || hLower.StartsWith("desktop-") || hLower.StartsWith("laptop-"))
        {
            return "Estação de Trabalho Windows";
        }

        // 7. Servidor Linux / Unix
        if (portas.Contains(22) && !portas.Contains(445))
        {
            return "Servidor Linux / Unix";
        }

        // 8. Câmera IP / DVR
        if (portas.Contains(554) || portas.Contains(8000) || fLower.Contains("hikvision") || fLower.Contains("dahua"))
        {
            return "Câmera IP / CFTV";
        }

        // 9. Smartphone / Dispositivo Móvel
        if (fLower.Contains("apple") || fLower.Contains("samsung") || fLower.Contains("xiaomi") || fLower.Contains("motorola"))
        {
            return "Dispositivo Móvel (" + fabricante + ")";
        }

        // 10. Gateway / Roteador genérico
        if (portas.Contains(80) || portas.Contains(443) || portas.Contains(53))
        {
            return "Equipamento de Rede / Roteador";
        }

        return "Dispositivo de Rede Genérico";
    }

    private static string NomeServicoPorta(int porta)
    {
        return porta switch
        {
            21 => "FTP",
            22 => "SSH",
            23 => "Telnet",
            53 => "DNS",
            80 => "HTTP (Web)",
            443 => "HTTPS (Web Seguro)",
            445 => "SMB (Compartilhamento Windows)",
            3389 => "RDP (Área de Trabalho Remota)",
            8080 => "HTTP-Proxy / Web Alt",
            8291 => "MikroTik Winbox",
            9100 => "Impressora RAW (JetDirect)",
            _ => "Porta " + porta
        };
    }

    private static (string nome, string workgroup) ObterNetBiosDetalhes(string ipStr, int timeoutMs)
    {
        try
        {
            using var udp = new UdpClient();
            udp.Client.ReceiveTimeout = timeoutMs;
            udp.Client.SendTimeout = timeoutMs;

            // Pacote padrão NetBIOS Node Status Request para *<00...00>
            byte[] query = new byte[]
            {
                0x80, 0x94, // Transaction ID
                0x00, 0x00, // Flags (Query)
                0x00, 0x01, // Questions: 1
                0x00, 0x00, // Answer RRs
                0x00, 0x00, // Authority RRs
                0x00, 0x00, // Additional RRs
                // Name: CKAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA (32 bytes)
                0x20, 0x43, 0x4B, 0x41, 0x41, 0x41, 0x41, 0x41,
                0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41,
                0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41,
                0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41,
                0x41, 0x00, // Terminator
                0x00, 0x21, // Type: NBSTAT
                0x00, 0x01  // Class: IN
            };

            var ep = new IPEndPoint(IPAddress.Parse(ipStr), 137);
            udp.Send(query, query.Length, ep);

            IPEndPoint? senderEp = null;
            byte[] resp = udp.Receive(ref senderEp);

            if (resp.Length > 56)
            {
                int numNames = resp[56];
                int offset = 57;
                string compName = "";
                string groupName = "";

                for (int i = 0; i < numNames && offset + 18 <= resp.Length; i++)
                {
                    string n = Encoding.ASCII.GetString(resp, offset, 15).Trim();
                    byte typeByte = resp[offset + 15];
                    byte flags = resp[offset + 16];
                    bool isGroup = (flags & 0x80) != 0;

                    if (!isGroup && typeByte == 0x00 && string.IsNullOrEmpty(compName))
                    {
                        compName = n;
                    }
                    else if (isGroup && typeByte == 0x00 && string.IsNullOrEmpty(groupName))
                    {
                        groupName = n;
                    }

                    offset += 18;
                }

                return (compName, groupName);
            }
        }
        catch { }
        return ("", "");
    }

    private static string ObterNomeNetBios(string ip, int timeoutMs)
    {
        var nb = ObterNetBiosDetalhes(ip, timeoutMs);
        if (!string.IsNullOrEmpty(nb.nome))
        {
            return !string.IsNullOrEmpty(nb.workgroup) ? $"{nb.nome} ({nb.workgroup})" : nb.nome;
        }
        return "";
    }

    private static readonly ConcurrentDictionary<string, string> _tabelaOuiCustomizada = new(StringComparer.OrdinalIgnoreCase);
    private static bool _ouiCarregado = false;
    private static readonly object _ouiLock = new object();

    private static void GarantirOuiCarregado()
    {
        if (_ouiCarregado) return;
        lock (_ouiLock)
        {
            if (_ouiCarregado) return;
            try
            {
                // Tenta carregar o arquivo kismet_manuf.txt se estiver no diretório de trabalho ou relativo
                string[] caminhosTentativa = new[]
                {
                    "kismet_manuf.txt",
                    System.IO.Path.Combine(AppContext.BaseDirectory, "kismet_manuf.txt"),
                    System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "kismet_manuf.txt"),
                    System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "exemplos", "yvira_network_monitor", "kismet_manuf.txt"),
                    @"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\exemplos\yvira_network_monitor\kismet_manuf.txt"
                };

                foreach (var caminho in caminhosTentativa)
                {
                    if (System.IO.File.Exists(caminho))
                    {
                        using var reader = new System.IO.StreamReader(caminho);
                        string? linha;
                        while ((linha = reader.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(linha) || linha.StartsWith("#")) continue;
                            var partes = linha.Split(new[] { '\t', ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                            if (partes.Length >= 2)
                            {
                                string prefix = partes[0].Replace(":", "").Replace("-", "").Trim().ToUpperInvariant();
                                if (prefix.Length == 6)
                                {
                                    _tabelaOuiCustomizada[prefix] = partes[1].Trim();
                                }
                            }
                        }
                        break;
                    }
                }
            }
            catch { }
            _ouiCarregado = true;
        }
    }

    private static string ResolverFabricanteOUI(string mac)
    {
        if (string.IsNullOrWhiteSpace(mac)) return "Desconhecido";

        string cleanMac = mac.Replace(":", "").Replace("-", "").ToUpperInvariant();
        if (cleanMac.Length < 6) return "Desconhecido";

        string prefixo = cleanMac.Substring(0, 6);

        // 1. Tenta buscar na base completa carregada do kismet_manuf.txt (+59.000 fabricantes)
        GarantirOuiCarregado();
        if (_tabelaOuiCustomizada.TryGetValue(prefixo, out var fabricanteArquivo))
        {
            return fabricanteArquivo;
        }

        // 2. Dicionário IEEE OUI padrão dos principais fabricantes de infraestrutura de rede, TI e mobile
        return prefixo switch
        {
            // TP-Link
            "841EA3" or "50C7BF" or "E848B8" or "003192" or "984827" or "6032B1" or "F4F26D" or "30B5C2" => "TP-Link",

            // MikroTik
            "488F5A" or "64D154" or "000C42" or "CC2DE0" or "B869F4" or "744D28" or "18FD74" or "2C6B7D" => "MikroTik",

            // Ubiquiti Networks
            "24A43C" or "788A20" or "802AA8" or "F09FC2" or "0418D6" or "DC9FDB" or "AC8BFA" or "B4FBE4" => "Ubiquiti UniFi",

            // Cisco Systems
            "00000C" or "000142" or "000143" or "000163" or "000164" or "000196" or "000197" or "0001C7" or
            "0001C9" or "000216" or "000217" or "00023B" or "00024A" or "00027D" or "00027E" or "0002B9" or
            "0002BA" or "0002FC" or "0002FD" or "000331" or "000332" or "00036B" or "00036C" or "0003A0" => "Cisco Systems",

            // Huawei
            "00E0FC" or "001882" or "001E10" or "00259E" or "200B3C" or "20F41B" or "4846FB" or "7054F5" => "Huawei",

            // Intelbras
            "001A3F" or "A0F3C1" or "5C899A" or "F801B4" or "90F652" or "606405" => "Intelbras",

            // Apple
            "F01898" or "F07959" or "F07960" or "F09838" or "F099BF" or "F0B0E7" or "F0C1F1" or "F0D5BF" or
            "F40F24" or "F41B26" or "F431C3" or "F434F0" or "F437B7" or "F45C89" or "F4F15A" or "F4F951" or
            "000393" or "000502" or "000A27" or "000A95" or "000D93" or "0010FA" or "001124" or "001451" => "Apple",

            // Samsung Electronics
            "0007AB" or "001247" or "001377" or "001599" or "001632" or "00166B" or "00166C" or "0017C9" or
            "0017D5" or "0018AF" or "001A8A" or "001B98" or "001C43" or "001D25" or "70B306" or "6CFFCE" => "Samsung",

            // Intel
            "0002B3" or "000347" or "000423" or "0007E9" or "000CF1" or "000E0C" or "000E35" or "001111" or
            "0012F0" or "001302" or "001320" or "0013E8" or "001500" or "001517" or "20C19B" => "Intel",

            // Dell
            "00065B" or "000874" or "000BDB" or "000D56" or "000F1F" or "001143" or "001217" or "00123F" or
            "001372" or "001422" or "0015C5" or "0016F0" or "00188B" or "0019B9" or "001A4B" or "001A6B" => "Dell",

            // HP / Hewlett Packard
            "0001E6" or "0002A5" or "0004EA" or "000802" or "000883" or "000BCD" or "000E7F" or "000F20" => "HP / Aruba",

            // Raspberry Pi
            "B827EB" or "DCA632" or "E45F01" or "28CDC9" => "Raspberry Pi",

            // Espressif (ESP8266 / ESP32 / IoT)
            "18FE34" or "240AC4" or "246F28" or "24B2DE" or "2C3AE8" or "30AEA4" or "84F3EB" or "A4E57C" or
            "A4CF12" or "AC67B2" or "B4E62D" or "BCDD29" or "C44F33" or "CC50E3" or "DC4F22" or "ECFAB0" => "Espressif IoT (ESP32/ESP8266)",

            // Microsoft / Hyper-V
            "00155D" or "0003FF" => "Microsoft (Hyper-V / Virtual)",

            _ => "Outro (" + prefixo + ")"
        };
    }

    /// <summary>
    /// Cria o objeto de instância de um Switch conectado via SSH.
    /// </summary>
    private static YviraMap CreateSwitchInstance(SshClient client, string host, string usuario, int porta)
    {
        var methods = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        methods["host"] = host;
        methods["usuario"] = usuario;
        methods["porta"] = porta;

        // sw.executar(comando) -> string resposta
        methods["executar"] = new HostFunction("switch.executar", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("executar requer o comando como primeiro argumento.");

            if (!client.IsConnected)
                client.Connect();

            string cmdText = args[0]!.ToString()!;
            using var cmd = client.CreateCommand(cmdText);
            cmd.CommandTimeout = TimeSpan.FromSeconds(30);
            string output = cmd.Execute();
            
            if (!string.IsNullOrEmpty(cmd.Error))
            {
                return output + "\n[ERRO]: " + cmd.Error;
            }
            return output;
        });

        // sw.desconectar() -> fecha sessao SSH
        methods["desconectar"] = new HostFunction("switch.desconectar", (interp, args) =>
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
            client.Dispose();
            return true;
        });

        // sw.conectado() -> bool
        methods["conectado"] = new HostFunction("switch.conectado", (interp, args) =>
        {
            return client.IsConnected;
        });

        return new YviraMap(methods);
    }
}
