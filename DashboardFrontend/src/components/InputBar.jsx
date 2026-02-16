import React, { useState, useRef, useEffect, useCallback } from 'react';
import { searchQueryLibrary } from '../services/api';
import { CHAT_MESSAGE_MAX_LENGTH, stripDangerousTags } from '../utils/sanitize';

const SEND_DEBOUNCE_MS = 400;
const QUERY_LIBRARY_DEBOUNCE_MS = 350;
const MIN_CONFIDENCE = 0.7;

const InputBar = ({ onSendMessage, onUseSuggestedQuery, disabled, loading }) => {
  const [message, setMessage] = useState('');
  const [isComposing, setIsComposing] = useState(false);
  const [suggestion, setSuggestion] = useState(null);
  const debounceRef = useRef(null);
  const searchDebounceRef = useRef(null);
  const textareaRef = useRef(null);

  const adjustHeight = useCallback(() => {
    const el = textareaRef.current;
    if (!el) return;
    el.style.height = 'auto';
    el.style.height = `${Math.min(el.scrollHeight, 160)}px`;
  }, []);

  useEffect(() => {
    adjustHeight();
  }, [message, adjustHeight]);

  useEffect(() => {
    const trimmed = message.trim();
    if (!trimmed || trimmed.length < 3) {
      setSuggestion(null);
      return;
    }
    if (searchDebounceRef.current) clearTimeout(searchDebounceRef.current);
    searchDebounceRef.current = setTimeout(() => {
      searchQueryLibrary(trimmed)
        .then((results) => {
          const list = Array.isArray(results) ? results : [];
          const top = list[0];
          if (top && top.confidence >= MIN_CONFIDENCE && top.value) {
            setSuggestion({ key: top.key, value: top.value, confidence: top.confidence });
          } else {
            setSuggestion(null);
          }
        })
        .catch(() => setSuggestion(null));
    }, QUERY_LIBRARY_DEBOUNCE_MS);
    return () => {
      if (searchDebounceRef.current) clearTimeout(searchDebounceRef.current);
    };
  }, [message]);

  const handleSend = useCallback(() => {
    let trimmed = message.trim();
    if (!trimmed || disabled || loading) return;
    if (trimmed.length > CHAT_MESSAGE_MAX_LENGTH) return;
    trimmed = stripDangerousTags(trimmed);
    if (suggestion) setSuggestion(null);
    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
      debounceRef.current = null;
    }
    onSendMessage(trimmed);
    setMessage('');
  }, [message, disabled, loading, onSendMessage, suggestion]);

  const handleUseSuggestion = useCallback(() => {
    if (!suggestion?.value || !onUseSuggestedQuery) return;
    onUseSuggestedQuery(suggestion.value);
    setSuggestion(null);
    setMessage('');
  }, [suggestion, onUseSuggestedQuery]);

  const handleDismissSuggestion = useCallback(() => {
    setSuggestion(null);
  }, []);

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' && !e.shiftKey && !isComposing) {
      e.preventDefault();
      debounceRef.current = setTimeout(handleSend, SEND_DEBOUNCE_MS);
    }
  };

  const handleSubmit = () => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(handleSend, SEND_DEBOUNCE_MS);
  };

  return (
    <div className="input-bar">
      {suggestion && (
        <div className="query-suggestion-banner" role="status">
          <span className="query-suggestion-text">Found a similar query — Use this?</span>
          <div className="query-suggestion-actions">
            <button type="button" className="query-suggestion-btn query-suggestion-yes" onClick={handleUseSuggestion}>
              Yes
            </button>
            <button type="button" className="query-suggestion-btn query-suggestion-no" onClick={handleDismissSuggestion}>
              No
            </button>
          </div>
        </div>
      )}
      <div className="input-bar-row">
      <textarea
        ref={textareaRef}
        className="input-bar-textarea"
        placeholder={disabled ? 'Chat is not configured. See Settings.' : 'Type a message...'}
        value={message}
        onChange={(e) => setMessage(e.target.value.slice(0, CHAT_MESSAGE_MAX_LENGTH))}
        onKeyDown={handleKeyDown}
        onCompositionStart={() => setIsComposing(true)}
        onCompositionEnd={() => setIsComposing(false)}
        rows={1}
        disabled={disabled}
        aria-label="Message input"
        maxLength={CHAT_MESSAGE_MAX_LENGTH}
      />
      {message.length > CHAT_MESSAGE_MAX_LENGTH - 100 && (
        <span className="input-bar-char-count" aria-live="polite">
          {message.length} / {CHAT_MESSAGE_MAX_LENGTH}
        </span>
      )}
      <button
        type="button"
        className="input-bar-send"
        onClick={handleSubmit}
        disabled={disabled || loading || !message.trim() || message.length > CHAT_MESSAGE_MAX_LENGTH}
        aria-label="Send"
      >
        <span role="img" aria-label="send">▶️</span>
      </button>
      </div>
    </div>
  );
};

export default InputBar;
