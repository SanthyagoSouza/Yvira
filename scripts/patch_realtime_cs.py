import os

path = r"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\Yvira.Vm\RealtimeModule.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

c = c.replace("private static object? ConvertJsonElement(JsonElement el)", "public static object? ConvertJsonElement(JsonElement el)")
with open(path, "w", encoding="utf-8") as f:
    f.write(c)

print("REALTIME_CS_PATCHED")
