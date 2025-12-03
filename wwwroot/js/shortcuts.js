window.erpShortcuts = (function () {
  let searchHandler = null;
  let globalHandler = null;
  let targetId = null;
  let dotNetHelper = null;

  function onKeyDown(e) {
    const tag = (document.activeElement && document.activeElement.tagName || '').toLowerCase();
    const isTyping = tag === 'input' || tag === 'textarea' || document.activeElement?.isContentEditable;
    // Ctrl/Cmd + K always focuses search
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
      const el = document.getElementById(targetId);
      if (el) { el.focus(); e.preventDefault(); e.stopPropagation(); }
      return;
    }
    // '/' focuses search if not already typing in an input
    if (!isTyping && e.key === '/') {
      const el = document.getElementById(targetId);
      if (el) { el.focus(); e.preventDefault(); e.stopPropagation(); }
    }
  }

  function onGlobalKeyDown(e) {
    if (!dotNetHelper) return;

    const tag = (document.activeElement && document.activeElement.tagName || '').toLowerCase();
    const isTyping = tag === 'input' || tag === 'textarea' || document.activeElement?.isContentEditable;

    // Skip if typing in input
    if (isTyping) return;

    // Global shortcuts (without modifier keys, only when not typing)
    const shortcuts = {
      'g': { next: { 'd': '/dashboard', 'h': '/', 's': '/sales', 'c': '/customers', 'p': '/products', 'k': '/kanban', 'a': '/settings' } }
    };

    // Handle 'g' prefix for navigation (g+d = dashboard, g+h = home, etc.)
    if (e.key.toLowerCase() === 'g' && !e.ctrlKey && !e.metaKey && !e.altKey) {
      e.preventDefault();
      window._erpWaitingForSecondKey = true;
      window._erpSecondKeyTimeout = setTimeout(() => {
        window._erpWaitingForSecondKey = false;
      }, 1000);
      return;
    }

    if (window._erpWaitingForSecondKey) {
      window._erpWaitingForSecondKey = false;
      clearTimeout(window._erpSecondKeyTimeout);
      
      const route = shortcuts['g']?.next?.[e.key.toLowerCase()];
      if (route) {
        e.preventDefault();
        dotNetHelper.invokeMethodAsync('NavigateFromShortcut', route);
      }
      return;
    }

    // Ctrl/Cmd shortcuts
    if (e.ctrlKey || e.metaKey) {
      switch (e.key.toLowerCase()) {
        case 'b':
          // Toggle sidebar
          e.preventDefault();
          dotNetHelper.invokeMethodAsync('ToggleSidebarFromShortcut');
          break;
      }
    }

    // Escape to close dialogs/modals (MudBlazor handles this, but we can add custom behavior)
  }

  return {
    registerSearchHotkeys: function (id) {
      if (searchHandler) window.removeEventListener('keydown', searchHandler, true);
      targetId = id;
      searchHandler = onKeyDown;
      window.addEventListener('keydown', searchHandler, true);
    },
    unregisterSearchHotkeys: function () {
      if (searchHandler) window.removeEventListener('keydown', searchHandler, true);
      searchHandler = null; targetId = null;
    },
    initialize: function (helper) {
      dotNetHelper = helper;
      globalHandler = onGlobalKeyDown;
      window.addEventListener('keydown', globalHandler, true);
      return {
        dispose: function () {
          if (globalHandler) {
            window.removeEventListener('keydown', globalHandler, true);
            globalHandler = null;
            dotNetHelper = null;
          }
        }
      };
    }
  };
})();
