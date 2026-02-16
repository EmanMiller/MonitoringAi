import DOMPurify from 'dompurify';

/**
 * Sanitize HTML for safe rendering (e.g. user-generated or API content that may contain links).
 * Use for any content that might be rendered with dangerouslySetInnerHTML.
 */
export function sanitizeHtml(html) {
  if (typeof html !== 'string') return '';
  return DOMPurify.sanitize(html, {
    ALLOWED_TAGS: ['a', 'b', 'strong', 'i', 'em', 'br', 'p', 'span'],
    ALLOWED_ATTR: ['href', 'target', 'rel'],
    ADD_ATTR: ['rel'],
  });
}

/** Strip script/iframe/object/embed from string (for chat messages before send). */
export function stripDangerousTags(text) {
  if (typeof text !== 'string') return '';
  return text
    .replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi, '')
    .replace(/<iframe\b[^>]*>[\s\S]*?<\/iframe>/gi, '')
    .replace(/<object\b[^>]*>[\s\S]*?<\/object>/gi, '')
    .replace(/<embed\b[^>]*>/gi, '');
}

export const CHAT_MESSAGE_MAX_LENGTH = 2000;
export const SEARCH_QUERY_MAX_LENGTH = 100;
