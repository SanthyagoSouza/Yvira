import os

path = r"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\Yvira.Vm\RealtimeModule.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Helper para normalizar tipos Yvira (YviraMap, YviraList) antes do JsonSerializer.Serialize
converter = """
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
"""

# Inserir antes de ConvertJsonElement
idx = c.find("public static object? ConvertJsonElement(JsonElement el)")
if idx != -1:
    c = c[:idx] + converter + "\n        " + c[idx:]

# Usar NormalizeForJson em SendAsync do RealtimeConnectedClient
old_send = """            var packet = new { evento = eventName, payload = payload };
            string json = JsonSerializer.Serialize(packet);"""
new_send = """            var packet = new { evento = eventName, payload = RealtimeServerInstance.NormalizeForJson(payload) };
            string json = JsonSerializer.Serialize(packet);"""
c = c.replace(old_send, new_send)

# Usar NormalizeForJson em EmitAsync do RealtimeClientInstance
old_emit = """            var packet = new { evento = eventName, payload = payload };
            string json = JsonSerializer.Serialize(packet);"""
new_emit = """            var packet = new { evento = eventName, payload = RealtimeServerInstance.NormalizeForJson(payload) };
            string json = JsonSerializer.Serialize(packet);"""
c = c.replace(old_emit, new_emit)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)

print("SERIALIZATION_FIXED")
