<script lang="ts">
  import { store } from '../lib/store.svelte'
  import { bridge } from '../lib/bridge'

  let { onNewTab } = $props<{ onNewTab: () => void }>()
</script>

<div 
  class="titlebar select-none"
  onmousedown={(e) => {
    // Permite arrastar janela apenas em áreas neutras da barra
    const target = e.target as HTMLElement
    if (!target.closest('button') && !target.closest('.tab') && !target.closest('.controls')) {
      bridge.drag()
    }
  }}
  role="region"
  aria-label="Barra de Título"
>
  <!-- Ícone do Aplicativo Yvira -->
  <div class="app-icon" title="Yvira Engine">
    <img src="yvira-logo.svg" alt="Yvira" class="w-full h-full object-contain" />
  </div>

  <!-- Lista de Abas Original Estilizada -->
  <div class="tabs" role="tablist">
    {#each store.tabs as tab (tab.id)}
      {@const isActive = tab.id === store.activeId}
      <div
        role="tab"
        tabindex="0"
        aria-selected={isActive}
        onclick={() => store.selectTab(tab.id)}
        onkeydown={(e) => e.key === 'Enter' && store.selectTab(tab.id)}
        class="tab {isActive ? 'active' : ''}"
        title={tab.title}
      >
        <span class="tfav">{tab.priv ? '◐' : (tab.title ? tab.title[0]?.toUpperCase() : 'Y')}</span>
        <span class="label">{tab.title || 'Nova Guia'}</span>
        <button
          onclick={(e) => {
            e.stopPropagation()
            store.closeTab(tab.id)
          }}
          class="x"
          title="Fechar aba"
          aria-label="Fechar aba"
        >
          ✕
        </button>
      </div>
    {/each}
  </div>

  <!-- Botão Nova Aba Original (+) com rotação fluida -->
  <button
    onclick={onNewTab}
    class="new-tab"
    title="Nova aba (Ctrl+T)"
    aria-label="Nova aba"
  >
    <svg class="i" viewBox="0 0 24 24">
      <path d="M12 5v14M5 12h14" />
    </svg>
  </button>

  {#if store.isPrivate}
    <span class="pill">● Anônimo</span>
  {/if}

  <!-- Controles da Janela com Glow Original (Min, Max, Close) -->
  <div class="controls" role="group" aria-label="Controles da janela">
    <button
      onclick={() => bridge.minimize()}
      class="wbtn min"
      title="Minimizar"
      aria-label="Minimizar"
    >
      <svg viewBox="0 0 24 24"><path d="M6 12h12" /></svg>
    </button>

    <button
      onclick={() => bridge.maximize()}
      class="wbtn max"
      title="Maximizar ou restaurar"
      aria-label="Maximizar ou restaurar"
    >
      <svg class="ic-max" viewBox="0 0 24 24">
        <rect x="6" y="6" width="12" height="12" rx="3" />
      </svg>
    </button>

    <button
      onclick={() => bridge.close()}
      class="wbtn close"
      title="Fechar"
      aria-label="Fechar"
    >
      <svg viewBox="0 0 24 24"><path d="M6 6l12 12M18 6L6 18" /></svg>
    </button>
  </div>
</div>

<style>
  .titlebar {
    display: flex;
    align-items: center;
    gap: 10px;
    height: 46px;
    padding: 0 8px 0 12px;
    background: var(--glass-strong);
    border-bottom: 1px solid var(--stroke);
    user-select: none;
    cursor: default;
    flex-shrink: 0;
    z-index: 9000;
  }

  .app-icon {
    width: 24px;
    height: 24px;
    border-radius: 7px;
    flex: none;
    display: flex;
    align-items: center;
    justify-content: center;
    background: transparent;
  }

  .pill {
    flex: none;
    font-size: 11.5px;
    padding: 4px 10px;
    border-radius: 99px;
    background: var(--hover);
    border: 1px solid var(--stroke-strong);
    color: var(--muted);
  }

  .tabs {
    display: flex;
    align-items: flex-end;
    gap: 4px;
    flex: 1;
    min-width: 0;
    height: 100%;
    padding-top: 9px;
    overflow: hidden;
  }

  .tab {
    position: relative;
    display: flex;
    align-items: center;
    gap: 8px;
    max-width: 210px;
    min-width: 44px;
    flex: 0 1 210px;
    height: 100%;
    padding: 0 8px 0 10px;
    border-radius: 10px 10px 0 0;
    font-size: 12.5px;
    color: var(--muted);
    cursor: pointer;
    transition: background 0.2s, color 0.2s, transform 0.15s;
    white-space: nowrap;
  }

  .tab:hover {
    background: var(--hover);
    color: var(--text);
  }

  .tab.active {
    background: var(--glass-strong);
    color: var(--text);
    box-shadow: inset 0 1px 0 var(--stroke-strong);
  }

  .tab.active::after {
    content: "";
    position: absolute;
    left: 12px;
    right: 12px;
    top: 0;
    height: 2px;
    border-radius: 0 0 3px 3px;
    background: linear-gradient(90deg, var(--accent), var(--accent-2));
  }

  .tfav {
    width: 16px;
    height: 16px;
    border-radius: 5px;
    display: grid;
    place-items: center;
    font-size: 10px;
    font-weight: 700;
    background: linear-gradient(135deg, var(--accent), var(--accent-2));
    color: #fff;
    flex: none;
  }

  .tab .label {
    overflow: hidden;
    text-overflow: ellipsis;
    flex: 1;
  }

  .tab .x {
    width: 18px;
    height: 18px;
    border-radius: 6px;
    display: grid;
    place-items: center;
    flex: none;
    opacity: 0.6;
    transition: 0.15s;
    font-size: 11px;
    background: none;
    border: none;
    color: inherit;
    cursor: pointer;
  }

  .tab .x:hover {
    background: var(--stroke-strong);
    opacity: 1;
  }

  .new-tab {
    width: 28px;
    height: 28px;
    border-radius: 8px;
    border: 0;
    background: none;
    color: var(--muted);
    cursor: pointer;
    display: grid;
    place-items: center;
    transition: transform 0.25s cubic-bezier(0.22, 1, 0.36, 1), background 0.2s, color 0.2s;
    flex: none;
    align-self: center;
    margin-top: 6px;
  }

  .new-tab:hover {
    background: var(--hover);
    color: var(--text);
    transform: rotate(90deg) scale(1.1);
  }

  .new-tab:active {
    transform: rotate(90deg) scale(0.92);
  }

  svg.i {
    width: 16px;
    height: 16px;
    stroke: currentColor;
    fill: none;
    stroke-width: 1.8;
    stroke-linecap: round;
    stroke-linejoin: round;
    flex: none;
  }

  /* ---------- Botões da janela (Minimizar, Maximizar, Fechar) com Efeitos Glow ---------- */
  .controls {
    display: flex;
    gap: 2px;
    padding: 3px;
    border-radius: 12px;
    flex: none;
    background: var(--field);
    border: 1px solid var(--stroke);
    box-shadow: inset 0 1px 2px rgba(0,0,0,0.25);
  }

  .wbtn {
    --c: var(--muted);
    position: relative;
    width: 38px;
    height: 28px;
    border: 0;
    border-radius: 9px;
    background: transparent;
    color: var(--c);
    cursor: pointer;
    display: grid;
    place-items: center;
    overflow: hidden;
    transition: background 0.2s, color 0.2s, box-shadow 0.25s, transform 0.15s cubic-bezier(0.22, 1, 0.36, 1);
  }

  .wbtn svg {
    width: 14px;
    height: 14px;
    stroke: currentColor;
    fill: none;
    stroke-width: 1.8;
    stroke-linecap: round;
    stroke-linejoin: round;
    transition: transform 0.3s cubic-bezier(0.22, 1, 0.36, 1);
  }

  .wbtn:hover {
    transform: translateY(-1px);
  }

  .wbtn:active {
    transform: scale(0.9);
  }

  .wbtn.min:hover {
    --c: #1a1203;
    background: linear-gradient(135deg, #fde68a, var(--min));
    box-shadow: 0 0 16px -2px rgba(251, 191, 36, 0.8);
  }

  .wbtn.max:hover {
    --c: #03150e;
    background: linear-gradient(135deg, #a7f3d0, var(--max));
    box-shadow: 0 0 16px -2px rgba(52, 211, 153, 0.8);
  }

  .wbtn.close:hover {
    --c: #fff;
    background: linear-gradient(135deg, #ff8fa3, var(--close));
    box-shadow: 0 0 18px -2px rgba(255, 77, 109, 0.9);
  }

  .wbtn.min:hover svg {
    transform: translateY(2px);
  }

  .wbtn.max:hover svg {
    transform: scale(1.15);
  }

  .wbtn.close:hover svg {
    transform: rotate(90deg);
  }
</style>
