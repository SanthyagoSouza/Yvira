<script lang="ts">
  import { onMount } from 'svelte'
  import TabBar from './components/TabBar.svelte'
  import AddressBar from './components/AddressBar.svelte'
  import { store } from './lib/store.svelte'
  import { bridge } from './lib/bridge'

  onMount(() => {
    // Escuta eventos vindos do Host Yvira nativo (.NET)
    window.receberAtualizacaoUrl = (dados) => {
      if (dados && dados.url) {
        store.updateUrlFromNative(dados.url, dados.titulo || '', dados.tabId || 0)
      }
    }

    window.rastreadorBloqueadoNativo = (dados) => {
      if (dados && dados.host) {
        store.incrementTrackerBlock(dados.tabId)
      }
    }

    window.solicitarNovaAbaExterna = (dados) => {
      if (dados && dados.url) {
        store.createTab(dados.url, store.isPrivate)
      }
    }

    window.downloadIniciado = (dados) => {
      if (dados && dados.arquivo) {
        store.showToast(dados.perigoso ? `⚠️ Executável detectado: ${dados.arquivo}` : `📥 Download: ${dados.arquivo}`)
      }
    }

    window.alertaSeguranca = (dados) => {
      if (dados) {
        store.showToast(`🛡️ Bloqueado: ${dados.uri}`)
      }
    }

    bridge.setHeaderHeight(90)
  })

  function handleNewTab() {
    store.createTab('yvira://newtab', store.isPrivate)
  }

  function getHost(url: string) {
    try {
      if (!url || url.startsWith('yvira://')) return 'yvira.local'
      return new URL(url).host
    } catch {
      return 'yvira.local'
    }
  }
</script>

<div 
  class="win flex flex-col h-screen w-screen overflow-hidden select-none"
  onclick={(e) => {
    const target = e.target as HTMLElement
    if (!target.closest('.menu') && !target.closest('.pop') && !target.closest('#kebab') && !target.closest('#siteInfoBtn') && !target.closest('#shieldBtn')) {
      if (store.menuOpen || store.shieldOpen || store.siteInfoOpen) {
        store.closeAllOverlays()
      }
    }
  }}
>
  <!-- Barra de Abas e Janela -->
  <TabBar onNewTab={handleNewTab} />

  <!-- Barra de Ferramentas / Omnibox -->
  <AddressBar />

  <!-- Área de Página Interna (Apenas exibida quando a URL for yvira://) -->
  {#if store.activeTab?.url === 'yvira://newtab'}
    <div class="flex-1 flex flex-col items-center justify-center p-6 bg-gradient-to-b from-[#0d0f17] to-[#07080d] text-[#e8eaf3]">
      <div class="flex flex-col items-center gap-6 max-w-lg w-full">
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 rounded-2xl bg-gradient-to-tr from-[#7c5cff] to-[#22d3ee] p-[1px] shadow-lg shadow-cyan-500/10">
            <div class="w-full h-full bg-[#07080d] rounded-2xl flex items-center justify-center">
              <span class="text-xl font-black bg-gradient-to-r from-[#7c5cff] to-[#22d3ee] bg-clip-text text-transparent">Y</span>
            </div>
          </div>
          <span class="text-2xl font-bold tracking-tight text-white">Yvira Browser</span>
        </div>

        <div class="w-full">
          <input
            type="text"
            placeholder="Pesquisar com Google ou digitar endereço..."
            onkeydown={(e) => {
              if (e.key === 'Enter' && e.currentTarget.value.trim()) {
                store.navigateActiveTab(e.currentTarget.value)
              }
            }}
            class="w-full h-12 px-5 rounded-2xl bg-white/[0.04] border border-white/[0.08] focus:border-[#7c5cff] focus:ring-4 focus:ring-[#7c5cff]/20 outline-none text-sm text-white placeholder-white/40 transition-all duration-300 shadow-xl shadow-black/40"
          />
        </div>

        <div class="grid grid-cols-4 gap-3 w-full pt-4">
          {#each [
            { name: 'Google', url: 'https://www.google.com', icon: 'G' },
            { name: 'GitHub', url: 'https://github.com', icon: 'GH' },
            { name: 'YouTube', url: 'https://www.youtube.com', icon: 'YT' },
            { name: 'Wikipédia', url: 'https://www.wikipedia.org', icon: 'W' }
          ] as atalho}
            <button
              onclick={() => store.navigateActiveTab(atalho.url)}
              class="flex flex-col items-center gap-2 p-3 rounded-2xl bg-white/[0.03] hover:bg-white/[0.08] border border-white/[0.05] hover:border-white/10 transition-all duration-200 active:scale-95 group"
            >
              <div class="w-10 h-10 rounded-xl bg-white/[0.06] flex items-center justify-center font-bold text-sm text-white/80 group-hover:text-white transition-colors">
                {atalho.icon}
              </div>
              <span class="text-xs text-[#8a90a8] group-hover:text-[#e8eaf3] transition-colors">{atalho.name}</span>
            </button>
          {/each}
        </div>
      </div>
    </div>
  {/if}

  <!-- MODAL DO MEIO DA TELA: INFORMAÇÕES DO SITE E PERMISSÕES (CADEADO) -->
  {#if store.siteInfoOpen}
    {@const host = getHost(store.activeTab?.url || '')}
    {@const isHttps = !!store.activeTab?.url?.startsWith('https://')}
    <!-- Fundo Backdrop Translúcido Escurecido -->
    <div 
      class="fixed inset-0 bg-black/60 backdrop-blur-sm z-[9990] flex items-center justify-center p-4 animate-in fade-in duration-150"
      onclick={() => store.closeAllOverlays()}
    >
      <!-- Janela do Modal Centralizado -->
      <div 
        class="pop modal-center show relative w-full max-w-[360px] bg-[#1a1c28]/95 border border-white/15 rounded-2xl p-5 shadow-2xl z-[9995]"
        onclick={(e) => e.stopPropagation()}
      >
        <div class="flex items-center justify-between mb-3">
          <h3 class="text-sm font-bold text-white flex items-center gap-2">
            <svg class="w-4 h-4 text-emerald-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
              <rect x="5" y="11" width="14" height="9" rx="2"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/>
            </svg>
            Informações do site
          </h3>
          <button 
            onclick={() => store.closeAllOverlays()} 
            class="w-6 h-6 rounded-lg flex items-center justify-center hover:bg-white/10 text-white/60 hover:text-white transition-colors"
          >
            ✕
          </button>
        </div>

        <div class="flex items-center gap-2 text-xs mb-3 {isHttps ? 'text-emerald-400' : 'text-amber-400'}">
          <span class="font-medium">{isHttps ? 'Conexão segura (HTTPS)' : 'Página local / interna'}</span>
        </div>

        <p class="text-xs font-mono font-semibold text-white/90 bg-black/40 px-3 py-2 rounded-xl mb-4 truncate border border-white/5">
          {host}
        </p>

        <div class="text-xs text-[#8a90a8] font-medium mb-2">Permissões do site:</div>
        <div class="flex flex-col gap-2.5 text-xs">
          {#each [
            { id: 'camera', label: 'Câmera' },
            { id: 'microphone', label: 'Microfone' },
            { id: 'notifications', label: 'Notificações' },
            { id: 'geolocation', label: 'Localização' }
          ] as perm}
            <div class="flex items-center justify-between py-1 border-b border-white/5">
              <span class="text-white/80">{perm.label}</span>
              <select 
                class="bg-white/10 border border-white/15 text-white rounded-lg px-2 py-1 text-xs outline-none cursor-pointer"
                onchange={(e) => {
                  bridge.emit('set_site_permission', { host, kind: perm.id, state: e.currentTarget.value })
                  store.showToast(`Permissão de ${perm.label} alterada`)
                }}
              >
                <option value="allow" class="bg-[#1a1c28]">Permitir</option>
                <option value="deny" class="bg-[#1a1c28]">Bloquear</option>
                <option value="prompt" selected class="bg-[#1a1c28]">Perguntar</option>
              </select>
            </div>
          {/each}
        </div>

        <button 
          onclick={() => store.closeAllOverlays()}
          class="w-full mt-5 py-2 rounded-xl bg-gradient-to-r from-[#7c5cff] to-[#22d3ee] text-xs font-semibold text-white shadow-lg shadow-[#7c5cff]/20 hover:brightness-110 active:scale-98 transition-all"
        >
          Concluir
        </button>
      </div>
    </div>
  {/if}

  <!-- MODAL DO ESCUDO DE PRIVACIDADE -->
  {#if store.shieldOpen}
    <div 
      class="fixed inset-0 z-[9990]" 
      onclick={() => store.closeAllOverlays()}
    >
      <div 
        class="absolute top-24 right-14 w-80 bg-[#1a1c28]/95 border border-white/15 rounded-2xl p-5 shadow-2xl z-[9995] animate-in fade-in slide-in-from-top-2 duration-150"
        onclick={(e) => e.stopPropagation()}
      >
        <div class="flex items-center justify-between mb-2">
          <h3 class="text-sm font-bold text-white flex items-center gap-2">
            <svg class="w-4 h-4 text-[#7c5cff]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
            </svg>
            Relatório de Privacidade
          </h3>
          <button onclick={() => store.closeAllOverlays()} class="text-white/60 hover:text-white text-xs">✕</button>
        </div>

        <div class="bignum text-3xl font-extrabold my-2 text-transparent bg-clip-text bg-gradient-to-r from-[#7c5cff] to-[#22d3ee]">
          {store.activeTab?.blocked || 0}
        </div>
        <p class="text-xs text-[#8a90a8] mb-4">rastreadores e anúncios invasivos bloqueados nesta página.</p>

        <div class="p-3 rounded-xl bg-white/5 border border-white/5 text-xs text-white/90">
          Total acumulado de ameaças bloqueadas: <b class="text-emerald-400 font-bold">{store.blockedTotal}</b>
        </div>
      </div>
    </div>
  {/if}

  <!-- MENU SUSPENSO (3 PONTINHOS / KEBAB) -->
  {#if store.menuOpen}
    <div 
      class="fixed inset-0 z-[9990]" 
      onclick={() => store.closeAllOverlays()}
    >
      <div 
        class="absolute top-24 right-3 w-72 bg-[#1a1c28]/95 border border-white/15 rounded-2xl p-2 shadow-2xl z-[9995] flex flex-col gap-0.5 animate-in fade-in slide-in-from-top-2 duration-150"
        onclick={(e) => e.stopPropagation()}
      >
        <button 
          onclick={() => { store.closeAllOverlays(); store.createTab('yvira://newtab', false); }}
          class="mi flex items-center justify-between w-full px-3 py-2 rounded-xl text-xs text-white/90 hover:bg-white/10 transition-colors"
        >
          <span>Nova aba</span>
          <kbd class="text-[10px] text-white/40">Ctrl+T</kbd>
        </button>

        <button 
          onclick={() => { store.closeAllOverlays(); store.createTab('yvira://newtab', true); }}
          class="mi flex items-center justify-between w-full px-3 py-2 rounded-xl text-xs text-cyan-400 hover:bg-white/10 transition-colors"
        >
          <span>Nova aba anônima</span>
          <kbd class="text-[10px] text-white/40">Ctrl+Shift+N</kbd>
        </button>

        <hr class="border-white/10 my-1" />

        <button 
          onclick={() => { store.closeAllOverlays(); store.navigateActiveTab('https://www.google.com'); }}
          class="mi flex items-center justify-between w-full px-3 py-2 rounded-xl text-xs text-white/90 hover:bg-white/10 transition-colors"
        >
          <span>Página Inicial</span>
        </button>

        <button 
          onclick={() => { store.closeAllOverlays(); store.showToast('Limpando dados de navegação...'); }}
          class="mi flex items-center justify-between w-full px-3 py-2 rounded-xl text-xs text-white/90 hover:bg-white/10 transition-colors"
        >
          <span>Limpar dados de navegação</span>
        </button>

        <hr class="border-white/10 my-1" />

        <button 
          onclick={() => bridge.close()}
          class="mi flex items-center justify-between w-full px-3 py-2 rounded-xl text-xs text-rose-400 hover:bg-rose-500/20 transition-colors"
        >
          <span>Fechar janela</span>
          <kbd class="text-[10px] text-rose-400/60">Alt+F4</kbd>
        </button>
      </div>
    </div>
  {/if}

  <!-- Container de Toasts Flutuantes -->
  <div class="fixed bottom-4 right-4 flex flex-col gap-2 pointer-events-none z-[9999]">
    {#each store.toasts as toast (toast.id)}
      <div class="px-4 py-2.5 rounded-xl bg-[#1e2130]/95 border border-white/15 text-xs font-medium text-white shadow-2xl transition-all duration-300">
        {toast.msg}
      </div>
    {/each}
  </div>
</div>
