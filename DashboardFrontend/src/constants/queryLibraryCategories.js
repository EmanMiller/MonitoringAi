/** Predefined categories with color codes for badges */
export const PREDEFINED_CATEGORIES = [
  { name: 'Browse Product', color: '#3b82f6' },
  { name: 'Browse Path', color: '#8b5cf6' },
  { name: 'Account', color: '#10b981' },
  { name: 'Checkout', color: '#f59e0b' },
  { name: 'Gift Registry', color: '#ec4899' },
  { name: 'API', color: '#ef4444' },
];

export const CATEGORY_NAMES = PREDEFINED_CATEGORIES.map((c) => c.name);

export function getCategoryColor(categoryName) {
  const found = PREDEFINED_CATEGORIES.find((c) => c.name === categoryName);
  return found ? found.color : '#64748b';
}

export const KEY_MAX_LENGTH = 200;
export const SUGGESTED_TAGS = ['performance', 'security', 'user-behavior', 'errors', 'latency', 'product', 'api'];
