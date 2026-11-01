import os

path = r"C:\Users\santh\OneDrive\Área de Trabalho\ARQQUIVOS DRIVE\Beta OS\Yvira.Vm\YviraWindow.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

target = """        public void ExecuteScript(string javascript)
        {
            if (_form != null && _webView != null && _webView.CoreWebView2 != null)
            {
                _form.Invoke((MethodInvoker)(async () =>
                {
                    try
                    {
                        await _webView.CoreWebView2.ExecuteScriptAsync(javascript);
                    }
                    catch { }
                }));
            }
        }"""

replacement = """        public void ExecuteScript(string javascript)
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
        }"""

if target in c:
    c = c.replace(target, replacement)
    with open(path, "w", encoding="utf-8") as f:
        f.write(c)
    print("THREAD_MARSHAL_FIXED")
else:
    print("TARGET_NOT_FOUND")
