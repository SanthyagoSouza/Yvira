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
    /// Cria janelas desktop com suporte à arquitetura multi-aba de navegadores reais (Multi-View Browser Architecture),
    /// onde cada aba possui sua própria instância isolada de CoreWebView2, preservando sessões, vídeos em execução
    /// e alternância instantânea sem recarregar páginas.
    /// </summary>
    public class YviraWindow : IDisposable
    {
        private CustomWindowForm? _form;
        private WebView2? _webView;
        private Panel? _contentPanel;          // Header/UI WebView (dock Top ou Fill para páginas internas)
        private readonly Dictionary<int, WebView2> _tabWebViews = new(); // Abas Web individuais (preserva sessão e abas independentes)
        private int _activeTabId = 1;
        private int _headerHeight = 132;
        private bool _overlayOpen = false;
        private readonly Dictionary<string, Dictionary<string, string>> _sitePermissions = new(StringComparer.OrdinalIgnoreCase);
        private CoreWebView2Environment? _sharedEnv;
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

            // Aplica o icone oficial do Yvira na janela e barra de tarefas do Windows
            try
            {
                string? exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    var appIcon = Icon.ExtractAssociatedIcon(exePath);
                    if (appIcon != null) _form.Icon = appIcon;
                }
            }
            catch { }

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

            // WebView principal: Renderiza o Header / Barra de Navegação / Abas do Navegador (ou App completo em tela cheia)
            _contentPanel = new Panel
            {
                BackColor = ColorTranslator.FromHtml(BackgroundColorHex),
                Visible = false
            };
            _form.Controls.Add(_contentPanel);

            _webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            _form.Controls.Add(_webView);

            _form.Resize += (s, e) => UpdateViewsLayout();

            _form.Load += async (s, e) =>
            {
                try
                {
                    string userDataFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Yvira", "WebView2Data_" + System.Diagnostics.Process.GetCurrentProcess().Id);
                    Directory.CreateDirectory(userDataFolder);
                    var options = new CoreWebView2EnvironmentOptions
                    {
                        AdditionalBrowserArguments = "--autoplay-policy=no-user-gesture-required --enable-features=HardwareMediaKeyHandling"
                    };
                    _sharedEnv = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);

                    // Inicializa _webView (Header/Interface)
                    await _webView.EnsureCoreWebView2Async(_sharedEnv);

                    _webView.CoreWebView2.FrameNavigationStarting += (sArgs, args) =>
                    {
                        args.AdditionalAllowedFrameAncestors = "https://yvira.local";
                    };

                    _webView.CoreWebView2.PermissionRequested += (sArgs, args) =>
                    {
                        args.State = CoreWebView2PermissionState.Allow;
                    };

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
                                if (!e.target.closest('button') && !e.target.closest('input') && !e.target.closest('a') && !e.target.closest('.btn-reorder-col') && !e.target.closest('.x') && !e.target.closest('.tab') && !e.target.closest('.new-tab')) {
                                    window.yvira.arrastar();
                                }
                            }
                        });
                    ";

                    await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(bridgeScript);

                    if (!string.IsNullOrEmpty(InitialHtmlPath) && File.Exists(InitialHtmlPath))
                    {
                        string absPath = Path.GetFullPath(InitialHtmlPath);
                        string folder = Path.GetDirectoryName(absPath)!;
                        string fileName = Path.GetFileName(absPath);

                        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                            "yvira.local",
                            folder,
                            CoreWebView2HostResourceAccessKind.Allow
                        );

                        _webView.CoreWebView2.Navigate($"https://yvira.local/{fileName}");
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

                                    // Interceptação nativa da arquitetura Multi-Aba do Navegador:
                                    if (payload is YviraMap pMap)
                                    {
                                        int tabId = 1;
                                        var tabObj = pMap.Get("tab") ?? pMap.Get("id");
                                        if (tabObj != null) int.TryParse(tabObj.ToString(), out tabId);

                                        if (string.Equals(eventName, "navigate", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string? targetUrl = pMap.Get("url")?.ToString();
                                            if (!string.IsNullOrEmpty(targetUrl))
                                            {
                                                NavigateTab(tabId, targetUrl);
                                            }
                                        }
                                        else if (string.Equals(eventName, "tab_switched", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string? targetUrl = pMap.Get("url")?.ToString() ?? "";
                                            SwitchToTab(tabId, targetUrl);
                                        }
                                        else if (string.Equals(eventName, "tab_closed", StringComparison.OrdinalIgnoreCase))
                                        {
                                            CloseTabControl(tabId);
                                        }
                                        else if (string.Equals(eventName, "set_header_height", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var hObj = pMap.Get("height");
                                            if (hObj != null && int.TryParse(hObj.ToString(), out int hVal) && hVal > 0)
                                            {
                                                _headerHeight = hVal;
                                                _form?.BeginInvoke((MethodInvoker)(() => UpdateViewsLayout()));
                                            }
                                        }
                                        else if (string.Equals(eventName, "set_overlay", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var oObj = pMap.Get("open");
                                            bool isOpen = oObj != null && (oObj is bool bVal ? bVal : oObj.ToString()?.ToLower() == "true");
                                            _overlayOpen = isOpen;
                                            _form?.BeginInvoke((MethodInvoker)(() => UpdateViewsLayout()));
                                        }
                                        else if (string.Equals(eventName, "set_site_permission", StringComparison.OrdinalIgnoreCase))
                                        {
                                            string host = pMap.Get("host")?.ToString() ?? "";
                                            string kind = pMap.Get("kind")?.ToString() ?? "";
                                            string state = pMap.Get("state")?.ToString() ?? "";
                                            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(kind))
                                            {
                                                lock (_sitePermissions)
                                                {
                                                    if (!_sitePermissions.TryGetValue(host, out var map))
                                                    {
                                                        map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                                        _sitePermissions[host] = map;
                                                    }
                                                    map[kind] = state;
                                                }
                                            }
                                        }
                                    }
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

        /// <summary>
        /// Recalcula o layout dos controles garantindo que o _contentPanel e as abas web nunca fiquem sob o cabecalho.
        /// </summary>
        private void UpdateViewsLayout()
        {
            if (_form == null || _form.IsDisposed) return;

            int clientW = _form.ClientSize.Width;
            int clientH = _form.ClientSize.Height;
            bool hasActiveWebTab = _contentPanel != null && _contentPanel.Visible;

            if (!hasActiveWebTab)
            {
                // Paginas internas ou tela cheia
                if (_webView != null)
                {
                    _webView.SetBounds(0, 0, clientW, clientH);
                }
            }
            else
            {
                // Modo Web: o WebView de cabecalho fica no topo. Se um menu/pop estiver aberto, expande para a altura total da janela para nao cortar dropdowns
                if (_webView != null)
                {
                    int topH = _overlayOpen ? clientH : _headerHeight;
                    _webView.SetBounds(0, 0, clientW, topH);
                    if (_overlayOpen)
                    {
                        _webView.BringToFront();
                    }
                    else if (_contentPanel != null)
                    {
                        _contentPanel.BringToFront();
                    }
                }

                // O painel container das paginas web comeca estritamente em Y = _headerHeight
                if (_contentPanel != null)
                {
                    int contentH = Math.Max(0, clientH - _headerHeight);
                    _contentPanel.SetBounds(0, _headerHeight, clientW, contentH);
                }
            }
        }

        /// <summary>
        /// Alterna a visibilidade para a aba informada sem recarregar ou matar as outras abas.
        /// </summary>
        public void SwitchToTab(int tabId, string currentUrl)
        {
            if (_form == null || _form.IsDisposed) return;

            _form.BeginInvoke((MethodInvoker)(() =>
            {
                try
                {
                    _activeTabId = tabId;
                    bool isInternal = string.IsNullOrEmpty(currentUrl) || currentUrl.StartsWith("yvira://", StringComparison.OrdinalIgnoreCase);

                    if (isInternal)
                    {
                        // Oculta o container web e exibe a interface interna em tela cheia
                        if (_contentPanel != null) _contentPanel.Visible = false;
                        foreach (var kvp in _tabWebViews) kvp.Value.Visible = false;
                    }
                    else
                    {
                        // Exibe o container web fixado abaixo do header
                        if (_contentPanel != null) _contentPanel.Visible = true;

                        foreach (var kvp in _tabWebViews)
                        {
                            if (kvp.Key == tabId)
                            {
                                kvp.Value.Visible = true;
                                kvp.Value.BringToFront();
                            }
                            else
                            {
                                kvp.Value.Visible = false;
                            }
                        }
                    }

                    UpdateViewsLayout();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SwitchToTab Erro]: {ex.Message}");
                }
            }));
        }

        /// <summary>
        /// Navega uma aba especifica para a URL desejada, criando sua instancia dedicada caso nao exista.
        /// </summary>
        public void NavigateTab(int tabId, string url)
        {
            if (_form == null || _form.IsDisposed) return;

            _form.BeginInvoke((MethodInvoker)(async () =>
            {
                try
                {
                    _activeTabId = tabId;
                    bool isInternal = url.StartsWith("yvira://", StringComparison.OrdinalIgnoreCase);

                    if (isInternal)
                    {
                        if (_tabWebViews.TryGetValue(tabId, out var existingWv))
                        {
                            existingWv.Visible = false;
                        }
                        if (_contentPanel != null) _contentPanel.Visible = false;
                    }
                    else
                    {
                        if (_contentPanel != null) _contentPanel.Visible = true;

                        // Obtem ou cria a instancia isolada do WebView2 dentro de _contentPanel
                        if (!_tabWebViews.TryGetValue(tabId, out var tabWv))
                        {
                            tabWv = new WebView2
                            {
                                Dock = DockStyle.Fill,
                                Visible = false
                            };

                            if (_contentPanel != null)
                            {
                                _contentPanel.Controls.Add(tabWv);
                            }
                            else
                            {
                                _form.Controls.Add(tabWv);
                            }
                            _tabWebViews[tabId] = tabWv;

                            if (_sharedEnv != null)
                            {
                                await tabWv.EnsureCoreWebView2Async(_sharedEnv);
                                tabWv.CoreWebView2.Settings.IsStatusBarEnabled = false;
                                tabWv.CoreWebView2.Settings.AreDevToolsEnabled = true;

                                tabWv.CoreWebView2.PermissionRequested += (sArgs, args) =>
                                {
                                    try
                                    {
                                        string permKindStr = args.PermissionKind switch
                                        {
                                            CoreWebView2PermissionKind.Camera => "camera",
                                            CoreWebView2PermissionKind.Microphone => "microphone",
                                            CoreWebView2PermissionKind.Geolocation => "geolocation",
                                            CoreWebView2PermissionKind.Notifications => "notifications",
                                            _ => "other"
                                        };

                                        string reqHost = "";
                                        try { reqHost = new Uri(args.Uri).Host; } catch { }

                                        string? savedState = null;
                                        lock (_sitePermissions)
                                        {
                                            if (!string.IsNullOrEmpty(reqHost) && _sitePermissions.TryGetValue(reqHost, out var map))
                                            {
                                                map.TryGetValue(permKindStr, out savedState);
                                            }
                                        }

                                        if (string.Equals(savedState, "allow", StringComparison.OrdinalIgnoreCase))
                                        {
                                            args.State = CoreWebView2PermissionState.Allow;
                                        }
                                        else if (string.Equals(savedState, "deny", StringComparison.OrdinalIgnoreCase) || string.Equals(savedState, "block", StringComparison.OrdinalIgnoreCase))
                                        {
                                            args.State = CoreWebView2PermissionState.Deny;
                                        }
                                        else
                                        {
                                            // Padrao seguro: bloqueia auto-concessao e notifica a interface
                                            args.State = CoreWebView2PermissionState.Deny;
                                            SendData("solicitacaoPermissao", new Dictionary<string, object?>
                                            {
                                                ["host"] = reqHost,
                                                ["kind"] = permKindStr,
                                                ["uri"] = args.Uri
                                            });
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[Permissao Erro]: {ex.Message}");
                                        args.State = CoreWebView2PermissionState.Deny;
                                    }
                                };

                                int capturedId = tabId;
                                tabWv.CoreWebView2.SourceChanged += (sArgs, args) =>
                                {
                                    if (tabWv.CoreWebView2 != null)
                                    {
                                        string newUrl = tabWv.CoreWebView2.Source;
                                        if (!string.IsNullOrEmpty(newUrl) && !newUrl.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase))
                                        {
                                            SendData("receberAtualizacaoUrl", new Dictionary<string, object?>
                                            {
                                                ["url"] = newUrl,
                                                ["titulo"] = tabWv.CoreWebView2.DocumentTitle,
                                                ["tabId"] = capturedId
                                            });
                                        }
                                    }
                                };

                                tabWv.CoreWebView2.DocumentTitleChanged += (sArgs, args) =>
                                {
                                    if (tabWv.CoreWebView2 != null)
                                    {
                                        SendData("receberAtualizacaoUrl", new Dictionary<string, object?>
                                        {
                                            ["url"] = tabWv.CoreWebView2.Source,
                                            ["titulo"] = tabWv.CoreWebView2.DocumentTitle,
                                            ["tabId"] = capturedId
                                        });
                                    }
                                };
                            }
                        }

                        // Oculta as outras abas e exibe esta
                        foreach (var kvp in _tabWebViews)
                        {
                            kvp.Value.Visible = (kvp.Key == tabId);
                        }

                        tabWv.BringToFront();

                        if (tabWv.CoreWebView2 != null)
                        {
                            if (tabWv.CoreWebView2.Source != url)
                            {
                                tabWv.CoreWebView2.Navigate(url);
                            }
                        }
                    }

                    UpdateViewsLayout();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NavigateTab Erro]: {ex.Message}");
                }
            }));
        }

        /// <summary>
        /// Destroi a instancia WebView2 da aba fechada liberando recursos de memoria.
        /// </summary>
        public void CloseTabControl(int tabId)
        {
            if (_form == null || _form.IsDisposed) return;

            _form.BeginInvoke((MethodInvoker)(() =>
            {
                try
                {
                    if (_tabWebViews.TryGetValue(tabId, out var wv))
                    {
                        if (_contentPanel != null) _contentPanel.Controls.Remove(wv);
                        else _form.Controls.Remove(wv);
                        wv.Dispose();
                        _tabWebViews.Remove(tabId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CloseTabControl Erro]: {ex.Message}");
                }
            }));
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
                if (!_eventHandlers.TryGetValue(eventName, out var list) || list.Count == 0) return;
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
            if (obj is Dictionary<string, object?> sDict)
            {
                return JsonSerializer.Serialize(sDict);
            }
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
            foreach (var kvp in _tabWebViews) kvp.Value.Dispose();
            _tabWebViews.Clear();
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
