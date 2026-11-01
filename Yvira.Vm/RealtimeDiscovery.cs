using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Yvira.Vm
{
    public static class RealtimeDiscovery
    {
        private const int DiscoveryPort = 7071;

        // O Servidor anuncia na rede a cada intervalo em Broadcast e Loopback
        public static CancellationTokenSource StartBeacon(int serverPort, string serverName, string secret)
        {
            var cts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                using var udp = new UdpClient();
                udp.EnableBroadcast = true;
                var broadcastEp = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
                var loopbackEp = new IPEndPoint(IPAddress.Loopback, DiscoveryPort);

                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        var packet = new
                        {
                            tipo = "yvira_beacon",
                            porta = serverPort,
                            nome = serverName,
                            segredo = secret,
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };
                        string json = JsonSerializer.Serialize(packet);
                        byte[] bytes = Encoding.UTF8.GetBytes(json);
                        await udp.SendAsync(bytes, bytes.Length, broadcastEp);
                        await udp.SendAsync(bytes, bytes.Length, loopbackEp);
                    }
                    catch { }

                    try { await Task.Delay(500, cts.Token); } catch { break; }
                }
            }, cts.Token);

            return cts;
        }

        // O Cliente escuta na rede e localiza o IP do servidor em timeoutMs
        public static async Task<DiscoveryResult?> DiscoverServerAsync(int timeoutMs = 4000)
        {
            using var udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

            var cts = new CancellationTokenSource(timeoutMs);

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var receiveTask = udp.ReceiveAsync();
                    var timeoutTask = Task.Delay(timeoutMs, cts.Token);
                    var completed = await Task.WhenAny(receiveTask, timeoutTask);

                    if (completed == receiveTask)
                    {
                        var res = await receiveTask;
                        string json = Encoding.UTF8.GetString(res.Buffer);
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("tipo", out var tipoProp) && tipoProp.GetString() == "yvira_beacon")
                        {
                            int port = root.GetProperty("porta").GetInt32();
                            string name = root.TryGetProperty("nome", out var n) ? n.GetString() ?? "Servidor" : "Servidor";
                            string secret = root.TryGetProperty("segredo", out var s) ? s.GetString() ?? "" : "";
                            string ip = res.RemoteEndPoint.Address.ToString();

                            // Normaliza IP
                            if (ip == "::1" || ip == "127.0.0.1") ip = "127.0.0.1";

                            return new DiscoveryResult(ip, port, $"ws://{ip}:{port}", name, secret);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch { }

            return null;
        }
    }

    public record DiscoveryResult(string Ip, int Port, string Url, string Name, string Secret);
}
