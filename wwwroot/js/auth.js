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
  }
};
