import React from 'react';

/**
 * Renders clickable option buttons for dashboard creation steps.
 * When clicked, calls onSelect(optionValue).
 */
export default function ChatMessageOptions({ options = [], onSelect }) {
  if (!options?.length) return null;
  return (
    <div className="chat-message-options" role="group" aria-label="Choose an option">
      {options.map((opt, i) => (
        <button
          key={i}
          type="button"
          className="chat-message-option-btn"
          onClick={() => onSelect?.(opt)}
        >
          {opt}
        </button>
      ))}
    </div>
  );
}
