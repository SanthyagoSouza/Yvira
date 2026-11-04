<script lang="ts">
  import { store } from '../lib/store.svelte'
  import { bridge } from '../lib/bridge'

  let inputVal = $state('')

  $effect(() => {
    const active = store.activeTab
    if (active) {
      inputVal = active.url === 'yvira://newtab' ? '' : active.url
    }
  })

  function handleSubmit(e: Event) {
    e.preventDefault()
    if (inputVal.trim()) {
      store.navigateActiveTab(inputVal)
    }
  }
</script>

<div class="toolbar">
  <!-- Botões de Navegação Histórico -->
  <button
    onclick={() => bridge.emit('nav_back', { tab: store.activeId })}
    class="tbtn"
    title="Voltar (Alt+←)"
    aria-label="Voltar"
  >
    <svg viewBox="0 0 24 24"><path d="M15 6l-6 6 6 6" /></svg>
  </button>

  <button
    onclick={() => bridge.emit('nav_forward', { tab: store.activeId })}
    class="tbtn"
    title="Avançar (Alt+→)"
    aria-label="Avançar"
  >
    <svg viewBox="0 0 24 24"><path d="M9 6l6 6-6 6" /></svg>
  </button>

  <button
    onclick={() => bridge.emit('reload_tab', { tab: store.activeId })}
    class="tbtn"
    title="Recarregar (F5)"
    aria-label="Recarregar"
  >
    <svg viewBox="0 0 24 24"><path d="M20 12a8 8 0 1 1-2.6-5.9M20 4v5h-5" /></svg>
  </button>

  <!-- Barra de Endereço Original com Cadeado e Estrela -->
  <label class="address">
    <button
      type="button"
      onclick={() => {
        store.menuOpen = false
        store.siteInfoOpen = !store.siteInfoOpen
        bridge.setOverlay(store.siteInfoOpen)
      }}
      class="tbtn sm"
      id="siteInfoBtn"
      title="Visualizar informações do site e permissões"
    >
      <svg viewBox="0 0 24 24"><rect x="5" y="11" width="14" height="9" rx="2" /><path d="M8 11V8a4 4 0 0 1 8 0v3" /></svg>
    </button>

    <input
      id="url"
      type="text"
      bind:value={inputVal}
      onkeydown={(e) => e.key === 'Enter' && handleSubmit(e)}
      placeholder="Pesquise ou digite um endereço"
      spellcheck="false"
      autocomplete="off"
    />

    <button
      type="button"
      class="tbtn sm"
      id="starBtn"
      title="Favoritar esta página (Ctrl+D)"
      onclick={() => store.showToast('Página adicionada aos favoritos!')}
    >
      <svg viewBox="0 0 24 24"><path d="M12 3.5l2.6 5.4 5.9.8-4.3 4.1 1 5.9L12 17l-5.2 2.7 1-5.9L3.5 9.7l5.9-.8z" /></svg>
    </button>
  </label>

  <!-- Botão Escudo de Privacidade com Badge de Bloqueios Reais -->
  <button
    class="tbtn relative"
    id="shieldBtn"
    title="Relatório de privacidade"
    onclick={() => {
      store.siteInfoOpen = false
      store.shieldOpen = !store.shieldOpen
      bridge.setOverlay(store.shieldOpen)
    }}
  >
    <svg viewBox="0 0 24 24"><path d="M12 3l7 3v5c0 5-3 8-7 10-4-2-7-5-7-10V6z" /></svg>
    {#if store.activeTab?.blocked}
      <b class="tbadge">{store.activeTab.blocked}</b>
    {/if}
  </button>

  <!-- Botão Menu (3 Pontinhos / Kebab) -->
  <button
    class="tbtn"
    id="kebab"
    title="Personalizar e controlar o Yvira"
    aria-haspopup="menu"
    aria-expanded={store.menuOpen}
    onclick={() => {
      store.siteInfoOpen = false
      store.shieldOpen = false
      store.menuOpen = !store.menuOpen
      bridge.setOverlay(store.menuOpen)
    }}
  >
    <svg viewBox="0 0 24 24"><circle cx="12" cy="5" r="1.5" /><circle cx="12" cy="12" r="1.5" /><circle cx="12" cy="19" r="1.5" /></svg>
  </button>
</div>

<style>
  .toolbar {
    display: flex;
    align-items: center;
    gap: 6px;
    height: 44px;
    padding: 0 10px;
    background: var(--glass-strong);
    border-bottom: 1px solid var(--stroke);
    position: relative;
    z-index: 8900;
  }

  .tbtn {
    width: 32px;
    height: 32px;
    border-radius: 9px;
    border: 0;
    background: transparent;
    color: var(--muted);
    cursor: pointer;
    display: grid;
    place-items: center;
    position: relative;
    transition: background 0.18s, color 0.18s, transform 0.12s;
    flex: none;
  }

  .tbtn svg {
    width: 17px;
    height: 17px;
    stroke: currentColor;
    fill: none;
    stroke-width: 1.8;
    stroke-linecap: round;
    stroke-linejoin: round;
  }

  .tbtn:hover {
    background: var(--hover);
    color: var(--text);
  }

  .tbtn:active {
    transform: scale(0.92);
  }

  .tbtn.sm {
    width: 26px;
    height: 26px;
    border-radius: 7px;
  }

  .tbtn.sm svg {
    width: 14px;
    height: 14px;
  }

  .tbadge {
    position: absolute;
    top: 4px;
    right: 4px;
    min-width: 14px;
    height: 14px;
    padding: 0 3px;
    border-radius: 99px;
    background: var(--accent);
    color: #fff;
    font-size: 9px;
    font-weight: 700;
    display: grid;
    place-items: center;
    border: 1px solid var(--glass-strong);
  }

  .address {
    flex: 1;
    display: flex;
    align-items: center;
    gap: 6px;
    height: 32px;
    padding: 0 6px 0 8px;
    border-radius: 10px;
    background: var(--field);
    border: 1px solid var(--stroke);
    box-shadow: inset 0 1px 2px rgba(0,0,0,0.25);
    transition: border-color 0.2s, box-shadow 0.2s;
  }

  .address:focus-within {
    border-color: var(--accent);
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--accent) 25%, transparent), inset 0 1px 2px rgba(0,0,0,0.3);
  }

  .address input {
    flex: 1;
    background: none;
    border: 0;
    outline: 0;
    color: var(--text);
    font: inherit;
    font-size: 13px;
    min-width: 0;
  }

  .address input::placeholder {
    color: var(--muted);
    opacity: 0.6;
  }
</style>
