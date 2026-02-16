import React from 'react';
import Message from './Message';
import InputBar from './InputBar';

const ChatWindow = ({ messages, onSendMessage }) => {
  return (
    <div className="chat-container">
      <div className="message-list">
        {messages.map((message, index) => (
          <Message key={index} message={message} />
        ))}
      </div>
      <InputBar onSendMessage={onSendMessage} />
    </div>
  );
};

export default ChatWindow;
