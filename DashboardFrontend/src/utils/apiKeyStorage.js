const STORAGE_KEY = 'gemini_api_key_v1';
const OBFUSCATE_PREFIX = 'gk_';

/**
 * Simple obfuscation so the key isn't stored as plain text in localStorage.
 * Not cryptographically secure—use env var in production if possible.
 */
function obfuscate(value) {
  if (!value) return '';
  try {
    return OBFUSCATE_PREFIX + btoa(encodeURIComponent(value));
  } catch {
    return '';
  }
}

function deobfuscate(value) {
  if (!value || !value.startsWith(OBFUSCATE_PREFIX)) return '';
  try {
    return decodeURIComponent(atob(value.slice(OBFUSCATE_PREFIX.length)));
  } catch {
    return '';
  }
}

/**
 * Get stored Gemini API key (from localStorage or env).
 * Vite exposes env via import.meta.env.VITE_*.
 */
export function getStoredApiKey() {
  try {
    const fromEnv = typeof import.meta !== 'undefined' && import.meta.env && import.meta.env.VITE_GEMINI_API_KEY;
    if (fromEnv && typeof fromEnv === 'string' && fromEnv.trim()) {
      return fromEnv.trim();
    }
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? deobfuscate(raw) : '';
  } catch {
    return '';
  }
}

/**
 * Save Gemini API key to localStorage (obfuscated).
 */
export function setStoredApiKey(apiKey) {
  try {
    if (!apiKey || !apiKey.trim()) {
      localStorage.removeItem(STORAGE_KEY);
      return;
    }
    localStorage.setItem(STORAGE_KEY, obfuscate(apiKey.trim()));
  } catch {
    // ignore
  }
}

/**
 * Clear stored API key.
 */
export function clearStoredApiKey() {
  try {
    localStorage.removeItem(STORAGE_KEY);
  } catch {
    // ignore
  }
}
