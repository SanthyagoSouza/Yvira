with open('exemplos/yvira_browser/janela-yvira.html', 'r', encoding='utf-8') as f:
    text = f.read()

pos = text.find('function pWeb(t, m){')
end = text.find('function pSearch(t, m){', pos)

new_pweb = """function pWeb(t, m){
    return `<div class="appslot web-container" style="width:100%;height:100%;display:flex;flex-direction:column;position:relative;background:#0d0e15;">
      <div style="flex:1;display:flex;align-items:center;justify-content:center;flex-direction:column;gap:14px;color:#8a90a8;">
        <div style="width:32px;height:32px;border:3px solid rgba(255,255,255,0.1);border-top-color:#7c5cff;border-radius:50%;animation:spin 0.8s linear infinite;"></div>
        <div style="font-size:14px;font-weight:500;">Carregando ${esc(hostOf(t.url))}...</div>
      </div>
      <style>@keyframes spin { to { transform: rotate(360deg); } }</style>
    </div>`;
  }
  """

updated = text[:pos] + new_pweb + text[end:]
with open('exemplos/yvira_browser/janela-yvira.html', 'w', encoding='utf-8') as f:
    f.write(updated)
print('Updated pWeb successfully!')
