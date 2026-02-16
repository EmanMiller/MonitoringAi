import React, { useState } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Header from './components/Header';
import Sidebar from './components/Sidebar';
import ChatWindow from './components/ChatWindow';
import DashboardCreatorWizard from './components/DashboardCreatorWizard';
import QuickQueryPopup from './components/QuickQueryPopup';
import CommonQAPage from './components/CommonQAPage';
import ProtectedRoute from './components/ProtectedRoute';
import LoginPage from './components/LoginPage';
import { AuthProvider } from './context/AuthContext';

function MainPage() {
  const [isWizardOpen, setWizardOpen] = useState(false);
  const [isQuickQueryOpen, setQuickQueryOpen] = useState(false);

  const handleQueryReady = (queryText) => {
    // Optional: pass to ChatWindow to inject as message, e.g. via ref or context
    console.info('Query ready:', queryText?.slice(0, 80));
  };

  return (
    <div className="app-container">
      <Header />
      <div className="main-content">
        <Sidebar
          onStartWizard={() => setWizardOpen(true)}
          onQuickQuery={() => setQuickQueryOpen(true)}
        />
        <ChatWindow />
      </div>
      <DashboardCreatorWizard
        isOpen={isWizardOpen}
        onClose={() => setWizardOpen(false)}
      />
      <QuickQueryPopup
        isOpen={isQuickQueryOpen}
        onClose={() => setQuickQueryOpen(false)}
        onQueryReady={handleQueryReady}
      />
    </div>
  );
}

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<ProtectedRoute><MainPage /></ProtectedRoute>} />
          <Route path="/common-qa" element={<ProtectedRoute><CommonQAPage /></ProtectedRoute>} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
