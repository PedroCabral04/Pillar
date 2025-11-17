window.erpShortcuts = (function () {
  let handler = null;
  let targetId = null;
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
  return {
    registerSearchHotkeys: function (id) {
      if (handler) window.removeEventListener('keydown', handler, true);
      targetId = id;
      handler = onKeyDown;
      window.addEventListener('keydown', handler, true);
    },
    unregisterSearchHotkeys: function () {
      if (handler) window.removeEventListener('keydown', handler, true);
      handler = null; targetId = null;
    }
  };
})();
