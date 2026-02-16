import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'https://localhost:7290';

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
