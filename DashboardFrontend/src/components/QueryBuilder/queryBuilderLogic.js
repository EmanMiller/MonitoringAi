/**
 * Query Builder logic: Visual config ↔ Sumo Logic query string
 * All queries assume production (_sourceCategory=prod). Optional "Area of site" narrows to prod/{area}.
 */

/** Production source category — always applied; do not expose in Filters. */
export const PROD_SOURCE = 'prod';

/** Area of site options; selection becomes _sourceCategory=prod/{value} or prod when empty. */
export const SITE_AREAS = [
  { value: '', label: 'All areas' },
  { value: 'browse', label: 'Browse' },
  { value: 'checkout', label: 'Checkout' },
  { value: 'account', label: 'Account' },
  { value: 'api', label: 'API' },
  { value: 'search', label: 'Search' },
];

/** Filter fields only; _sourceCategory is fixed to production + site area. */
export const FIELDS = [
  { value: 'status', label: 'status' },
  { value: 'error', label: 'error' },
  { value: 'user_id', label: 'user_id' },
  { value: 'response_time', label: 'response_time' },
  { value: 'method', label: 'method' },
  { value: 'path', label: 'path' },
  { value: 'status_code', label: 'status_code' },
  { value: '_sourceHost', label: '_sourceHost' },
  { value: 'level', label: 'level' },
];

export const OPERATORS = [
  { value: '=', label: '=' },
  { value: '!=', label: '!=' },
  { value: '>', label: '>' },
  { value: '<', label: '<' },
  { value: '>=', label: '>=' },
  { value: '<=', label: '<=' },
  { value: 'contains', label: 'contains' },
  { value: 'matches', label: 'matches' },
];

export const TIME_PRESETS = [
  { value: '15m', label: 'Last 15 min' },
  { value: '1h', label: 'Last 1 hr' },
  { value: '24h', label: 'Last 24 hr' },
  { value: '7d', label: 'Last 7 days' },
  { value: '30d', label: 'Last 30 days' },
  { value: 'custom', label: 'Custom' },
];

export const LIMIT_OPTIONS = [10, 50, 100, 500, 1000];

export const AGG_FUNCTIONS = [
  { value: 'count', label: 'count', needsField: false },
  { value: 'sum', label: 'sum', needsField: true },
  { value: 'avg', label: 'avg', needsField: true },
  { value: 'min', label: 'min', needsField: true },
  { value: 'max', label: 'max', needsField: true },
];

export const GROUP_BY_FIELDS = [
  { value: '', label: '(none)' },
  ...FIELDS,
];

function escapeRegex(s) {
  return String(s).replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function escapeValue(v, op) {
  if (op === 'contains' || op === 'matches') return v;
  const n = parseFloat(v);
  if (!Number.isNaN(n)) return n;
  return `"${String(v).replace(/"/g, '\\"')}"`;
}

export function visualToQuery(visual) {
  const sourceCategory = visual.siteArea
    ? `${PROD_SOURCE}/${visual.siteArea}`
    : PROD_SOURCE;
  const parts = [`_sourceCategory=${sourceCategory}`];
  if (visual.filters?.length) {
    const whereClauses = visual.filters
      .filter((f) => f.field && f.operator && (f.value !== '' || ['=', '!='].includes(f.operator)))
      .map((f) => {
        const val = escapeValue(f.value, f.operator);
        if (f.operator === 'contains') return `${f.field} matches ".*${escapeRegex(f.value)}.*"`;
        if (f.operator === 'matches') return `${f.field} matches "${escapeRegex(f.value)}"`;
        if (['>', '<', '>=', '<='].includes(f.operator)) return `${f.field} ${f.operator} ${val}`;
        return `${f.field} ${f.operator} ${val}`;
      });
    if (whereClauses.length) {
      parts.push(`| where ${whereClauses.join(' and ')}`);
    }
  }
  if (visual.groupBy && visual.aggFunction) {
    const fn = AGG_FUNCTIONS.find((a) => a.value === visual.aggFunction);
    if (fn?.needsField && visual.aggField) {
      parts.push(`| ${visual.aggFunction}(${visual.aggField}) by ${visual.groupBy}`);
    } else if (visual.groupBy) {
      parts.push(`| count by ${visual.groupBy}`);
    }
  }
  if (visual.limit) {
    parts.push(`| limit ${visual.limit}`);
  }
  return parts.join(' ');
}

export function parseQueryToVisual(query) {
  const defaultVisual = {
    siteArea: '',
    filters: [{ field: '', operator: '=', value: '' }],
    groupBy: '',
    aggFunction: 'count',
    aggField: '',
    timeRange: '1h',
    timeStart: null,
    timeEnd: null,
    limit: 100,
  };
  if (!query || !query.trim()) return defaultVisual;
  const q = query.trim();
  let siteArea = '';
  const prodAreaMatch = q.match(/^_sourceCategory=prod\/(\w+)/i);
  if (prodAreaMatch) siteArea = prodAreaMatch[1].toLowerCase();
  const filters = [];
  let groupBy = '';
  let aggFunction = 'count';
  let aggField = '';
  let limit = 100;

  const limitMatch = q.match(/\|\s*limit\s+(\d+)/i);
  if (limitMatch) limit = parseInt(limitMatch[1], 10);

  const countByMatch = q.match(/\|\s*count\s+by\s+(\w+)/i);
  if (countByMatch) {
    groupBy = countByMatch[1];
    aggFunction = 'count';
  }

  const aggMatch = q.match(/\|\s*(sum|avg|min|max)\s*\(\s*(\w+)\s*\)\s+by\s+(\w+)/i);
  if (aggMatch) {
    aggFunction = aggMatch[1].toLowerCase();
    aggField = aggMatch[2];
    groupBy = aggMatch[3];
  }

  const whereMatch = q.match(/\|\s*where\s+(.+?)(?=\s*\|\s*(?:count|sum|avg|min|max|limit)|$)/is);
  if (whereMatch) {
    const clause = whereMatch[1].trim();
    const simple = clause.match(/(\w+)\s*(=|<|>|!=|>=|<=)\s*(.+)/);
    if (simple) {
      const val = simple[3].replace(/^["']|["']$/g, '');
      filters.push({ field: simple[1], operator: simple[2], value: val });
    }
    if (filters.length === 0) {
      filters.push({ field: '', operator: '=', value: '' });
    }
  } else {
    filters.push({ field: '', operator: '=', value: '' });
  }

  return {
    siteArea,
    filters,
    groupBy,
    aggFunction,
    aggField,
    timeRange: '1h',
    timeStart: null,
    timeEnd: null,
    limit,
  };
}

export const TEMPLATES = [
  { id: 'errors', name: 'Find Errors', description: 'Find error-level logs across sources', category: 'Errors', icon: '⚠️' },
  { id: 'api-perf', name: 'Track API Performance', description: 'Monitor response times and latency', category: 'Performance', icon: '📊' },
  { id: 'logins', name: 'Monitor User Logins', description: 'Track authentication events', category: 'Security', icon: '🔐' },
  { id: 'slow-queries', name: 'Slow Query Detection', description: 'Identify slow database queries', category: 'Performance', icon: '🐢' },
  { id: 'security', name: 'Security Events', description: 'Audit security-related log entries', category: 'Security', icon: '🛡️' },
  { id: 'status-codes', name: 'HTTP Status Codes', description: 'Count requests by status code', category: 'API', icon: '🌐' },
];

export const TEMPLATE_QUERIES = {
  errors: '_sourceCategory=prod | where level="error" or status="error" or status_code>=500 | count by status | limit 100',
  'api-perf': '_sourceCategory=prod | where response_time > 0 | avg(response_time) by _sourceHost | limit 100',
  logins: '_sourceCategory=prod/account | where method="POST" and path matches ".*login.*" | count by user_id | limit 100',
  'slow-queries': '_sourceCategory=prod | where response_time > 1000 | count by _sourceHost | limit 100',
  security: '_sourceCategory=prod | where status matches ".*denied|failed|unauthorized.*" | count by status | limit 100',
  'status-codes': '_sourceCategory=prod | count by status_code | limit 100',
};
