import React from 'react';

const Message = ({ message }) => {
  const { text, sender } = message;
  const messageClass = `message ${sender}`;

  return (
    <div className={messageClass}>
      <div className="message-sender">
        {sender === 'user' ? 'You' : 'Sumo Logic Employee'}
      </div>
      <div className="message-content">
        <p>{text}</p>
      </div>
    </div>
  );
};

export default Message;
