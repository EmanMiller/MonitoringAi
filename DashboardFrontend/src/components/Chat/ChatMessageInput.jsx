import React, { useState } from 'react';

/**
 * Inline text input for dashboard creation steps (e.g. dashboard title).
 * placeholder from stepData.prompt. onSubmit(value) when user submits.
 */
export default function ChatMessageInput({ placeholder = 'Enter value...', onSubmit }) {
  const [value, setValue] = useState('');

  const handleSubmit = (e) => {
    e.preventDefault();
    const v = value.trim();
    if (v && onSubmit) {
      onSubmit(v);
      setValue('');
    }
  };

  return (
    <form className="chat-message-input-form" onSubmit={handleSubmit}>
      <input
        type="text"
        className="chat-message-input-field"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        placeholder={placeholder}
        autoFocus
        aria-label={placeholder}
      />
      <button type="submit" className="chat-message-input-submit" disabled={!value.trim()}>
        Submit
      </button>
    </form>
  );
}
