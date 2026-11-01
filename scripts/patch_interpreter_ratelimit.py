import os

path = r"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\Yvira.Vm\Interpreter.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Atualiza a instanciação do RealtimeServerInstance para suportar maxPackets
old_instantiation = "var server = new RealtimeServerInstance(porta, segredo, rotacao, this);"
new_instantiation = """int limitePacotes = 60;
            if (evaluatedArgs.Count > 0 && evaluatedArgs[0] is YviraMap optMapLimit)
            {
                var lp = optMapLimit.Get("limitePacotes") ?? optMapLimit.Get("maxPackets");
                if (lp != null) limitePacotes = Convert.ToInt32(lp);
            }
            var server = new RealtimeServerInstance(porta, segredo, rotacao, limitePacotes, this);"""

if old_instantiation in c:
    c = c.replace(old_instantiation, new_instantiation)
    with open(path, "w", encoding="utf-8") as f:
        f.write(c)
    print("INTERPRETER_RATELIMIT_PATCHED")
else:
    print("INSTANTIATION_NOT_FOUND")
