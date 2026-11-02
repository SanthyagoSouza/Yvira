with open('exemplos/yvira_browser/janela-yvira.html', 'r', encoding='utf-8') as f:
    text = f.read()

# 1. Update activate and closeTab
pos_act = text.find('function activate(id){')
end_close = text.find('function openInternal(url){', pos_act)

new_tabs_code = """function activate(id){
    if (activeId === id) return;
    activeId = id;
    renderTabs();
    const t = cur();
    show(t);
    bridge('tab_switched', { tab: t.id, id: t.id, url: t.url, private: t.priv });
  }

  function closeTab(id){
    const idx = tabs.findIndex(t => t.id === id);
    if (idx < 0) return;

    bridge('tab_closed', { tab: id, id: id });

    if (tabs.length === 1){
      const t = tabs[0];
      t.priv = false;
      t.stack = ['yvira://newtab'];
      t.i = 0;
      t.url = t.stack[0];
      load(t);
      return;
    }

    tabs.splice(idx, 1);
    if (id === activeId) {
      activeId = (tabs[idx] || tabs[idx-1]).id;
    }
    renderTabs();
    const t = cur();
    show(t);
    bridge('tab_switched', { tab: t.id, id: t.id, url: t.url, private: t.priv });
  }

  """

text = text[:pos_act] + new_tabs_code + text[end_close:]

# 2. Update tabsEl click listener so that clicking .x immediately calls closeTab
pos_click = text.find("tabsEl.addEventListener('click', e => { if (e.target.closest('.x')) return; const tab = e.target.closest('.tab'); if (tab) activate(+tab.dataset.id); });")
new_click = """tabsEl.addEventListener('click', e => {
    const closeBtn = e.target.closest('.x');
    if (closeBtn) {
      e.preventDefault();
      e.stopPropagation();
      const tab = closeBtn.closest('.tab');
      if (tab) closeTab(+tab.dataset.id);
      return;
    }
    const tab = e.target.closest('.tab');
    if (tab) activate(+tab.dataset.id);
  });"""

text = text[:pos_click] + new_click + text[pos_click + len("tabsEl.addEventListener('click', e => { if (e.target.closest('.x')) return; const tab = e.target.closest('.tab'); if (tab) activate(+tab.dataset.id); });"):]

# 3. Update updateCurrentUrl and receberAtualizacaoUrl to receive tab id
pos_update = text.find('updateCurrentUrl: function(url, title) {')
end_receber = text.find('/* ================= Início ================= */', pos_update)

new_update_code = """updateCurrentUrl: function(url, title, tabId) {
      const targetId = tabId ? +tabId : activeId;
      const t = tabs.find(x => x.id === targetId) || cur();
      if (!t) return;
      if (url && t.url !== url) {
        t.url = url;
        if (t.stack[t.i] !== url) {
          t.stack = t.stack.slice(0, t.i + 1);
          t.stack.push(url);
          t.i++;
        }
        if (t.id === activeId && document.activeElement !== urlIn) {
          urlIn.value = url;
        }
      }
      if (title) {
        t.title = title;
      }
      renderTabs();
    }
  };

  window.receberAtualizacaoUrl = function(dados) {
    if (dados && dados.url) {
      window.YviraBrowser.updateCurrentUrl(dados.url, dados.titulo || '', dados.tabId || dados.tab || 0);
    }
  };

  """

text = text[:pos_update] + new_update_code + text[end_receber:]

with open('exemplos/yvira_browser/janela-yvira.html', 'w', encoding='utf-8') as f:
    f.write(text)

print('janela-yvira.html updated successfully!')
