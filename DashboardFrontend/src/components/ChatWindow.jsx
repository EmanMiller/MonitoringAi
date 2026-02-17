import React, { useState, useEffect, useCallback, useRef } from 'react';
import Message from './Message';
import ChatInput from './Chat/ChatInput';
import ChatSettingsModal from './ChatSettingsModal';
import Toast from './Toast';
import { getChatStatus, postChat, postDashboardFlow, createDashboardFromWizard, matchQuery } from '../services/api';
import { useDashboardFlow } from '../context/DashboardFlowContext';

const NO_KEY_MESSAGE = 'Chat is not configured. Ask your administrator to set the Gemini API key on the server.';
const WELCOME_MESSAGE = 'How can I help you today?';

function getChatErrorMessage(err) {
  if (!err) return 'Something went wrong.';
  const status = err.response?.status;
  const details = err.response?.data?.details;
  if (status === 429 || details?.toLowerCase?.().includes('too many'))
    return 'Too many requests. Please wait.';
  if (status === 503 || details?.toLowerCase?.().includes('api key') || details?.toLowerCase?.().includes('not configured'))
    return 'Gemini unavailable. Check API key.';
  if (status === 401 || status === 403)
    return 'API key invalid. Check settings.';
  if (err.message === 'Network Error' || err.code === 'ERR_NETWORK')
    return 'Connection failed. Please try again.';
  return details || err.message || 'Something went wrong.';
}

const DASHBOARD_CREATION_PROMPT = 'I want to create a dashboard';

const ChatWindow = () => {
  const [messages, setMessages] = useState([]);
  const [chatAvailable, setChatAvailable] = useState(false);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [toast, setToast] = useState({ message: '', variant: 'error' });
  const [dashboardFlowState, setDashboardFlowState] = useState({
    active: false,
    step: 0,
    collected: {},
    lastStepData: null,
  });
  const { consumeRequest, pendingRequest } = useDashboardFlow();

  const showToast = useCallback((message, variant = 'error') => {
    setToast({ message, variant });
  }, []);

  const checkStatus = useCallback(async () => {
    try {
      await getChatStatus();
      setChatAvailable(true);
      return true;
    } catch {
      setChatAvailable(false);
      return false;
    }
  }, []);

  useEffect(() => {
    checkStatus();
  }, [checkStatus]);


  useEffect(() => {
    if (chatAvailable) {
      setMessages((prev) => {
        if (prev.length === 0 || (prev.length === 1 && prev[0].text === NO_KEY_MESSAGE))
          return [{ text: WELCOME_MESSAGE, sender: 'assistant' }];
        return prev;
      });
    } else {
      setMessages((prev) => {
        if (prev.length === 0 || (prev.length === 1 && prev[0].text === WELCOME_MESSAGE))
          return [{ text: NO_KEY_MESSAGE, sender: 'system' }];
        return prev;
      });
    }
  }, [chatAvailable]);

  const handleConnectionChange = useCallback((available) => {
    setChatAvailable(!!available);
  }, []);

  const handleSendMessage = useCallback(async (text) => {
    if (!text.trim()) return;

    const userMessage = { text: text.trim(), sender: 'user', timestamp: Date.now() };
    setMessages((prev) => {
      const withoutPlaceholder = prev.filter((m) => m.text !== NO_KEY_MESSAGE && m.text !== WELCOME_MESSAGE);
      return [...withoutPlaceholder, userMessage];
    });
    setLoading(true);

    const useDashboardFlow = text.trim() === DASHBOARD_CREATION_PROMPT || dashboardFlowState.active;
    let collected = { ...dashboardFlowState.collected };
    if (useDashboardFlow && dashboardFlowState.lastStepData) {
      const sd = dashboardFlowState.lastStepData;
      if (sd.step === 1) collected = { ...collected, dashboardTitle: text.trim() };
      else if (sd.step === 2 && sd.options?.includes(text.trim()))
        collected = { ...collected, panels: { ...(collected.panels || {}), [text.trim()]: true } };
      else if (sd.step === 3) collected = { ...collected, useDefaults: text.trim().toLowerCase().includes('default') || text.trim().toLowerCase() === 'yes' };
    }
    const history = messages
      .filter((m) => m.sender === 'user' || m.sender === 'assistant')
      .concat([userMessage])
      .slice(0, -1);
    const flowContext = useDashboardFlow ? {
      step: dashboardFlowState.step,
      collected: Object.keys(collected).length ? {
        dashboardTitle: collected.dashboardTitle,
        useDefaults: collected.useDefaults,
        variables: collected.variables,
        panels: collected.panels,
      } : null,
    } : null;

    try {
      let data;
      if (useDashboardFlow) {
        data = await postDashboardFlow(text.trim(), history, flowContext);
      } else {
        // Simulate chatbox: try match-query first for natural language → pre-built query
        const trimmed = text.trim();
        if (trimmed.length >= 3) {
          try {
            const matchResult = await matchQuery(trimmed);
            if (matchResult?.matched && matchResult?.query) {
              const explanation = matchResult.explanation || `Matched: ${matchResult.category || 'query'}.`;
              const replyText = `${explanation}\n\n\`\`\`\n${matchResult.query}\n\`\`\``;
              setMessages((prev) => [...prev, { text: replyText, sender: 'assistant', timestamp: Date.now() }]);
              setLoading(false);
              return;
            }
            if (matchResult?.matched === false && matchResult?.message) {
              setMessages((prev) => [...prev, { text: matchResult.message, sender: 'assistant', timestamp: Date.now() }]);
              setLoading(false);
              return;
            }
          } catch {
            // Fall through to general chat
          }
        }
        data = await postChat(text.trim(), history);
      }

      if (useDashboardFlow && data) {
        const { responseText, stepData, completePayload } = data;
        const assistantMsg = {
          text: responseText || 'Continuing...',
          sender: 'assistant',
          timestamp: Date.now(),
          stepData: stepData || undefined,
        };
        setMessages((prev) => [...prev, assistantMsg]);

        if (completePayload) {
          setDashboardFlowState({ active: false, step: 0, collected: {}, lastStepData: null });
          try {
            const result = await createDashboardFromWizard(completePayload);
            showToast(`Dashboard created! ${result?.dashboardUrl ? 'View in Sumo Logic.' : ''}`, 'success');
            setMessages((prev) => [...prev, {
              text: `✅ Your dashboard has been created successfully.`,
              sender: 'assistant',
              timestamp: Date.now(),
            }]);
          } catch (e) {
            showToast('Dashboard creation failed. ' + (e?.response?.data?.message || e.message));
          }
        } else if (stepData) {
          setDashboardFlowState((s) => ({
            ...s,
            active: true,
            step: stepData.step ?? s.step + 1,
            lastStepData: stepData,
          }));
        }
      } else {
        const reply = data?.response ?? data?.responseText ?? '';
        setMessages((prev) => [...prev, { text: reply || 'No response.', sender: 'assistant', timestamp: Date.now() }]);
      }
    } catch (err) {
      showToast(getChatErrorMessage(err));
      setMessages((prev) => prev.filter((m) => m !== userMessage));
    } finally {
      setLoading(false);
    }
  }, [messages, dashboardFlowState, showToast]);

  const handleSendMessageRef = useRef(handleSendMessage);
  handleSendMessageRef.current = handleSendMessage;

  useEffect(() => {
    if (consumeRequest()) {
      handleSendMessageRef.current(DASHBOARD_CREATION_PROMPT);
    }
  }, [consumeRequest, pendingRequest]);

  const handleUseSuggestedQuery = useCallback((queryValue) => {
    const now = Date.now();
    const userMessage = { text: 'Use saved query', sender: 'user', timestamp: now };
    const assistantMessage = {
      text: `Here's a matching saved query:\n\n\`\`\`\n${queryValue}\n\`\`\``,
      sender: 'assistant',
      timestamp: now,
    };
    setMessages((prev) => {
      const withoutPlaceholder = prev.filter((m) => m.text !== NO_KEY_MESSAGE && m.text !== WELCOME_MESSAGE);
      return [...withoutPlaceholder, userMessage, assistantMessage];
    });
  }, []);

  const displayMessages = messages.filter((m) => m.text || m.stepData);
  const lastWithStepData = [...displayMessages].reverse().find((m) => m.sender === 'assistant' && m.stepData);

  return (
    <div className="chat-container corner-brackets">
      <div className="chat-header">
        <span className="chat-header-title">Chat</span>
        <div className="chat-header-right">
          <span className={`chat-status ${chatAvailable ? 'connected' : 'disconnected'}`}>
            <span className="chat-status-dot" />
            {chatAvailable ? 'Connected' : 'Not Connected'}
          </span>
          <button
            type="button"
            className="chat-settings-btn"
            onClick={() => setSettingsOpen(true)}
            aria-label="Chat settings"
            title="Settings"
          >
            ⚙️
          </button>
        </div>
      </div>
      <div className="message-list">
        {displayMessages.map((message, index) => (
          <Message
            key={`${index}-${message.text?.slice(0, 20)}`}
            message={message}
            isActiveStep={!loading && message === lastWithStepData}
            onOptionSelect={(val) => handleSendMessage(val)}
            onInputSubmit={(val) => handleSendMessage(val)}
          />
        ))}
        {loading && (
          <div className="message assistant typing-indicator">
            <div className="message-sender">Gemini</div>
            <div className="message-content typing-content">Gemini is thinking...</div>
          </div>
        )}
      </div>
      <ChatInput
        onSend={handleSendMessage}
        disabled={loading}
      />
      <ChatSettingsModal
        isOpen={settingsOpen}
        onClose={() => setSettingsOpen(false)}
        onConnectionChange={handleConnectionChange}
        connectionStatus={chatAvailable}
        onCheckStatus={checkStatus}
      />
      <Toast
        message={toast.message}
        variant={toast.variant}
        onDismiss={() => setToast((t) => ({ ...t, message: '' }))}
      />
    </div>
  );
};

export default ChatWindow;
