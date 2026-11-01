using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Yvira.Vm;

public class HttpServerInstance : IDisposable
{
    private readonly HttpListener _listener;
    private readonly object _handler;
    private readonly Interpreter _interpreter;
    private readonly ConcurrentBag<Interpreter> _workerPool = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _listenerTask;
    private bool _disposed = false;

    public int Port { get; }
    public bool IsRunning => _listener.IsListening && !_disposed;

    public HttpServerInstance(int port, object handler, Interpreter interpreter)
    {
        Port = port;
        _handler = handler;
        _interpreter = interpreter;

        _listener = new HttpListener();
        // Escuta em localhost e 127.0.0.1
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
    }

    public void Start()
    {
        _listener.Start();
        _listenerTask = Task.Run(ListenLoop);
    }

    private async Task ListenLoop()
    {
        while (!_cts.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => ProcessRequest(context));
            }
            catch (HttpListenerException) when (_cts.IsCancellationRequested || !_listener.IsListening)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!_cts.IsCancellationRequested)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[HTTP Server Error]: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        var req = context.Request;
        var res = context.Response;

        // Suporte default a CORS permissivo para facilitar desenvolvimento de APIs / Frontends
        res.Headers["Access-Control-Allow-Origin"] = "*";
        res.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, PATCH, OPTIONS";
        res.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, Accept";

        if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            res.StatusCode = 204;
            res.Close();
            return;
        }

        try
        {
            // 1. Extrai Body com proteção de tamanho máximo (Anti-DoS Payload - Max 10MB)
            string bodyString = "";
            if (req.HasEntityBody)
            {
                if (req.ContentLength64 > 10 * 1024 * 1024)
                {
                    res.StatusCode = 413; // Payload Too Large
                    res.Close();
                    return;
                }
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
                bodyString = reader.ReadToEnd();
            }

            object? parsedBody = bodyString;
            if (!string.IsNullOrWhiteSpace(bodyString))
            {
                string contentType = req.ContentType ?? "";
                if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                    (bodyString.TrimStart().StartsWith("{") || bodyString.TrimStart().StartsWith("[")))
                {
                    try
                    {
                        parsedBody = FileParsers.ParseJson(bodyString);
                    }
                    catch
                    {
                        parsedBody = bodyString;
                    }
                }
            }

            // 2. Extrai Query params
            var queryMap = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (req.QueryString.Count > 0)
            {
                foreach (string? key in req.QueryString.AllKeys)
                {
                    if (key != null)
                    {
                        queryMap[key] = req.QueryString[key];
                    }
                }
            }

            // 3. Extrai Headers
            var headersMap = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (string? key in req.Headers.AllKeys)
            {
                if (key != null)
                {
                    headersMap[key.ToLowerInvariant()] = req.Headers[key];
                }
            }

            // 4. Constr�i o objeto YviraMap req
            var reqMapData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["method"] = req.HttpMethod.ToUpperInvariant(),
                ["path"] = req.Url?.AbsolutePath ?? "/",
                ["url"] = req.Url?.ToString() ?? "",
                ["body"] = parsedBody,
                ["query"] = new YviraMap(queryMap),
                ["headers"] = new YviraMap(headersMap)
            };
            var yvReq = new YviraMap(reqMapData);

            // 5. Invoca a fun��o do usu�rio Yvira
            object? result = null;
            if (!_workerPool.TryTake(out var worker))
            {
                worker = _interpreter.CreateWorkerClone();
            }

            try
            {
                if (_handler is YviraFunction userFn)
                {
                    result = userFn.Call(worker, new List<object?> { yvReq });
                }
                else if (_handler is HostFunction hostFn)
                {
                    result = hostFn.Call(worker, new List<object?> { yvReq });
                }
            }
            finally
            {
                _workerPool.Add(worker);
            }

            // 6. Formata e envia a resposta
            SendResponse(res, result);
        }
        catch (Exception ex)
        {
            try
            {
                res.StatusCode = 500;
                res.ContentType = "application/json; charset=utf-8";
                string errJson = $"{{\"erro\": \"Erro interno do servidor Yvira\", \"detalhes\": \"{ex.Message.Replace("\"", "\\\"")}\"}}";
                byte[] buffer = Encoding.UTF8.GetBytes(errJson);
                res.ContentLength64 = buffer.Length;
                res.OutputStream.Write(buffer, 0, buffer.Length);
                res.Close();
            }
            catch { }
        }
    }

    private void SendResponse(HttpListenerResponse res, object? result)
    {
        int statusCode = 200;
        string contentType = "application/json; charset=utf-8";
        byte[] responseBytes = Array.Empty<byte>();

        // Cabeçalhos de segurança defensiva automáticos (OWASP / Hardening)
        res.Headers["X-Content-Type-Options"] = "nosniff";
        res.Headers["X-Frame-Options"] = "SAMEORIGIN";
        res.Headers["X-XSS-Protection"] = "1; mode=block";
        res.Headers["Server"] = "Yvira-Server/2.2";

        if (result is HttpResponseHelper respHelper)
        {
            statusCode = respHelper.StatusCode;
            contentType = respHelper.ContentType;
            responseBytes = respHelper.BodyBytes;
        }
        else if (result is YviraMap resMap && (resMap.HasKey("status") || resMap.HasKey("body")))
        {
            // Objeto de resposta customizado retornado pelo usu�rio: { status: 201, body: ..., headers: ... }
            if (resMap.HasKey("status") && resMap.Get("status") is double sc)
            {
                statusCode = (int)sc;
            }

            if (resMap.HasKey("headers") && resMap.Get("headers") is YviraMap customHeaders)
            {
                foreach (var kv in customHeaders)
                {
                    res.Headers[kv.Key] = kv.Value?.ToString();
                }
            }

            object? customBody = resMap.HasKey("body") ? resMap.Get("body") : null;
            if (customBody is string strBody)
            {
                contentType = "text/plain; charset=utf-8";
                responseBytes = Encoding.UTF8.GetBytes(strBody);
            }
            else if (customBody is YviraMap or YviraList)
            {
                contentType = "application/json; charset=utf-8";
                string json = SerializeToJson(customBody);
                responseBytes = Encoding.UTF8.GetBytes(json);
            }
            else if (customBody != null)
            {
                contentType = "text/plain; charset=utf-8";
                responseBytes = Encoding.UTF8.GetBytes(customBody.ToString() ?? "");
            }
        }
        else if (result is string s)
        {
            if (s.TrimStart().StartsWith("<") && s.TrimEnd().EndsWith(">"))
            {
                contentType = "text/html; charset=utf-8";
            }
            else
            {
                contentType = "text/plain; charset=utf-8";
            }
            responseBytes = Encoding.UTF8.GetBytes(s);
        }
        else if (result is YviraMap or YviraList)
        {
            contentType = "application/json; charset=utf-8";
            string json = SerializeToJson(result);
            responseBytes = Encoding.UTF8.GetBytes(json);
        }
        else if (result != null)
        {
            contentType = "text/plain; charset=utf-8";
            responseBytes = Encoding.UTF8.GetBytes(result.ToString() ?? "");
        }

        res.StatusCode = statusCode;
        res.ContentType = contentType;
        res.ContentLength64 = responseBytes.Length;
        if (responseBytes.Length > 0)
        {
            res.OutputStream.Write(responseBytes, 0, responseBytes.Length);
        }
        res.Close();
    }

    private static string SerializeToJson(object? obj)
    {
        if (obj is YviraMap map)
        {
            return System.Text.Json.JsonSerializer.Serialize(ConvertYviraMap(map));
        }
        if (obj is YviraList list)
        {
            return System.Text.Json.JsonSerializer.Serialize(ConvertYviraList(list));
        }
        return System.Text.Json.JsonSerializer.Serialize(obj);
    }

    private static Dictionary<string, object?> ConvertYviraMap(YviraMap map)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var kv in map)
        {
            dict[kv.Key] = ConvertYviraValue(kv.Value);
        }
        return dict;
    }

    private static List<object?> ConvertYviraList(YviraList list)
    {
        var result = new List<object?>();
        foreach (var item in list)
        {
            result.Add(ConvertYviraValue(item));
        }
        return result;
    }

    private static object? ConvertYviraValue(object? val)
    {
        if (val is YviraMap m) return ConvertYviraMap(m);
        if (val is YviraList l) return ConvertYviraList(l);
                if (val is YviraMatrix mat)
        {
            var rowsList = new List<object?>();
            for (int r = 0; r < mat.Rows; r++)
            {
                var row = new List<object?>();
                for (int c = 0; c < mat.Cols; c++)
                {
                    row.Add(mat.Get(r, c));
                }
                rowsList.Add(row);
            }
            return rowsList;
        }
        return val;
    }

    public void Stop()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        try
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
            _listener.Close();
        }
        catch { }
    }
}

public class HttpResponseHelper
{
    public int StatusCode { get; }
    public string ContentType { get; }
    public byte[] BodyBytes { get; }

    public HttpResponseHelper(int statusCode, string contentType, byte[] bodyBytes)
    {
        StatusCode = statusCode;
        ContentType = contentType;
        BodyBytes = bodyBytes;
    }
}
