import { bridge, type TabItem } from './bridge'

class BrowserStore {
  tabs = $state<TabItem[]>([
    { id: 1, title: 'Nova Guia', url: 'yvira://newtab', priv: false, blocked: 0 }
  ])
  activeId = $state<number>(1)
  blockedTotal = $state<number>(0)
  theme = $state<'dark' | 'light'>('dark')
  menuOpen = $state<boolean>(false)
  shieldOpen = $state<boolean>(false)
  siteInfoOpen = $state<boolean>(false)
  toasts = $state<Array<{ id: number; msg: string }>>([])

  get activeTab(): TabItem | undefined {
    return this.tabs.find(t => t.id === this.activeId) || this.tabs[0]
  }

  get isPrivate(): boolean {
    return !!this.activeTab?.priv
  }

  showToast(msg: string) {
    const id = Date.now() + Math.random()
    this.toasts.push({ id, msg })
    setTimeout(() => {
      this.toasts = this.toasts.filter(t => t.id !== id)
    }, 3800)
  }

  createTab(url: string = 'yvira://newtab', isPrivate: boolean = false) {
    const nextId = (this.tabs.reduce((max, t) => Math.max(max, t.id), 0) || 0) + 1
    const newTab: TabItem = {
      id: nextId,
      title: url === 'yvira://newtab' ? 'Nova Guia' : url,
      url,
      priv: isPrivate,
      blocked: 0
    }
    this.tabs.push(newTab)
    this.selectTab(nextId)
    bridge.navigate(url, nextId, isPrivate)
    return newTab
  }

  selectTab(id: number) {
    this.activeId = id
    const t = this.activeTab
    if (t) {
      bridge.switchTab(t.id, t.url, !!t.priv)
    }
  }

  closeTab(id: number) {
    if (this.tabs.length <= 1) {
      const single = this.tabs[0]
      single.url = 'yvira://newtab'
      single.title = 'Nova Guia'
      bridge.navigate('yvira://newtab', single.id, !!single.priv)
      return
    }

    const idx = this.tabs.findIndex(t => t.id === id)
    this.tabs = this.tabs.filter(t => t.id !== id)
    bridge.closeTab(id)

    if (id === this.activeId) {
      const nextTab = this.tabs[Math.max(0, idx - 1)]
      if (nextTab) {
        this.selectTab(nextTab.id)
      }
    }
  }

  navigateActiveTab(rawUrl: string) {
    const t = this.activeTab
    if (!t) return

    let finalUrl = rawUrl.trim()
    if (!finalUrl) finalUrl = 'yvira://newtab'
    else if (!/^https?:\/\//i.test(finalUrl) && !/^yvira:\/\//i.test(finalUrl)) {
      if (finalUrl.includes('.') && !finalUrl.includes(' ')) {
        finalUrl = 'https://' + finalUrl
      } else {
        finalUrl = 'https://www.google.com/search?q=' + encodeURIComponent(finalUrl)
      }
    }

    t.url = finalUrl
    t.title = finalUrl.replace(/^https?:\/\//, '')
    bridge.navigate(finalUrl, t.id, !!t.priv)
  }

  updateUrlFromNative(url: string, title: string, tabId: number) {
    const t = this.tabs.find(x => x.id === tabId) || this.activeTab
    if (t) {
      if (url && t.url !== url) t.url = url
      if (title) t.title = title
    }
  }

  incrementTrackerBlock(tabId?: number) {
    this.blockedTotal++
    const target = this.tabs.find(t => t.id === tabId) || this.activeTab
    if (target) {
      target.blocked = (target.blocked || 0) + 1
    }
  }

  closeAllOverlays() {
    this.menuOpen = false
    this.shieldOpen = false
    this.siteInfoOpen = false
    bridge.setOverlay(false)
  }
}

export const store = new BrowserStore()
