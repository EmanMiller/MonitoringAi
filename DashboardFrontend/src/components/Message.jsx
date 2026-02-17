import React from 'react';
import ChatMessageOptions from './Chat/ChatMessageOptions';
import ChatMessageInput from './Chat/ChatMessageInput';

const formatTime = (ts) => {
  if (ts == null) return null;
  try {
    const d = new Date(typeof ts === 'number' ? ts : ts);
    return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  } catch {
    return null;
  }
};

const Message = ({ message, isActiveStep, onOptionSelect, onInputSubmit }) => {
  const { text, sender, timestamp, stepData } = message;
  const messageClass = `message ${sender}`;
  const displayName = sender === 'user' ? 'You' : sender === 'assistant' ? 'Gemini' : 'System';
  const timeStr = formatTime(timestamp);
  const showOptions = isActiveStep && stepData?.options?.length > 0;
  const showInput = isActiveStep && stepData && (!stepData.options || stepData.options.length === 0) && stepData.type !== 'complete' && stepData.type !== 'confirm';

  return (
    <div className={messageClass}>
      <div className="message-sender">
        {displayName}
        {timeStr && <span className="message-timestamp">{timeStr}</span>}
      </div>
      <div className="message-content">
        {text && <p>{text}</p>}
        {showOptions && (
          <ChatMessageOptions options={stepData.options} onSelect={onOptionSelect} />
        )}
        {showInput && (
          <ChatMessageInput
            placeholder={stepData.prompt || 'Enter value...'}
            onSubmit={onInputSubmit}
          />
        )}
      </div>
    </div>
  );
};

export default Message;
