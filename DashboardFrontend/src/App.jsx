import React, { useState, useEffect } from 'react';
import './App.css';
import Header from './components/Header';
import Sidebar from './components/Sidebar';
import ChatWindow from './components/ChatWindow';
import DashboardCreatorWizard from './components/DashboardCreatorWizard';

function App() {
  const [messages, setMessages] = useState([]);
  const [isWizardOpen, setWizardOpen] = useState(false);

  useEffect(() => {
    // Initial welcome message
    setMessages([
      {
        text: 'How can I help you today?',
        sender: 'system',
      },
    ]);
  }, []);

  const handleSendMessage = async (messageText) => {
    const userMessage = { text: messageText, sender: 'user' };
    setMessages((prevMessages) => [...prevMessages, userMessage]);

    // Simple echo bot for now
    const systemMessage = {
      text: `You said: "${messageText}"`,
      sender: 'system',
    };
    setMessages((prevMessages) => [...prevMessages, systemMessage]);
  };

  return (
    <div className="app-container">
      <Header />
      <div className="main-content">
        <Sidebar onStartWizard={() => setWizardOpen(true)} />
        <ChatWindow messages={messages} onSendMessage={handleSendMessage} />
      </div>
      <DashboardCreatorWizard 
        isOpen={isWizardOpen} 
        onClose={() => setWizardOpen(false)} 
      />
    </div>
  );
}

export default App;
