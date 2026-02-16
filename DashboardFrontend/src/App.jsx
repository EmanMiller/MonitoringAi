import React, { useState } from 'react';
import { BrowserRouter, Routes, Route, useNavigate } from 'react-router-dom';
import './App.css';
import Header from './components/Header';
import Sidebar from './components/Sidebar';
import ChatWindow from './components/ChatWindow';
import DashboardCreatorWizard from './components/DashboardCreatorWizard';
import QuickQueryPopup from './components/QuickQueryPopup';
import AdminPanel from './components/AdminPanel';
import CommonQAPage from './components/CommonQAPage';
import RoleGuard from './components/RoleGuard';
import ProtectedRoute from './components/ProtectedRoute';
import LoginPage from './components/LoginPage';
import TokenExpiryWarningModal from './components/TokenExpiryWarningModal';
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

function AdminPage() {
  const navigate = useNavigate();
  return (
    <div className="app-container">
      <header className="header">
        <div className="header-left">
          <button type="button" className="admin-back-btn" onClick={() => navigate('/')}>
            ← Back
          </button>
          <h2>Admin</h2>
        </div>
      </header>
      <div className="main-content main-content-admin">
        <AdminPanel />
      </div>
    </div>
  );
}

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <TokenExpiryWarningModal />
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<ProtectedRoute><MainPage /></ProtectedRoute>} />
          <Route
            path="/admin"
            element={
              <ProtectedRoute>
                <RoleGuard allowedRoles={['admin', 'senior_developer', 'manager', 'vp']} redirectTo="/">
                  <AdminPage />
                </RoleGuard>
              </ProtectedRoute>
            }
          />
          <Route path="/common-qa" element={<ProtectedRoute><CommonQAPage /></ProtectedRoute>} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
