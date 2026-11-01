using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Yvira.Vm;

public static class HttpModule
{
    private static readonly HttpClient _httpClient = new HttpClient();

    static HttpModule()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Yvira-Runtime/2.1");
    }

    public static YviraMap CreateModule(Interpreter? interpreter = null)
    {
        var methods = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // http.get(url) -> YviraMap { status, ok, body, json() }
        methods["get"] = new HostFunction("http.get", (interp, args) =>
        {
            if (args.Count == 0 || args[0] == null)
                throw new Exception("http.get requer a URL como primeiro argumento.");
            string url = args[0]!.ToString()!;

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            HttpResponseMessage response;
            try {
                response = _httpClient.Send(req);
            } catch (Exception ex) {
                Console.WriteLine("DEBUG HTTP SEND ERROR: " + ex);
                throw;
            }
            using var reader = new System.IO.StreamReader(response.Content.ReadAsStream(), Encoding.UTF8);
            string body = reader.ReadToEnd();

            return BuildResponseMap(response, body);
        });

        // http.post(url, bodyOrMap) -> YviraMap { status, ok, body, json() }
        methods["post"] = new HostFunction("http.post", (interp, args) =>
        {
            if (args.Count < 2)
                throw new Exception("http.post requer a URL e o corpo da requisição.");
            string url = args[0]?.ToString() ?? "";
            object? payload = args[1];

            HttpContent content;
            if (payload is YviraMap map)
            {
                string jsonString = System.Text.Json.JsonSerializer.Serialize(map.ToDictionary());
                content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            }
            else
            {
                content = new StringContent(payload?.ToString() ?? "", Encoding.UTF8, "text/plain");
            }

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            HttpResponseMessage response;
            try {
                response = _httpClient.Send(req);
            } catch (Exception ex) {
                Console.WriteLine("DEBUG HTTP SEND ERROR: " + ex);
                throw;
            }
            using var reader = new System.IO.StreamReader(response.Content.ReadAsStream(), Encoding.UTF8);
            string body = reader.ReadToEnd();

            return BuildResponseMap(response, body);
        });

        // http.batchPost(url, payloadGeneratorOrList, totalCount, concurrency?, onProgress?) -> YviraMap com métricas
        methods["batchPost"] = new HostFunction("http.batchPost", (interp, args) =>
        {
            if (args.Count < 3)
                throw new Exception("http.batchPost requer (url, payloadListOuGerador, totalCount, [concurrency], [onProgress]).");

            string url = args[0]?.ToString() ?? "";
            object? payloadSource = args[1];
            int total = Convert.ToInt32(args[2]);
            int concurrency = args.Count > 3 && args[3] != null ? Convert.ToInt32(args[3]) : System.Environment.ProcessorCount * 2;
            if (concurrency < 1) concurrency = 1;

            object? onProgress = args.Count > 4 ? args[4] : null;

            int completed = 0;
            int successes = 0;
            int failures = 0;
            long totalDurationTicks = 0;

            var startTime = System.Diagnostics.Stopwatch.StartNew();

            using var semaphore = new System.Threading.SemaphoreSlim(concurrency, concurrency);
            var tasks = new List<System.Threading.Tasks.Task>(total);

            // Prepara payloads antecipadamente ou com gerador
            for (int i = 0; i < total; i++)
            {
                int index = i;
                semaphore.Wait();

                tasks.Add(System.Threading.Tasks.Task.Run(async () =>
                {
                    var reqSw = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        object? itemPayload = null;
                        if (payloadSource is YviraList yList)
                        {
                            itemPayload = index < yList.Items.Count ? yList.Items[index] : null;
                        }
                        else if (payloadSource is YviraFunction fnGen && interp != null)
                        {
                            lock (interp)
                            {
                                itemPayload = fnGen.Call(interp, new List<object?> { (double)index });
                            }
                        }
                        else
                        {
                            itemPayload = payloadSource;
                        }

                        HttpContent content;
                        if (itemPayload is YviraMap map)
                        {
                            string jsonString = System.Text.Json.JsonSerializer.Serialize(map.ToDictionary());
                            content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                        }
                        else
                        {
                            content = new StringContent(itemPayload?.ToString() ?? "", Encoding.UTF8, "text/plain");
                        }

                        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
                        using var res = await _httpClient.SendAsync(req).ConfigureAwait(false);
                        reqSw.Stop();

                        System.Threading.Interlocked.Add(ref totalDurationTicks, reqSw.ElapsedTicks);

                        if (res.IsSuccessStatusCode)
                        {
                            System.Threading.Interlocked.Increment(ref successes);
                        }
                        else
                        {
                            System.Threading.Interlocked.Increment(ref failures);
                        }
                    }
                    catch
                    {
                        System.Threading.Interlocked.Increment(ref failures);
                    }
                    finally
                    {
                        int done = System.Threading.Interlocked.Increment(ref completed);
                        semaphore.Release();

                        if (onProgress != null && interp != null)
                        {
                            int step = total >= 1000 ? (total / 10) : (total >= 100 ? (total / 5) : 10);
                            if (step < 1) step = 1;
                            if (done % step == 0 || done == total)
                            {
                                if (onProgress is YviraFunction pFn)
                                {
                                    lock (interp)
                                    {
                                        pFn.Call(interp, new List<object?> { (double)done, (double)total });
                                    }
                                }
                            }
                        }
                    }
                }));
            }

            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
            startTime.Stop();

            double totalSeconds = startTime.Elapsed.TotalSeconds;
            if (totalSeconds <= 0) totalSeconds = 0.001;
            double tps = total / totalSeconds;
            double avgMs = total > 0 ? (totalDurationTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency) / total : 0;

            var metrics = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["total"] = (double)total,
                ["successes"] = (double)successes,
                ["failures"] = (double)failures,
                ["elapsed_ms"] = (double)startTime.ElapsedMilliseconds,
                ["avg_ms"] = avgMs,
                ["tps"] = tps
            };

            return new YviraMap(metrics);
        });

        // http.response(status, body, headers?) -> Helper para customizar resposta
        methods["response"] = new HostFunction("http.response", (interp, args) =>
        {
            if (args.Count == 0)
                throw new Exception("http.response requer pelo menos o código de status.");

            int statusCode = Convert.ToInt32(args[0]);
            object? body = args.Count > 1 ? args[1] : null;

            string contentType = "application/json; charset=utf-8";
            byte[] bodyBytes = Array.Empty<byte>();

            if (body is string s)
            {
                if (s.TrimStart().StartsWith("<") && s.TrimEnd().EndsWith(">"))
                    contentType = "text/html; charset=utf-8";
                else
                    contentType = "text/plain; charset=utf-8";
                bodyBytes = Encoding.UTF8.GetBytes(s);
            }
            else if (body is YviraMap or YviraList)
            {
                contentType = "application/json; charset=utf-8";
                string json = System.Text.Json.JsonSerializer.Serialize(body is YviraMap m ? m.ToDictionary() : body);
                bodyBytes = Encoding.UTF8.GetBytes(json);
            }
            else if (body != null)
            {
                contentType = "text/plain; charset=utf-8";
                bodyBytes = Encoding.UTF8.GetBytes(body.ToString() ?? "");
            }

            return new HttpResponseHelper(statusCode, contentType, bodyBytes);
        });

        // http.serve(port, handler, options?) -> YviraMap { stop, port, isRunning }
        methods["serve"] = new HostFunction("http.serve", (interp, args) =>
        {
            var activeInterp = interp ?? interpreter;
            if (activeInterp == null)
                throw new Exception("Interpretador não disponível para iniciar servidor HTTP.");

            if (args.Count < 2)
                throw new Exception("http.serve requer a porta (número) e o handler (função). Exemplo: http.serve(8080, fn(req) { ... });");

            int port = Convert.ToInt32(args[0]);
            object? handler = args[1];

            if (handler is not YviraFunction && handler is not HostFunction)
                throw new Exception("O segundo argumento de http.serve deve ser uma função de callback (fn(req) { ... }).");

            bool isBackground = false;
            if (args.Count > 2 && args[2] is YviraMap options)
            {
                var bgVal = options.Get("background");
                if (bgVal is bool b) isBackground = b;
                else if (bgVal is string strBg && bool.TryParse(strBg, out bool parsedB)) isBackground = parsedB;
            }

            var server = new HttpServerInstance(port, handler, activeInterp);
            server.Start();
            activeInterp.RegisterHttpServer(server);

            var serverMap = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            serverMap["port"] = (double)port;
            serverMap["stop"] = new HostFunction("server.stop", (_, _) =>
            {
                server.Stop();
                return null;
            });

            // Se NÃO for em background, bloqueia a execução da thread mantendo o servidor ouvindo
            if (!isBackground)
            {
                Console.WriteLine($"[Yvira HTTP Server] Rodando em http://localhost:{port}/ (Pressione Ctrl+C para encerrar)");
                using var waitHandle = new ManualResetEventSlim(false);

                ConsoleCancelEventHandler cancelHandler = (s, e) =>
                {
                    e.Cancel = true;
                    server.Stop();
                    waitHandle.Set();
                };

                Console.CancelKeyPress += cancelHandler;
                try
                {
                    while (server.IsRunning)
                    {
                        if (waitHandle.Wait(250))
                            break;
                    }
                }
                finally
                {
                    Console.CancelKeyPress -= cancelHandler;
                    server.Stop();
                }
            }
            else
            {
                Console.WriteLine($"[Yvira HTTP Server (Background)] Rodando em http://localhost:{port}/");
            }

            return new YviraMap(serverMap);
        });

        return new YviraMap(methods);
    }

    private static YviraMap BuildResponseMap(HttpResponseMessage response, string body)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        map["status"] = (double)response.StatusCode;
        map["ok"] = response.IsSuccessStatusCode;
        map["body"] = body;

        map["json"] = new HostFunction("response.json", (interp, args) =>
        {
            return FileParsers.ParseJson(body);
        });

        return new YviraMap(map);
    }
}
