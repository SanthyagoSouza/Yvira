import os

path = r"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\Yvira.Vm\Interpreter.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Replace the EvaluateRealtime method with YviraMap.Get compatible version
start_marker = "private object? EvaluateRealtime(RealtimeExpressionNode node)"
idx = c.find(start_marker)
if idx != -1:
    before = c[:idx]
    new_method = """private object? EvaluateRealtime(RealtimeExpressionNode node)
    {
        var evaluatedArgs = new List<object?>();
        foreach (var arg in node.Arguments)
        {
            evaluatedArgs.Add(EvaluateExpression(arg));
        }

        string kind = node.Kind.ToLowerInvariant();
        if (kind == "servidor" || kind == "server")
        {
            int porta = 5000;
            string segredo = "";
            int rotacao = 30;

            if (evaluatedArgs.Count > 0)
            {
                if (evaluatedArgs[0] is YviraMap optMap)
                {
                    var p = optMap.Get("porta") ?? optMap.Get("port");
                    if (p != null) porta = Convert.ToInt32(p);

                    var s = optMap.Get("segredo") ?? optMap.Get("secret");
                    if (s != null) segredo = s.ToString() ?? "";

                    var r = optMap.Get("rotacao") ?? optMap.Get("rotacaoSegundos");
                    if (r != null) rotacao = Convert.ToInt32(r);
                }
                else
                {
                    if (evaluatedArgs[0] != null) porta = Convert.ToInt32(evaluatedArgs[0]);
                    if (evaluatedArgs.Count > 1) segredo = evaluatedArgs[1]?.ToString() ?? "";
                    if (evaluatedArgs.Count > 2 && evaluatedArgs[2] != null) rotacao = Convert.ToInt32(evaluatedArgs[2]);
                }
            }

            var server = new RealtimeServerInstance(porta, segredo, rotacao, this);
            var serverMap = new YviraMap();

            serverMap.Set("iniciar", new HostFunction("iniciar", (interp, a) =>
            {
                server.Start();
                return null;
            }));

            serverMap.Set("ao", new HostFunction("ao", (interp, a) =>
            {
                if (a.Count >= 2 && a[0] != null && a[1] != null)
                {
                    server.On(a[0]!.ToString()!, a[1]!);
                }
                return null;
            }));

            serverMap.Set("emitir", new HostFunction("emitir", (interp, a) =>
            {
                if (a.Count >= 1 && a[0] != null)
                {
                    string ev = a[0]!.ToString()!;
                    object? pl = a.Count > 1 ? a[1] : null;
                    server.EmitAll(ev, pl);
                }
                return null;
            }));

            serverMap.Set("para", new HostFunction("para", (interp, a) =>
            {
                string room = a.Count > 0 && a[0] != null ? a[0]!.ToString()! : "";
                var roomScope = new YviraMap();
                roomScope.Set("emitir", new HostFunction("para.emitir", (i, rArgs) =>
                {
                    if (rArgs.Count >= 1 && rArgs[0] != null)
                    {
                        string ev = rArgs[0]!.ToString()!;
                        object? pl = rArgs.Count > 1 ? rArgs[1] : null;
                        server.BroadcastToRoom(room, ev, pl);
                    }
                    return null;
                }));
                return roomScope;
            }));

            serverMap.Set("parar", new HostFunction("parar", (interp, a) =>
            {
                server.Dispose();
                return null;
            }));

            return serverMap;
        }
        else // "cliente" ou "client"
        {
            string url = "ws://localhost:5000";
            string segredo = "";
            int rotacao = 30;

            if (evaluatedArgs.Count > 0)
            {
                if (evaluatedArgs[0] is YviraMap optMap)
                {
                    var u = optMap.Get("endereco") ?? optMap.Get("url");
                    if (u != null) url = u.ToString() ?? url;

                    var s = optMap.Get("segredo") ?? optMap.Get("secret");
                    if (s != null) segredo = s.ToString() ?? "";

                    var r = optMap.Get("rotacao") ?? optMap.Get("rotacaoSegundos");
                    if (r != null) rotacao = Convert.ToInt32(r);
                }
                else
                {
                    url = evaluatedArgs[0]?.ToString() ?? url;
                    if (evaluatedArgs.Count > 1) segredo = evaluatedArgs[1]?.ToString() ?? "";
                    if (evaluatedArgs.Count > 2 && evaluatedArgs[2] != null) rotacao = Convert.ToInt32(evaluatedArgs[2]);
                }
            }

            var client = new RealtimeClientInstance(url, segredo, rotacao, this);
            var clientMap = new YviraMap();

            clientMap.Set("conectar", new HostFunction("conectar", (interp, a) =>
            {
                client.Connect();
                return null;
            }));

            clientMap.Set("ao", new HostFunction("ao", (interp, a) =>
            {
                if (a.Count >= 2 && a[0] != null && a[1] != null)
                {
                    client.On(a[0]!.ToString()!, a[1]!);
                }
                return null;
            }));

            clientMap.Set("emitir", new HostFunction("emitir", (interp, a) =>
            {
                if (a.Count >= 1 && a[0] != null)
                {
                    string ev = a[0]!.ToString()!;
                    object? pl = a.Count > 1 ? a[1] : null;
                    _ = client.EmitAsync(ev, pl);
                }
                return null;
            }));

            clientMap.Set("desconectar", new HostFunction("desconectar", (interp, a) =>
            {
                client.Dispose();
                return null;
            }));

            return clientMap;
        }
    }
}
"""
    c = before + new_method
    with open(path, "w", encoding="utf-8") as f:
        f.write(c)
    print("INTERPRETER_FIXED_OK")
else:
    print("START_MARKER_NOT_FOUND")
