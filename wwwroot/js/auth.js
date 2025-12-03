window.erpAuth = {
  login: async function (payload) {
    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(payload)
      });
      const text = await res.text();
      let data = null;
      try {
        data = JSON.parse(text);
      } catch (err) {
        console.error('Failed to parse JSON:', err);
        // Se não for JSON, text já tem a mensagem
      }
      console.log('auth.js - parsed data:', data);
      return { ok: res.ok, status: res.status, text, data };
    } catch (e) {
      return { ok: false, status: 0, text: e?.message || 'Network error', data: null };
    }
  },
  fetchJson: async function (url, payload) {
    try {
      const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(payload)
      });
      const text = await res.text();
      let data = null;
      try { data = JSON.parse(text); } catch { /* noop */ }
      return JSON.stringify({ ok: res.ok, status: res.status, text, data });
    } catch (e) {
      return JSON.stringify({ ok: false, status: 0, text: e?.message || 'Network error', data: null });
    }
  },
  verify2fa: async function (payload) {
    try {
      const res = await fetch('/api/auth/verify-2fa', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(payload)
      });
      const text = await res.text();
      let data = null;
      try {
        data = JSON.parse(text);
      } catch (err) {
        console.error('Failed to parse JSON:', err);
      }
      console.log('auth.js - 2FA verify result:', data);
      const result = { ok: res.ok, status: res.status, text, data };
      return JSON.stringify(result); // Retorna como string JSON para C# deserializar
    } catch (e) {
      const result = { ok: false, status: 0, text: e?.message || 'Network error', data: null };
      return JSON.stringify(result);
    }
  },
  logout: async function () {
    try {
      const res = await fetch('/api/auth/logout', {
        method: 'POST',
        credentials: 'include'
      });
      return res.ok;
    } catch {
      return false;
    }
  },
  monitorActivity: function (dotNetHelper, timeoutMinutes) {
    let timer;
    // Throttle events to avoid excessive processing, though setTimeout is cheap
    const resetTimer = () => {
      clearTimeout(timer);
      if (timeoutMinutes > 0) {
          timer = setTimeout(() => {
            dotNetHelper.invokeMethodAsync('LogoutFromActivity');
          }, timeoutMinutes * 60 * 1000);
      }
    };
    
    const events = ['mousemove', 'keydown', 'click', 'scroll', 'touchstart'];
    events.forEach(e => window.addEventListener(e, resetTimer));
    
    resetTimer();
    
    return {
        updateTimeout: (newMinutes) => {
            timeoutMinutes = newMinutes;
            resetTimer();
        },
        dispose: () => {
            events.forEach(e => window.removeEventListener(e, resetTimer));
            clearTimeout(timer);
        }
    };
  }
  ,
  getSystemPrefersDark: function () {
    try {
      return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    } catch (e) {
      return false;
    }
  },
  registerThemeListener: function (dotNetHelper) {
    try {
      const mq = window.matchMedia('(prefers-color-scheme: dark)');
      const handler = (e) => {
        // e.matches is true when dark mode is preferred
        dotNetHelper.invokeMethodAsync('OnSystemThemeChanged', e.matches);
      };
      if (mq.addEventListener) mq.addEventListener('change', handler);
      else mq.addListener(handler);

      return {
        dispose: () => {
          try {
            if (mq.removeEventListener) mq.removeEventListener('change', handler);
            else mq.removeListener(handler);
          } catch (e) { /* noop */ }
        }
      };
    } catch (e) {
      return { dispose: () => { } };
    }
  }
};
