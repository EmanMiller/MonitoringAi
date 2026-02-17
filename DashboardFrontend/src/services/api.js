import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5160';

let csrfTokenPromise = null;
async function getCsrfToken() {
  if (!csrfTokenPromise) csrfTokenPromise = fetch(`${API_URL}/api/auth/csrf`, { credentials: 'include' }).then((r) => r.json()).then((d) => d?.token).catch(() => null);
  const token = await csrfTokenPromise;
  csrfTokenPromise = null;
  return token;
}

const api = axios.create({
  baseURL: API_URL,
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use(
  async (config) => {
    if (['post', 'put', 'patch', 'delete'].includes(config.method?.toLowerCase())) {
      const token = await getCsrfToken();
      if (token) config.headers['X-CSRF-TOKEN'] = token;
    }
    return config;
  },
  (err) => Promise.reject(err)
);

api.interceptors.response.use(
  (r) => r,
  (err) => {
    if (err.response?.status === 401) {
      const path = window.location.pathname || '';
      if (!path.startsWith('/login')) window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);

export const createDashboard = async (data) => {
  const { data: res } = await api.post('/dashboard', data);
  return res;
};

export const createDashboardFromWizard = async (wizardData) => {
  const { data } = await api.post('/dashboard/wizard', wizardData);
  return data;
};

export const getLogMappings = async () => {
  const { data } = await api.get('/api/LogMappings');
  return data;
};

export const createLogMapping = async (body) => {
  const { data } = await api.post('/api/LogMappings', body);
  return data;
};

export const updateLogMapping = async (id, body) => {
  const { data } = await api.put(`/api/LogMappings/${id}`, body);
  return data;
};

export const deleteLogMapping = async (id) => {
  await api.delete(`/api/LogMappings/${id}`);
};

export const searchSavedQueries = async (q) => {
  const { data } = await api.get('/api/SavedQueries/search', { params: { q: q || undefined } });
  return data;
};

export const getQueryLibrary = async (category) => {
  const { data } = await api.get('/api/QueryLibrary', { params: category ? { category } : {} });
  return data;
};

export const searchQueryLibrary = async (q) => {
  const { data } = await api.get('/api/QueryLibrary/search', { params: { q: q || undefined } });
  return data;
};

export const getAllQueryLibrary = async () => {
  const { data } = await api.get('/api/QueryLibrary');
  return data;
};

export const getQueryLibraryItem = async (id) => {
  const { data } = await api.get(`/api/QueryLibrary/${id}`);
  return data;
};

export const createQueryLibraryItem = async (body) => {
  const { data } = await api.post('/api/QueryLibrary', body);
  return data;
};

export const updateQueryLibraryItem = async (id, body) => {
  const { data } = await api.put(`/api/QueryLibrary/${id}`, body);
  return data;
};

export const deleteQueryLibraryItem = async (id) => {
  await api.delete(`/api/QueryLibrary/${id}`);
};

export const exportQueryLibrary = async () => {
  const { data } = await api.get('/api/QueryLibrary/export');
  return data;
};

export const importQueryLibrary = async (payload) => {
  const { data } = await api.post('/api/QueryLibrary/import', payload);
  return data;
};

export const getPopularQueries = async (top = 5) => {
  const { data } = await api.get('/api/QueryLibrary/popular', { params: { top } });
  return data;
};

export const incrementQueryUsage = async (id) => {
  await api.post(`/api/QueryLibrary/${id}/use`);
};

export const getRecentActivity = async (count = 10) => {
  const { data } = await api.get('/api/activity/recent', { params: { count } });
  return data;
};

export const askQuery = async (message) => {
  const { data } = await api.post('/api/Query/ask', { message });
  return data;
};

/**
 * Chat (Gemini) via backend proxy. API key never sent from frontend.
 */
export const getChatStatus = async () => {
  const { data } = await api.get('/api/Chat/status');
  return data;
};

export const postChat = async (message, history = []) => {
  const { data } = await api.post('/api/Chat', {
    message,
    history: history.map((m) => ({ sender: m.sender, text: m.text })),
  });
  return data;
};

/**
 * Dashboard creation flow: step-by-step guided conversation.
 * Body: { message, history, flowContext? }. Returns { responseText, stepData?, completePayload? }.
 */
export const postDashboardFlow = async (message, history = [], flowContext = null) => {
  const { data } = await api.post('/api/Chat/dashboard-flow', {
    message,
    history: history.map((m) => ({ sender: m.sender, text: m.text })),
    flowContext,
  });
  return data;
};

/**
 * NLP/semantic match: user natural language → most relevant query from mock library.
 * Body: { userInput }. Returns { matched, matchedId?, category?, query?, explanation?, confidence?, message? }.
 */
export const matchQuery = async (userInput) => {
  const { data } = await api.post('/api/Chat/match-query', { userInput });
  return data;
};

/**
 * Query Builder AI: natural language → Sumo Logic query + explanation.
 * Body: { userInput, context? }. Returns { query, explanation, confidence }.
 */
export const generateQuery = async (userInput, context = '') => {
  const { data } = await api.post('/api/Chat/generate-query', { userInput, context });
  return data;
};

/**
 * Query Builder AI: get 3–5 optimization suggestions for a query.
 * Body: { query, performance? }. Returns { suggestions: [{ suggestion, impact, reason }] }.
 */
export const optimizeQuery = async (query, performance = '') => {
  const { data } = await api.post('/api/Chat/optimize-query', { query, performance });
  return data;
};

/**
 * Query Builder AI: plain English explanation of a Sumo Logic query.
 * Body: { query }. Returns { explanation, confidence }.
 */
export const explainQuery = async (query) => {
  const { data } = await api.post('/api/Chat/explain-query', { query });
  return data;
};

/**
 * Run a Sumo Logic query and return results.
 * Backend (Gary) implements execution; stub returns mock until ready.
 */
export const runQuery = async (query, timeRange = '1h', limit = 100) => {
  const { data } = await api.post('/api/Query/run', {
    query,
    timeRange,
    limit,
  }).catch(() => ({
    data: {
      rows: [],
      columns: [],
      rowCount: 0,
      executionTimeMs: 0,
      message: 'Query execution not yet implemented. Backend will provide results.',
    },
  }));
  return data;
};
