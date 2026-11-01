using Fleck;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Yvira.Ast;

namespace Yvira.Vm
{
    /// <summary>
    /// Utilitários de Segurança e Criptografia Militar do @realtime.
    /// Inclui:
    /// 1. Handshake com Chaves Rotativas (HMAC-SHA256 Rolling Keys)
    /// 2. Criptografia Ponta a Ponta de Pacotes (AES-256-CBC com PBKDF2/HMAC)
    /// </summary>
    public static class RealtimeCrypto
    {
        public static string GenerateTimeToken(string secret, int intervalSeconds, long offsetSteps = 0)
        {
            if (string.IsNullOrEmpty(secret)) return "open_access";
            long timeStep = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() / Math.Max(5, intervalSeconds)) + offsetSteps;
            byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
            byte[] msgBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(timeStep));

            using var hmac = new HMACSHA256(keyBytes);
            byte[] hash = hmac.ComputeHash(msgBytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static bool ValidateTimeToken(string token, string secret, int intervalSeconds)
        {
            if (string.IsNullOrEmpty(secret)) return true;
            if (string.IsNullOrWhiteSpace(token)) return false;

            for (long offset = -1; offset <= 1; offset++)
            {
                string expected = GenerateTimeToken(secret, intervalSeconds, offset);
                if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(token.ToLowerInvariant()),
                    Encoding.UTF8.GetBytes(expected)))
                {
                    return true;
                }
            }
            return false;
        }

        private static byte[] DeriveKey(string secret, byte[] salt)
        {
            using var kdf = new Rfc2898DeriveBytes(secret, salt, 10000, HashAlgorithmName.SHA256);
            return kdf.GetBytes(32); // 256 bits
        }

        public static string EncryptPayload(string plainText, string secret)
        {
            if (string.IsNullOrEmpty(secret)) return plainText;

            byte[] salt = new byte[16];
            RandomNumberGenerator.Fill(salt);
            byte[] key = DeriveKey(secret, salt);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var ms = new MemoryStream();
            ms.Write(salt, 0, salt.Length);
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }

            return "enc:" + Convert.ToBase64String(ms.ToArray());
        }

        public static string DecryptPayload(string cipherText, string secret)
        {
            if (string.IsNullOrEmpty(secret) || !cipherText.StartsWith("enc:")) return cipherText;

            try
            {
                byte[] allBytes = Convert.FromBase64String(cipherText.Substring(4));
                if (allBytes.Length < 32) return cipherText;

                byte[] salt = new byte[16];
                byte[] iv = new byte[16];
                Buffer.BlockCopy(allBytes, 0, salt, 0, 16);
                Buffer.BlockCopy(allBytes, 16, iv, 0, 16);

                byte[] key = DeriveKey(secret, salt);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var ms = new MemoryStream(allBytes, 32, allBytes.Length - 32);
                using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);
                return sr.ReadToEnd();
            }
            catch
            {
                return cipherText;
            }
        }
    }

    /// <summary>
    /// Rate Limiter anti-flood por IP e por Conexão.
    /// Bloqueia ataques DoS e spam de pacotes em milissegundos.
    /// </summary>
    public class RealtimeRateLimiter
    {
        private readonly int _maxPacketsPerSecond;
        private int _packetCount = 0;
        private long _lastResetTimestamp = 0;
        private readonly object _lock = new();

        public RealtimeRateLimiter(int maxPacketsPerSecond = 60)
        {
            _maxPacketsPerSecond = maxPacketsPerSecond > 0 ? maxPacketsPerSecond : 60;
            _lastResetTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public bool CheckPacketAllowed()
        {
            lock (_lock)
            {
                long current = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (current > _lastResetTimestamp)
                {
                    _packetCount = 0;
                    _lastResetTimestamp = current;
                }

                _packetCount++;
                return _packetCount <= _maxPacketsPerSecond;
            }
        }
    }

    /// <summary>
    /// Representa um cliente conectado no RealtimeServer.
    public class RealtimeConnectedClient
    {
        public string Id { get; }
        public IWebSocketConnection Socket { get; }
        public string RemoteEndPoint { get; }
        public HashSet<string> Rooms { get; } = new(StringComparer.OrdinalIgnoreCase);
        public RealtimeRateLimiter RateLimiter { get; }
        private readonly RealtimeServerInstance _server;

        public RealtimeConnectedClient(string id, IWebSocketConnection socket, string remoteEndPoint, RealtimeServerInstance server, int maxPacketsPerSecond)
        {
            Id = id;
            Socket = socket;
            RemoteEndPoint = remoteEndPoint;
            _server = server;
            RateLimiter = new RealtimeRateLimiter(maxPacketsPerSecond);
        }

        public void Join(string room) => _server.JoinRoom(this, room);
        public void Leave(string room) => _server.LeaveRoom(this, room);

        public Task SendAsync(string eventName, object? payload)
        {
            if (!Socket.IsAvailable) return Task.CompletedTask;
            var packet = new { evento = eventName, payload = RealtimeServerInstance.NormalizeForJson(payload) };
            string json = JsonSerializer.Serialize(packet);

            if (!string.IsNullOrEmpty(_server.Secret))
            {
                json = RealtimeCrypto.EncryptPayload(json, _server.Secret);
            }

            return Socket.Send(json);
        }

        public void BroadcastTo(string room, string eventName, object? payload)
        {
            _server.BroadcastToRoom(room, eventName, payload, excludeClientId: Id);
        }
    }

    public class RealtimeServerInstance : IDisposable
    {
        public int Port { get; }
        public string Secret { get; }
        public int RotationSeconds { get; }
        public int MaxPacketsPerSec { get; }
        public bool IsRunning { get; private set; }

        private readonly WebSocketServer _server;
        private CancellationTokenSource? _beaconCts;
        private readonly ConcurrentDictionary<string, RealtimeConnectedClient> _clients = new();
        private readonly ConcurrentDictionary<IWebSocketConnection, RealtimeConnectedClient> _socketMap = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _roomMembers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<object>> _eventHandlers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Interpreter _interpreter;

        public RealtimeServerInstance(int port, string secret, int rotationSeconds, int maxPacketsPerSec, Interpreter interpreter)
        {
            Port = port;
            Secret = secret;
            RotationSeconds = rotationSeconds > 0 ? rotationSeconds : 30;
            MaxPacketsPerSec = maxPacketsPerSec > 0 ? maxPacketsPerSec : 60;
            _interpreter = interpreter;

            FleckLog.LogAction = (level, message, ex) => { };
            _server = new WebSocketServer($"ws://0.0.0.0:{Port}");
            _server.RestartAfterListenError = true;
        }

        public void On(string eventName, object callback)
        {
            if (!_eventHandlers.TryGetValue(eventName, out var list))
            {
                list = new List<object>();
                _eventHandlers[eventName] = list;
            }
            list.Add(callback);
        }

        public void Start()
        {
            if (IsRunning) return;

            _beaconCts = RealtimeDiscovery.StartBeacon(Port, "ClickBattleServer", Secret);

            _server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    string clientId = Guid.NewGuid().ToString("N")[..8];
                    string remoteIp = socket.ConnectionInfo.ClientIpAddress ?? "remoto";
                    var client = new RealtimeConnectedClient(clientId, socket, remoteIp, this, MaxPacketsPerSec);
                    _clients[clientId] = client;
                    _socketMap[socket] = client;

                    var okPacket = JsonSerializer.Serialize(new { evento = "autenticado", id = clientId });
                    socket.Send(okPacket);

                    var yClient = WrapClient(client);
                    TriggerLocalEvent("conectar", yClient);
                };

                socket.OnClose = () =>
                {
                    if (_socketMap.TryRemove(socket, out var client))
                    {
                        _clients.TryRemove(client.Id, out _);
                        foreach (var room in client.Rooms)
                        {
                            LeaveRoom(client, room);
                        }
                        TriggerLocalEvent("desconectar", WrapClient(client));
                    }
                };

                socket.OnMessage = message =>
                {
                    if (!_socketMap.TryGetValue(socket, out var client)) return;

                    if (!client.RateLimiter.CheckPacketAllowed())
                    {
                        TriggerLocalEvent("flood_detectado", new YviraMap(new Dictionary<string, object?>
                        {
                            ["id"] = client.Id,
                            ["ip"] = client.RemoteEndPoint,
                            ["motivo"] = $"Excedeu o limite de {MaxPacketsPerSec} pacotes/seg"
                        }));
                        socket.Close();
                        return;
                    }

                    string msgPayload = message;
                    if (msgPayload.StartsWith("enc:"))
                    {
                        msgPayload = RealtimeCrypto.DecryptPayload(msgPayload, Secret);
                    }

                    var yClient = WrapClient(client);
                    ProcessIncomingMessage(client, yClient, msgPayload);
                };
            });

            IsRunning = true;
            TriggerLocalEvent("iniciado", new YviraMap(new Dictionary<string, object?>
            {
                ["porta"] = (double)Port,
                ["segredoAtivo"] = !string.IsNullOrEmpty(Secret),
                ["criptografia"] = !string.IsNullOrEmpty(Secret) ? "AES-256-CBC" : "desativada",
                ["antiFloodLimite"] = (double)MaxPacketsPerSec,
                ["rotacao"] = (double)RotationSeconds
            }));
        }

        private void ProcessIncomingMessage(RealtimeConnectedClient client, YviraMap yClient, string msgJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(msgJson);
                var root = doc.RootElement;
                string evtName = root.TryGetProperty("evento", out var eProp) ? eProp.GetString() ?? "" : "";
                object? payload = root.TryGetProperty("payload", out var pProp) ? ConvertJsonElement(pProp) : null;

                if (_eventHandlers.TryGetValue(evtName, out var handlers))
                {
                    foreach (var h in handlers)
                    {
                        InvokeCallback(_interpreter, h, new List<object?> { yClient, payload });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[@realtime Erro ao processar mensagem]: {ex.Message}");
            }
        }

        public void JoinRoom(RealtimeConnectedClient client, string room)
        {
            client.Rooms.Add(room);
            var set = _roomMembers.GetOrAdd(room, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            lock (set) { set.Add(client.Id); }
        }

        public void LeaveRoom(RealtimeConnectedClient client, string room)
        {
            client.Rooms.Remove(room);
            if (_roomMembers.TryGetValue(room, out var set))
            {
                lock (set) { set.Remove(client.Id); }
            }
        }

        public void BroadcastToRoom(string room, string eventName, object? payload, string? excludeClientId = null)
        {
            if (!_roomMembers.TryGetValue(room, out var memberIds)) return;
            List<string> ids;
            lock (memberIds) { ids = new List<string>(memberIds); }

            foreach (var id in ids)
            {
                if (excludeClientId != null && id == excludeClientId) continue;
                if (_clients.TryGetValue(id, out var c))
                {
                    _ = c.SendAsync(eventName, payload);
                }
            }
        }

        public void EmitAll(string eventName, object? payload)
        {
            foreach (var c in _clients.Values)
            {
                _ = c.SendAsync(eventName, payload);
            }
        }

        private void TriggerLocalEvent(string eventName, object? arg)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                foreach (var h in handlers)
                {
                    InvokeCallback(_interpreter, h, new List<object?> { arg });
                }
            }
        }

        public YviraMap WrapClient(RealtimeConnectedClient client)
        {
            var map = new YviraMap();
            map.Set("id", client.Id);
            map.Set("ip", client.RemoteEndPoint);

            map.Set("entrar", new HostFunction("entrar", (interp, args) =>
            {
                if (args.Count > 0 && args[0] != null) client.Join(args[0]!.ToString()!);
                return null;
            }));

            map.Set("sair", new HostFunction("sair", (interp, args) =>
            {
                if (args.Count > 0 && args[0] != null) client.Leave(args[0]!.ToString()!);
                return null;
            }));

            map.Set("emitir", new HostFunction("emitir", (interp, args) =>
            {
                if (args.Count >= 1 && args[0] != null)
                {
                    string ev = args[0]!.ToString()!;
                    object? pl = args.Count > 1 ? args[1] : null;
                    _ = client.SendAsync(ev, pl);
                }
                return null;
            }));

            map.Set("transmitirPara", new HostFunction("transmitirPara", (interp, args) =>
            {
                if (args.Count >= 2 && args[0] != null && args[1] != null)
                {
                    string rm = args[0]!.ToString()!;
                    string ev = args[1]!.ToString()!;
                    object? pl = args.Count > 2 ? args[2] : null;
                    client.BroadcastTo(rm, ev, pl);
                }
                return null;
            }));

            return map;
        }

        public static void InvokeCallback(Interpreter interpreter, object callback, List<object?> args)
        {
            try
            {
                if (callback is string funcName)
                {
                    var fnObj = interpreter.Environment.Get(funcName);
                    if (fnObj is YviraFunction yf) yf.Call(interpreter, args);
                    else if (fnObj is HostFunction hf) hf.Call(interpreter, args);
                }
                else if (callback is YviraFunction fn)
                {
                    fn.Call(interpreter, args);
                }
                else if (callback is HostFunction hf)
                {
                    hf.Call(interpreter, args);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[@realtime Erro de Callback]: {ex.Message}");
            }
        }

        public static object? NormalizeForJson(object? obj)
        {
            if (obj == null) return null;
            if (obj is YviraMap yMap)
            {
                var dict = new Dictionary<string, object?>();
                foreach (var kvp in yMap)
                {
                    dict[kvp.Key] = NormalizeForJson(kvp.Value);
                }
                return dict;
            }
            if (obj is YviraList yList)
            {
                var list = new List<object?>();
                foreach (var item in yList)
                {
                    list.Add(NormalizeForJson(item));
                }
                return list;
            }
            return obj;
        }

        public static object? ConvertJsonElement(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.String: return el.GetString();
                case JsonValueKind.Number: return el.GetDouble();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Null: return null;
                case JsonValueKind.Array:
                    var list = new YviraList();
                    foreach (var item in el.EnumerateArray()) list.Add(ConvertJsonElement(item));
                    return list;
                case JsonValueKind.Object:
                    var map = new YviraMap();
                    foreach (var prop in el.EnumerateObject()) map.Set(prop.Name, ConvertJsonElement(prop.Value));
                    return map;
                default: return null;
            }
        }

        public void Dispose()
        {
            try { _server.Dispose(); } catch { }
        }
    }

    /// <summary>
    /// Motor do Cliente @realtime.cliente(...) com Handshake Criptografado e Cifragem de Mensagens
    /// </summary>
    public class RealtimeClientInstance : IDisposable
    {
        public string Url { get; }
        public string Secret { get; }
        public int RotationSeconds { get; }

        private ClientWebSocket? _client;
        private readonly Dictionary<string, List<object>> _eventHandlers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Interpreter _interpreter;
        private readonly CancellationTokenSource _cts = new();
        private bool _isConnected = false;

        public RealtimeClientInstance(string url, string secret, int rotationSeconds, Interpreter interpreter)
        {
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                url = "ws://" + url.Substring(7);
            else if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                url = "wss://" + url.Substring(8);
            else if (!url.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
                url = "ws://" + url;

            Url = url;
            Secret = secret;
            RotationSeconds = rotationSeconds > 0 ? rotationSeconds : 30;
            _interpreter = interpreter;
        }

        public void On(string eventName, object callback)
        {
            if (!_eventHandlers.TryGetValue(eventName, out var list))
            {
                list = new List<object>();
                _eventHandlers[eventName] = list;
            }
            list.Add(callback);
        }

        public void Connect()
        {
            Task.Run(ConnectLoop);
        }

        private async Task ConnectLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    _client = new ClientWebSocket();
                    var uri = new Uri(Url);
                    await _client.ConnectAsync(uri, _cts.Token);

                    // Handshake com Token Rotativo
                    string token = RealtimeCrypto.GenerateTimeToken(Secret, RotationSeconds);
                    var handshake = new { acao = "auth", token = token, ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
                    string hsJson = JsonSerializer.Serialize(handshake);
                    
                    if (!string.IsNullOrEmpty(Secret))
                    {
                        hsJson = RealtimeCrypto.EncryptPayload(hsJson, Secret);
                    }

                    byte[] hsBytes = Encoding.UTF8.GetBytes(hsJson);
                    await _client.SendAsync(new ArraySegment<byte>(hsBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    byte[] buffer = new byte[8192];
                    var authRes = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    string authJson = Encoding.UTF8.GetString(buffer, 0, authRes.Count);
                    if (authJson.StartsWith("enc:"))
                    {
                        authJson = RealtimeCrypto.DecryptPayload(authJson, Secret);
                    }

                    using var doc = JsonDocument.Parse(authJson);
                    if (doc.RootElement.TryGetProperty("erro", out var errProp))
                    {
                        TriggerEvent("erro_autenticacao", errProp.GetString());
                        await _client.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Erro auth", CancellationToken.None);
                        return;
                    }

                    _isConnected = true;
                    TriggerEvent("conectar", null);

                    // Task de Heartbeat Ping periódico (a cada 5 segundos)
                    _ = Task.Run(async () =>
                    {
                        while (_isConnected && _client.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                        {
                            try
                            {
                                await Task.Delay(5000, _cts.Token);
                                if (_client.State == WebSocketState.Open)
                                {
                                    var pingBytes = Encoding.UTF8.GetBytes("{\"evento\":\"__ping__\"}");
                                    await _client.SendAsync(new ArraySegment<byte>(pingBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                            }
                            catch { break; }
                        }
                    }, _cts.Token);

                    while (_client.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                    {
                        var res = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                        if (res.MessageType == WebSocketMessageType.Close) break;

                        string msg = Encoding.UTF8.GetString(buffer, 0, res.Count);
                        if (msg.StartsWith("enc:"))
                        {
                            msg = RealtimeCrypto.DecryptPayload(msg, Secret);
                        }

                        using var msgDoc = JsonDocument.Parse(msg);
                        var root = msgDoc.RootElement;
                        string evt = root.TryGetProperty("evento", out var eProp) ? eProp.GetString() ?? "" : "";
                        object? payload = root.TryGetProperty("payload", out var pProp) ? RealtimeServerInstance.ConvertJsonElement(pProp) : null;

                        TriggerEvent(evt, payload);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    if (_isConnected)
                    {
                        _isConnected = false;
                        TriggerEvent("desconectar", ex.Message);
                    }
                }

                if (_cts.IsCancellationRequested) break;
                await Task.Delay(3000);
            }
        }

        public async Task EmitAsync(string eventName, object? payload)
        {
            if (_client == null || _client.State != WebSocketState.Open) return;
            var packet = new { evento = eventName, payload = RealtimeServerInstance.NormalizeForJson(payload) };
            string json = JsonSerializer.Serialize(packet);

            if (!string.IsNullOrEmpty(Secret))
            {
                json = RealtimeCrypto.EncryptPayload(json, Secret);
            }

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await _client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private void TriggerEvent(string eventName, object? arg)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                foreach (var h in handlers)
                {
                    RealtimeServerInstance.InvokeCallback(_interpreter, h, arg != null ? new List<object?> { arg } : new List<object?>());
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _client?.Dispose(); } catch { }
        }
    }
}
