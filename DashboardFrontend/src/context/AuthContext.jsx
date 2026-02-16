import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';

const AuthContext = createContext(null);

const API_URL = import.meta.env.VITE_API_URL || 'https://localhost:7290';
const ACCESS_TOKEN_EXPIRY_MINUTES = 60;
const WARNING_BEFORE_EXPIRY_MINUTES = 5;

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const [csrfToken, setCsrfToken] = useState(null);
  const [expiresAt, setExpiresAt] = useState(null);
  const [showExpiryWarning, setShowExpiryWarning] = useState(false);
  const [logoutReason, setLogoutReason] = useState(null);

  const fetchWithCredentials = useCallback((url, options = {}) => {
    const headers = { ...options.headers, 'Content-Type': 'application/json' };
    if (csrfToken && ['POST', 'PUT', 'PATCH', 'DELETE'].includes(options.method || 'GET'))
      headers['X-CSRF-TOKEN'] = csrfToken;
    return fetch(url, { ...options, credentials: 'include', headers });
  }, [csrfToken]);

  const fetchCsrf = useCallback(async () => {
    const res = await fetch(`${API_URL}/api/auth/csrf`, { credentials: 'include' });
    if (res.ok) {
      const { token } = await res.json();
      setCsrfToken(token);
      return token;
    }
    return null;
  }, []);

  const fetchMe = useCallback(async () => {
    const res = await fetch(`${API_URL}/api/auth/me`, { credentials: 'include' });
    if (res.ok) {
      const data = await res.json();
      setUser({ id: data.id, userName: data.userName, role: data.role });
      return data;
    }
    setUser(null);
    return null;
  }, []);

  const refreshToken = useCallback(async () => {
    const res = await fetch(`${API_URL}/api/auth/refresh`, { method: 'POST', credentials: 'include' });
    if (res.ok) {
      const data = await res.json();
      setExpiresAt(Date.now() + data.expiresInMinutes * 60 * 1000);
      setShowExpiryWarning(false);
      await fetchCsrf();
      return true;
    }
    setUser(null);
    setExpiresAt(null);
    setShowExpiryWarning(false);
    setLogoutReason('Session expired. Please log in again.');
    return false;
  }, [fetchCsrf]);

  const login = useCallback(async (userName, password) => {
    const token = await fetchCsrf();
    if (!token) return { ok: false, error: 'Could not get CSRF token.' };
    const res = await fetch(`${API_URL}/api/auth/login`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
      body: JSON.stringify({ userName, password }),
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) return { ok: false, error: data.error || 'Login failed.' };
    setUser({ id: data.id, userName: data.userName, role: data.role });
    setExpiresAt(Date.now() + (data.expiresInMinutes || 60) * 60 * 1000);
    setShowExpiryWarning(false);
    await fetchCsrf();
    return { ok: true };
  }, [fetchCsrf]);

  const logout = useCallback(async () => {
    const token = await fetchCsrf();
    if (token) {
      await fetch(`${API_URL}/api/auth/logout`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
        body: JSON.stringify({}),
      });
    }
    setUser(null);
    setExpiresAt(null);
    setShowExpiryWarning(false);
    setCsrfToken(null);
    setLogoutReason(null);
  }, [fetchCsrf]);

  useEffect(() => {
    let warningTimer;
    let expiryTimer;
    const scheduleExpiryUI = (expAt) => {
      if (!expAt) return;
      const warnAt = expAt - WARNING_BEFORE_EXPIRY_MINUTES * 60 * 1000;
      if (warnAt > Date.now()) {
        warningTimer = setTimeout(() => setShowExpiryWarning(true), warnAt - Date.now());
      } else {
        setShowExpiryWarning(true);
      }
      if (expAt > Date.now()) {
        expiryTimer = setTimeout(async () => {
          const refreshed = await refreshToken();
          if (!refreshed) setLogoutReason('Session expired. Please log in again.');
        }, expAt - Date.now());
      }
    };
    if (expiresAt) scheduleExpiryUI(expiresAt);
    return () => {
      clearTimeout(warningTimer);
      clearTimeout(expiryTimer);
    };
  }, [expiresAt, refreshToken]);

  useEffect(() => {
    (async () => {
      setLoading(true);
      await fetchCsrf();
      const me = await fetchMe();
      if (me) setExpiresAt(Date.now() + ACCESS_TOKEN_EXPIRY_MINUTES * 60 * 1000);
      setLoading(false);
    })();
  }, [fetchCsrf, fetchMe]);

  const value = {
    user,
    loading,
    login,
    logout,
    refreshToken,
    fetchWithCredentials,
    API_URL,
    showExpiryWarning,
    setShowExpiryWarning,
    logoutReason,
    setLogoutReason,
    fetchMe,
    fetchCsrf,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
