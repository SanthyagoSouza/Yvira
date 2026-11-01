import os

path = r"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\Yvira.Vm\RealtimeModule.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

target = """            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
        }"""

replacement = """            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            try
            {
                // Permite conexões pelo IP da rede local (LAN)
                _listener.Prefixes.Add($"http://+:{Port}/");
            }
            catch { }
        }"""

if target in c:
    c = c.replace(target, replacement)
    with open(path, "w", encoding="utf-8") as f:
        f.write(c)
    print("PREFIXES_PATCHED_OK")
else:
    print("TARGET_NOT_FOUND")
