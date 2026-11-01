import os

path = r"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\Yvira.Vm\RealtimeModule.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# 1. Trocar list.Items.Add(...) por list.Add(...)
c = c.replace("list.Items.Add(ConvertJsonElement(item));", "list.Add(ConvertJsonElement(item));")

# 2. Criar método auxiliar de invocação de callbacks
helper = """
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
"""

c = c.replace("_interpreter.InvokeCallback(h, new List<object?> { yClient, payload });", "InvokeCallback(_interpreter, h, new List<object?> { yClient, payload });")
c = c.replace("_interpreter.InvokeCallback(h, new List<object?> { arg });", "InvokeCallback(_interpreter, h, new List<object?> { arg });")
c = c.replace("_interpreter.InvokeCallback(h, arg != null ? new List<object?> { arg } : new List<object?>());", "RealtimeServerInstance.InvokeCallback(_interpreter, h, arg != null ? new List<object?> { arg } : new List<object?>());")

# Inserir helper dentro de RealtimeServerInstance
idx = c.find("public void Dispose()")
if idx != -1:
    c = c[:idx] + helper + "\n        " + c[idx:]

with open(path, "w", encoding="utf-8") as f:
    f.write(c)

print("REALTIME_MODULE_FIXED")
