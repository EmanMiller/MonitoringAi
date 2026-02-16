import { GoogleGenerativeAI } from '@google/generative-ai';

let genAI = null;
let model = null;

const GEMINI_USER_ROLE = 'user';
const GEMINI_MODEL_ROLE = 'model';

/**
 * Convert app conversation format [{ text, sender }] to Gemini chat history.
 * @param {Array<{ text: string, sender: string }>} conversationHistory
 * @returns {Array<{ role: string, parts: Array<{ text: string }> }>}
 */
function toGeminiHistory(conversationHistory) {
  if (!Array.isArray(conversationHistory) || conversationHistory.length === 0) {
    return [];
  }
  return conversationHistory
    .filter((m) => m.sender === 'user' || m.sender === 'assistant')
    .map((m) => ({
      role: m.sender === 'user' ? GEMINI_USER_ROLE : GEMINI_MODEL_ROLE,
      parts: [{ text: m.text || '' }],
    }));
}

/**
 * Initialize the Gemini client with an API key.
 * @param {string} apiKey - Google Gemini API key
 * @returns {import('@google/generative-ai').GenerativeModel}
 */
export function initializeGemini(apiKey) {
  if (!apiKey || typeof apiKey !== 'string' || !apiKey.trim()) {
    throw new Error('Invalid API key');
  }
  genAI = new GoogleGenerativeAI(apiKey.trim());
  model = genAI.getGenerativeModel({ model: 'gemini-pro' });
  return model;
}

/**
 * Send a message and get a streaming or single response.
 * @param {string} prompt - User message
 * @param {Array<{ text: string, sender: string }>} [conversationHistory=[]] - Previous messages (user/assistant)
 * @returns {Promise<string>} - Assistant reply text
 */
export async function sendMessage(prompt, conversationHistory = []) {
  if (!model) {
    throw new Error('Gemini not initialized. Set your API key in Settings.');
  }

  const history = toGeminiHistory(conversationHistory);

  try {
    const chat = model.startChat({ history });
    const result = await chat.sendMessage(prompt);
    const response = result.response;
    if (!response || !response.text) {
      throw new Error('Empty response from Gemini');
    }
    return response.text();
  } catch (err) {
    if (err.message && err.message.includes('API key')) {
      throw new Error('Invalid or missing API key. Check Settings.');
    }
    if (err.message && (err.message.includes('429') || err.message.includes('RESOURCE_EXHAUSTED'))) {
      throw new Error('Rate limit exceeded. Please wait a moment and try again.');
    }
    if (err.message && (err.message.includes('503') || err.message.includes('UNAVAILABLE'))) {
      throw new Error('Gemini is temporarily unavailable. Try again later.');
    }
    if (err.message && err.message.includes('network') || err.name === 'TypeError') {
      throw new Error('Network error. Check your connection and try again.');
    }
    throw err;
  }
}

/**
 * Test the API key with a minimal request (ping).
 * @param {string} apiKey
 * @returns {Promise<boolean>}
 */
export async function testConnection(apiKey) {
  if (!apiKey || !apiKey.trim()) {
    throw new Error('Please enter an API key.');
  }
  initializeGemini(apiKey);
  await sendMessage('Hi');
  return true;
}
