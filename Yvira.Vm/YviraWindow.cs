using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Yvira.Vm
{
    /// <summary>
    /// Motor de Janela e Renderização Gráfica nativo do Yvira.
    /// Cria janelas desktop ultraleves baseadas em WebView2 com bridge bidirecional para scripts Yvira.
    /// Utiliza a arquitetura moderna de DWM (DwmExtendFrameIntoClientArea / WS_THICKFRAME)
    /// idêntica ao VSCode, Google Chrome, Spotify e Excel, permitindo redimensionar normalmente
    /// em todas as bordas e cantos do Windows com aceleração de hardware.
    /// </summary>
    public class YviraWindow : IDisposable
    {
        private CustomWindowForm? _form;
        private WebView2? _webView;
        private readonly Thread _uiThread;
        private readonly ManualResetEventSlim _readyEvent = new(false);
        private readonly ConcurrentQueue<Action> _dispatchQueue = new();
        private readonly Dictionary<string, List<object>> _eventHandlers = new(StringComparer.OrdinalIgnoreCase);

        // Propriedades da Janela
        public string Title { get; set; } = "Yvira Window";
        public int Width { get; set; } = 1000;
        public int Height { get; set; } = 700;
        public int MinWidth { get; set; } = 600;
        public int MinHeight { get; set; } = 400;
        public bool Resizable { get; set; } = true;
        public bool Frameless { get; set; } = true;
        public string BackgroundColorHex { get; set; } = "#121212";
        public string? InitialHtmlPath { get; set; }
        public string WindowMode { get; set; } = "normal";

        public Interpreter? InterpreterInstance { get; set; }

        public YviraWindow()
        {
            _uiThread = new Thread(RunMessageLoop)
            {
                IsBackground = true
            };
            _uiThread.SetApartmentState(ApartmentState.STA);
        }

        public void Start()
        {
            if (!_uiThread.IsAlive)
            {
                _uiThread.Start();
                _readyEvent.Wait();
            }
        }

        private void RunMessageLoop()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _form = new CustomWindowForm(Frameless, Resizable)
            {
                Text = Title,
                Width = Width,
                Height = Height,
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = ColorTranslator.FromHtml(BackgroundColorHex),
                MinimumSize = new Size(MinWidth, MinHeight)
            };

            if (string.Equals(WindowMode, "semi-full", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(WindowMode, "tela-semi-inteira", StringComparison.OrdinalIgnoreCase))
            {
                var workArea = Screen.PrimaryScreen != null ? Screen.PrimaryScreen.WorkingArea : Screen.GetWorkingArea(_form);
                _form.StartPosition = FormStartPosition.Manual;
                _form.Location = workArea.Location;
                _form.Size = workArea.Size;
            }
            else if (string.Equals(WindowMode, "fullscreen", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(WindowMode, "tela-cheia", StringComparison.OrdinalIgnoreCase))
            {
                var bounds = Screen.PrimaryScreen != null ? Screen.PrimaryScreen.Bounds : Screen.GetBounds(_form);
                _form.FormBorderStyle = FormBorderStyle.None;
                _form.StartPosition = FormStartPosition.Manual;
                _form.Location = bounds.Location;
                _form.Size = bounds.Size;
            }

            _webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            _form.Controls.Add(_webView);

            _form.Load += async (s, e) =>
            {
                try
                {
                    string userDataFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Yvira", "WebView2Data_" + System.Diagnostics.Process.GetCurrentProcess().Id);
                    Directory.CreateDirectory(userDataFolder);
                    var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                    await _webView.EnsureCoreWebView2Async(env);

                    _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                    _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                    _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                    string bridgeScript = @"
                        window.yvira = {
                            emit: function(evento, dados) {
                                window.chrome.webview.postMessage(JSON.stringify({
                                    action: 'event',
                                    name: evento,
                                    payload: dados || null
                                }));
                            },
                            minimizar: function() {
                                window.chrome.webview.postMessage(JSON.stringify({ action: 'minimize' }));
                            },
                            maximizar: function() {
                                window.chrome.webview.postMessage(JSON.stringify({ action: 'maximize' }));
                            },
                            fechar: function() {
                                window.chrome.webview.postMessage(JSON.stringify({ action: 'close' }));
                            },
                            arrastar: function() {
                                window.chrome.webview.postMessage(JSON.stringify({ action: 'drag' }));
                            }
                        };

                        document.addEventListener('mousedown', function(e) {
                            if (e.target.closest('[data-arrastavel=""true""]') || e.target.closest('.titlebar') || e.target.closest('.yvira-titlebar')) {
                                if (!e.target.closest('button') && !e.target.closest('input') && !e.target.closest('a') && !e.target.closest('.btn-reorder-col')) {
                                    window.yvira.arrastar();
                                }
                            }
                        });
                    ";

                    await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(bridgeScript);

                    if (!string.IsNullOrEmpty(InitialHtmlPath) && File.Exists(InitialHtmlPath))
                    {
                        string absPath = Path.GetFullPath(InitialHtmlPath);
                        _webView.CoreWebView2.Navigate(new Uri(absPath).AbsoluteUri);
                    }
                    else
                    {
                        string defaultHtml = $@"
                            <!DOCTYPE html>
                            <html>
                            <head>
                                <meta charset='utf-8' />
                                <style>
                                    body {{
                                        margin: 0;
                                        background-color: {BackgroundColorHex};
                                        color: #ffffff;
                                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                                        user-select: none;
                                        display: flex;
                                        flex-direction: column;
                                        height: 100vh;
                                    }}
                                    .titlebar {{
                                        display: flex;
                                        justify-content: space-between;
                                        align-items: center;
                                        background: #1e1e1e;
                                        padding: 8px 16px;
                                        border-bottom: 1px solid #2d2d2d;
                                        cursor: grab;
                                    }}
                                    .btn-janela {{
                                        background: transparent;
                                        border: none;
                                        color: #aaa;
                                        font-size: 14px;
                                        width: 30px;
                                        height: 30px;
                                        cursor: pointer;
                                    }}
                                    .btn-janela:hover {{ background: #333; color: #fff; }}
                                    .btn-fechar:hover {{ background: #e81123 !important; color: #fff !important; }}
                                    .conteudo {{
                                        flex: 1;
                                        padding: 24px;
                                        display: flex;
                                        flex-direction: column;
                                        align-items: center;
                                        justify-content: center;
                                    }}
                                </style>
                            </head>
                            <body>
                                <div class='titlebar' data-arrastavel='true'>
                                    <span>{Title}</span>
                                    <div>
                                        <button class='btn-janela' onclick='window.yvira.minimizar()'>─</button>
                                        <button class='btn-janela' onclick='window.yvira.maximizar()'>□</button>
                                        <button class='btn-janela btn-fechar' onclick='window.yvira.fechar()'>✕</button>
                                    </div>
                                </div>
                                <div class='conteudo'>
                                    <h1>Yvira Window Engine</h1>
                                    <p>Pronto para renderizar interfaces visuais de jogos e sistemas.</p>
                                </div>
                            </body>
                            </html>
                        ";
                        _webView.CoreWebView2.NavigateToString(defaultHtml);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[YviraWindow Erro]: {ex.Message}");
                }
                finally
                {
                    _readyEvent.Set();
                }
            };

            Application.Run(_form);
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string rawJson = e.TryGetWebMessageAsString();
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("action", out var actProp))
                {
                    string action = actProp.GetString() ?? "";
                    switch (action)
                    {
                        case "minimize":
                            _form?.Invoke((MethodInvoker)(() => _form.WindowState = FormWindowState.Minimized));
                            break;

                        case "maximize":
                            _form?.Invoke((MethodInvoker)(() =>
                            {
                                _form.WindowState = _form.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
                            }));
                            break;

                        case "close":
                            _form?.Invoke((MethodInvoker)(() => _form.Close()));
                            break;

                        case "drag":
                            _form?.Invoke((MethodInvoker)(() =>
                            {
                                ReleaseCapture();
                                SendMessage(_form.Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
                            }));
                            break;

                        case "event":
                            if (root.TryGetProperty("name", out var nameProp))
                            {
                                string eventName = nameProp.GetString() ?? "";
                                object? payload = null;
                                if (root.TryGetProperty("payload", out var pProp))
                                {
                                    payload = ParseJsonElementToYvira(pProp);
                                }
                                TriggerEvent(eventName, payload);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebMessage Erro]: {ex.Message}");
            }
        }

        public void On(string eventName, object callback)
        {
            lock (_eventHandlers)
            {
                if (!_eventHandlers.ContainsKey(eventName))
                {
                    _eventHandlers[eventName] = new List<object>();
                }
                _eventHandlers[eventName].Add(callback);
            }
        }

        private void TriggerEvent(string eventName, object? payload)
        {
            List<object> callbacks;
            lock (_eventHandlers)
            {
                if (!_eventHandlers.TryGetValue(eventName, out var list)) return;
                callbacks = new List<object>(list);
            }

            if (InterpreterInstance == null) return;

            foreach (var cb in callbacks)
            {
                var args = new List<object?>();
                if (payload != null) args.Add(payload);

                try
                {
                    if (cb is string funcName)
                    {
                        var fnObj = InterpreterInstance.Environment.Get(funcName);
                        if (fnObj is YviraFunction yf) yf.Call(InterpreterInstance, args);
                        else if (fnObj is HostFunction hf) hf.Call(InterpreterInstance, args);
                    }
                    else if (cb is YviraFunction fn)
                    {
                        fn.Call(InterpreterInstance, args);
                    }
                    else if (cb is HostFunction hf)
                    {
                        hf.Call(InterpreterInstance, args);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Erro Callback Yvira]: {ex.Message}");
                }
            }
        }

        public void ExecuteScript(string javascript)
        {
            if (_form == null || _form.IsDisposed || _webView == null) return;

            try
            {
                if (_form.InvokeRequired)
                {
                    _form.BeginInvoke((MethodInvoker)(async () =>
                    {
                        try
                        {
                            if (_webView != null && _webView.CoreWebView2 != null)
                            {
                                await _webView.CoreWebView2.ExecuteScriptAsync(javascript);
                            }
                        }
                        catch { }
                    }));
                }
                else
                {
                    if (_webView.CoreWebView2 != null)
                    {
                        _ = _webView.CoreWebView2.ExecuteScriptAsync(javascript);
                    }
                }
            }
            catch { }
        }

        public void SendData(string functionName, object? data)
        {
            string json = SerializeYviraObject(data);
            string script = $"if (window['{functionName}']) {{ window['{functionName}']({json}); }}";
            ExecuteScript(script);
        }

        private static string SerializeYviraObject(object? obj)
        {
            if (obj == null) return "null";
            if (obj is YviraMap yMap)
            {
                var dict = new Dictionary<string, object?>();
                foreach (var kvp in yMap)
                {
                    dict[kvp.Key] = kvp.Value is YviraMap or YviraList ? JsonDocument.Parse(SerializeYviraObject(kvp.Value)).RootElement.Clone() : kvp.Value;
                }
                return JsonSerializer.Serialize(dict);
            }
            if (obj is YviraList yList)
            {
                var list = new List<object?>();
                foreach (var item in yList.Items)
                {
                    list.Add(item is YviraMap or YviraList ? JsonDocument.Parse(SerializeYviraObject(item)).RootElement.Clone() : item);
                }
                return JsonSerializer.Serialize(list);
            }
            return JsonSerializer.Serialize(obj);
        }

        private static object? ParseJsonElementToYvira(JsonElement elem)
        {
            switch (elem.ValueKind)
            {
                case JsonValueKind.Object:
                    var yMap = new YviraMap();
                    foreach (var prop in elem.EnumerateObject())
                    {
                        yMap.Set(prop.Name, ParseJsonElementToYvira(prop.Value));
                    }
                    return yMap;

                case JsonValueKind.Array:
                    var yList = new YviraList();
                    foreach (var item in elem.EnumerateArray())
                    {
                        yList.Add(ParseJsonElementToYvira(item));
                    }
                    return yList;

                case JsonValueKind.String:
                    return elem.GetString();

                case JsonValueKind.Number:
                    if (elem.TryGetInt64(out long lVal)) return (double)lVal;
                    return elem.GetDouble();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                default:
                    return null;
            }
        }

        public void Close()
        {
            _form?.Invoke((MethodInvoker)(() => _form.Close()));
        }

        public bool IsOpen => _form != null && !_form.IsDisposed && _form.Visible;

        public void WaitUntilClosed()
        {
            while (IsOpen)
            {
                Thread.Sleep(100);
            }
        }

        public void Dispose()
        {
            _readyEvent.Dispose();
            _form?.Dispose();
        }

        /// <summary>
        /// Janela Windows Forms customizada com suporte nativo de DWM e bordas de redimensionamento idênticas ao VSCode e navegadores.
        /// Remove a barra nativa do Windows preservando a moldura de redimensionamento do sistema operacional.
        /// </summary>
        private class CustomWindowForm : Form
        {
            private readonly bool _frameless;
            private readonly bool _resizable;

            private const int WM_NCCALCSIZE = 0x0083;
            private const int WM_NCHITTEST = 0x0084;
            private const int WS_THICKFRAME = 0x00040000;
            private const int WS_CAPTION = 0x00C00000;

            [DllImport("dwmapi.dll")]
            private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

            [StructLayout(LayoutKind.Sequential)]
            private struct MARGINS
            {
                public int cxLeftWidth;
                public int cxRightWidth;
                public int cyTopHeight;
                public int cyBottomHeight;
            }

            public CustomWindowForm(bool frameless, bool resizable)
            {
                _frameless = frameless;
                _resizable = resizable;

                if (_frameless)
                {
                    FormBorderStyle = FormBorderStyle.Sizable;
                }
                else if (!_resizable)
                {
                    FormBorderStyle = FormBorderStyle.FixedSingle;
                    MaximizeBox = false;
                }
                else
                {
                    FormBorderStyle = FormBorderStyle.Sizable;
                }
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    if (_frameless && _resizable)
                    {
                        cp.Style |= WS_THICKFRAME;
                    }
                    return cp;
                }
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                if (_frameless)
                {
                    var margins = new MARGINS { cxLeftWidth = 1, cxRightWidth = 1, cyTopHeight = 1, cyBottomHeight = 1 };
                    DwmExtendFrameIntoClientArea(Handle, ref margins);
                }
            }

            protected override void WndProc(ref Message m)
            {
                if (_frameless && m.Msg == WM_NCCALCSIZE)
                {
                    // Removendo a área não-cliente do Windows: a janela preenche todo o espaço
                    // mas mantém a moldura invisível com as setinhas de redimensionamento ativas
                    if (m.WParam != IntPtr.Zero)
                    {
                        m.Result = IntPtr.Zero;
                        return;
                    }
                }

                base.WndProc(ref m);
            }
        }
    }
}
