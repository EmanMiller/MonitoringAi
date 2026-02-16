import React from 'react';

const InputBar = ({ onSendMessage }) => {
  const [message, setMessage] = React.useState('');

  const handleSendMessage = () => {
    if (message.trim()) {
      onSendMessage(message);
      setMessage('');
    }
  };

  return (
    <div className="input-bar">
      <input
        type="text"
        placeholder=""
        value={message}
        onChange={(e) => setMessage(e.target.value)}
        onKeyPress={(e) => e.key === 'Enter' && handleSendMessage()}
      />
      <button onClick={handleSendMessage}>
        <span role="img" aria-label="send">▶️</span>
      </button>
    </div>
  );
};

export default InputBar;
