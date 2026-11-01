import os

path = r"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\Yvira.Vm\Interpreter.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

target = """                winObj.Set("enviar_dados", new HostFunction("enviar_dados", (interp, a) =>
                {
                    if (a.Count >= 2 && a[0] != null)
                    {
                        string fnName = a[0]!.ToString()!;
                        object? d = a[1];
                        win.SendData(fnName, d);
                    }
                    return null;
                }));"""

replacement = """                winObj.Set("enviar_dados", new HostFunction("enviar_dados", (interp, a) =>
                {
                    if (a.Count >= 2 && a[0] != null)
                    {
                        string fnName = a[0]!.ToString()!;
                        object? d = a[1];
                        win.SendData(fnName, d);
                    }
                    return null;
                }));

                // Alias .emitir(evento, dados) que despacha tanto para window[evento](dados) quanto CustomEvent
                winObj.Set("emitir", new HostFunction("emitir", (interp, a) =>
                {
                    if (a.Count >= 1 && a[0] != null)
                    {
                        string fnName = a[0]!.ToString()!;
                        object? d = a.Count > 1 ? a[1] : null;
                        win.SendData(fnName, d);
                    }
                    return null;
                }));"""

if target in c:
    c = c.replace(target, replacement)
    with open(path, "w", encoding="utf-8") as f:
        f.write(c)
    print("EMITIR_ALIAS_ADDED")
else:
    print("TARGET_NOT_FOUND")
