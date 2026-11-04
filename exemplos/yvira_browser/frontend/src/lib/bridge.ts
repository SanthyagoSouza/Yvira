// Tipos e ponte IPC com o runtime Yvira

export interface TabItem {
  id: number
  title: string
  url: string
  fav?: string
  priv?: boolean
  blocked?: number
  loading?: boolean
}

declare global {
  interface Window {
    yvira?: {
      emit: (eventName: string, payload: any) => void
      minimizar: () => void
      maximizar: () => void
      fechar: () => void
      arrastar: () => void
    }
    receberAtualizacaoUrl?: (dados: any) => void
    solicitacaoPermissao?: (dados: any) => void
    rastreadorBloqueadoNativo?: (dados: any) => void
    solicitarNovaAbaExterna?: (dados: any) => void
    downloadIniciado?: (dados: any) => void
    alertaSeguranca?: (dados: any) => void
  }
}

export const bridge = {
  emit(action: string, payload: any = {}) {
    try {
      if (window.yvira && typeof window.yvira.emit === 'function') {
        window.yvira.emit(action, payload)
      } else {
        console.log(`[Bridge Dev] Emit '${action}':`, payload)
      }
    } catch (e) {
      console.warn('[Bridge Error]', e)
    }
  },

  navigate(url: string, tabId: number, isPrivate: boolean = false) {
    this.emit('navigate', { url, tab: tabId, id: tabId, private: isPrivate })
  },

  switchTab(tabId: number, url: string, isPrivate: boolean = false) {
    this.emit('tab_switched', { tab: tabId, id: tabId, url, private: isPrivate })
  },

  closeTab(tabId: number) {
    this.emit('tab_closed', { tab: tabId, id: tabId })
  },

  setHeaderHeight(height: number) {
    this.emit('set_header_height', { height })
  },

  setOverlay(open: boolean) {
    this.emit('set_overlay', { open })
  },

  minimize() {
    window.yvira?.minimizar()
  },

  maximize() {
    window.yvira?.maximizar()
  },

  close() {
    window.yvira?.fechar()
  },

  drag() {
    window.yvira?.arrastar()
  }
}
