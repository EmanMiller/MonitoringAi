import React from 'react';

const formatTime = (ts) => {
  if (ts == null) return null;
  try {
    const d = new Date(typeof ts === 'number' ? ts : ts);
    return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  } catch {
    return null;
  }
};

const Message = ({ message }) => {
  const { text, sender, timestamp } = message;
  const messageClass = `message ${sender}`;
  const displayName = sender === 'user' ? 'You' : sender === 'assistant' ? 'Gemini' : 'System';
  const timeStr = formatTime(timestamp);

  return (
    <div className={messageClass}>
      <div className="message-sender">
        {displayName}
        {timeStr && <span className="message-timestamp">{timeStr}</span>}
      </div>
      <div className="message-content">
        <p>{text}</p>
      </div>
    </div>
  );
};

export default Message;
