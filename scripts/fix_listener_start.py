import os

path = r"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\Yvira.Vm\RealtimeModule.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

target = """            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{Port}/");
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
            try
            {
                _listener.Start();
            }
            catch (HttpListenerException)
            {
                _listener.Prefixes.Clear();
                _listener.Prefixes.Add($"http://localhost:{Port}/");
                _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
                _listener.Start();
            }"""

replacement = """            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
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
            _listener.Start();"""

if target in c:
    c = c.replace(target, replacement)
    with open(path, "w", encoding="utf-8") as f:
        f.write(c)
    print("LISTENER_START_FIXED")
else:
    print("TARGET_NOT_FOUND")
