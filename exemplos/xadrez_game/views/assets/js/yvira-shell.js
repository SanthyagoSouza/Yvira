/**
 * Yvira Window & Tab Shell Controller
 * Fornece gerenciamento nativo de abas e botões de janela conectados ao IPC Yvira
 */
class YviraShell {
    constructor(options = {}) {
        this.tabsContainer = document.getElementById(options.tabsId || "yvira_tabs");
        this.newTabBtn = document.getElementById(options.newTabId || "yvira_new_tab");
        this.viewport = document.getElementById(options.viewportId || "yvira_viewport");
        this.tabs = [];
        this.tabCount = 0;
        this.onTabChange = options.onTabChange || null;
        this.onNewTab = options.onNewTab || null;

        this._initControls();
        this._initTabEvents();
    }

    _initControls() {
        // Ondulação dos botões de controle
        document.querySelectorAll(".yvira-btn").forEach(btn => {
            btn.addEventListener("pointerdown", e => {
                const r = btn.getBoundingClientRect();
                const s = Math.max(r.width, r.height);
                const el = document.createElement("span");
                el.className = "ripple";
                el.style.cssText = `width:${s}px;height:${s}px;left:${e.clientX - r.left - s/2}px;top:${e.clientY - r.top - s/2}px`;
                btn.appendChild(el);
                setTimeout(() => el.remove(), 600);
            });
        });

        // Botão Nova Aba
        if (this.newTabBtn) {
            this.newTabBtn.addEventListener("click", () => {
                if (this.onNewTab) {
                    this.onNewTab(this);
                } else {
                    this.adicionarAba("Nova Aba " + (this.tabs.length + 1));
                }
            });
        }
    }

    _initTabEvents() {
        if (!this.tabsContainer) return;
        this.tabsContainer.addEventListener("click", e => {
            const tabEl = e.target.closest(".yvira-tab");
            if (!tabEl) return;

            const tabId = tabEl.dataset.tabId;

            // Fechar aba
            if (e.target.closest(".yv-close-tab")) {
                this.fecharAba(tabId);
                return;
            }

            // Selecionar aba
            this.selecionarAba(tabId);
        });
    }

    adicionarAba(titulo, paneElement = null, closable = true) {
        this.tabCount++;
        const tabId = "tab_" + this.tabCount;

        const tabEl = document.createElement("div");
        tabEl.className = "yvira-tab";
        tabEl.dataset.tabId = tabId;
        tabEl.innerHTML = `
            <span class="yv-dot"></span>
            <span class="yv-label">${titulo}</span>
            ${closable ? '<span class="yv-close-tab" title="Fechar aba">✕</span>' : ''}
        `;

        this.tabsContainer.appendChild(tabEl);

        const tabData = {
            id: tabId,
            titulo: titulo,
            tabEl: tabEl,
            paneEl: paneElement
        };
        this.tabs.push(tabData);

        this.selecionarAba(tabId);
        return tabData;
    }

    selecionarAba(tabId) {
        this.tabs.forEach(t => {
            if (t.id === tabId) {
                t.tabEl.classList.add("active");
                if (t.paneEl) t.paneEl.classList.add("active");
            } else {
                t.tabEl.classList.remove("active");
                if (t.paneEl) t.paneEl.classList.remove("active");
            }
        });

        if (this.onTabChange) {
            const tabAtual = this.tabs.find(t => t.id === tabId);
            this.onTabChange(tabAtual);
        }
    }

    fecharAba(tabId) {
        if (this.tabs.length <= 1) return; // Não fecha a última aba

        const idx = this.tabs.findIndex(t => t.id === tabId);
        if (idx === -1) return;

        const tab = this.tabs[idx];
        tab.tabEl.remove();
        if (tab.paneEl) tab.paneEl.remove();

        this.tabs.splice(idx, 1);

        // Se a aba fechada era a ativa, ativa a anterior ou a primeira
        const proximoIdx = Math.max(0, idx - 1);
        if (this.tabs[proximoIdx]) {
            this.selecionarAba(this.tabs[proximoIdx].id);
        }
    }
}


// Funcoes globais de controle de janela com animacoes
window.fecharJanelaComAnimacao = function() {
    const container = document.querySelector('.janela-container') || document.body;
    container.classList.add('janela-fechando');
    setTimeout(() => {
        window.yvira.fechar();
    }, 280); // 280ms exato do janela-yvira.html
};

window.minimizarJanelaComAnimacao = function() {
    const container = document.querySelector('.janela-container') || document.body;
    container.classList.add('janela-minimizando');
    setTimeout(() => {
        window.yvira.minimizar();
        setTimeout(() => {
            container.classList.remove('janela-minimizando');
        }, 400);
    }, 320); // 320ms para completar a curva cubic-bezier(.22,1,.36,1)
};

window.maximizarJanelaComAnimacao = function() {
    const container = document.querySelector('.janela-container') || document.body;
    container.classList.remove('janela-maximizando');
    void container.offsetWidth; // Forca reflow
    container.classList.add('janela-maximizando');
    window.yvira.maximizar();
};

window.YviraShell = YviraShell;
