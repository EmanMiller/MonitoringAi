/**
 * Dashboard name validation and sanitization utilities.
 */

const DASHBOARD_NAME_REGEX = /^[A-Z][a-zA-Z0-9\s_-]{2,50}$/;
const MIN_LENGTH = 3;
const MAX_LENGTH = 50;

/**
 * Sanitize input: strip special characters except spaces, hyphens, underscores.
 * Prevents SQL injection: escape quotes, semicolons.
 * @param {string} value - Raw input
 * @returns {string} Sanitized value
 */
export function sanitizeDashboardName(value) {
  if (typeof value !== 'string') return '';
  let s = value
    .trim()
    .replace(/['";\\]/g, '')  // Remove quotes, semicolons, backslashes
    .replace(/[^\w\s\-]/g, ''); // Keep only letters, digits, spaces, hyphens, underscores
  return s;
}

/**
 * Validate dashboard name against rules.
 * @param {string} value - Input to validate
 * @returns {{ valid: boolean, error?: string }}
 */
export function validateDashboardName(value) {
  const trimmed = value?.trim() ?? '';
  if (trimmed.length === 0) {
    return { valid: false, error: 'Dashboard name is required' };
  }
  if (trimmed.length < MIN_LENGTH) {
    return { valid: false, error: 'Dashboard name must be at least 3 characters' };
  }
  if (trimmed.length > MAX_LENGTH) {
    return { valid: false, error: 'Dashboard name must not exceed 50 characters' };
  }
  const first = trimmed.charAt(0);
  if (!/^[A-Z]$/.test(first)) {
    return { valid: false, error: 'Dashboard name must start with a capital letter' };
  }
  if (!DASHBOARD_NAME_REGEX.test(trimmed)) {
    return {
      valid: false,
      error: 'Use only letters, numbers, spaces, hyphens, and underscores (3–50 characters)',
    };
  }
  return { valid: true };
}
