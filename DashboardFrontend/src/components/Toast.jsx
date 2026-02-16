import React, { useEffect } from 'react';

const Toast = ({ message, variant = 'error', onDismiss, duration = 5000 }) => {
  useEffect(() => {
    if (!duration || !onDismiss) return;
    const t = setTimeout(onDismiss, duration);
    return () => clearTimeout(t);
  }, [duration, onDismiss]);

  if (!message) return null;

  return (
    <div className={`chat-toast chat-toast-${variant}`} role="alert">
      <span>{message}</span>
      {onDismiss && (
        <button type="button" className="chat-toast-dismiss" onClick={onDismiss} aria-label="Dismiss">
          ×
        </button>
      )}
    </div>
  );
};

export default Toast;
