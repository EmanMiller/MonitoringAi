import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import AppLayout from './components/AppLayout';
import ChatPage from './pages/ChatPage';
import CommonQAPage from './pages/CommonQAPage';
import ProtectedRoute from './components/ProtectedRoute';
import LoginPage from './pages/LoginPage';
import { AuthProvider } from './context/AuthContext';
import { DashboardFlowProvider } from './context/DashboardFlowContext';

function App() {
  return (
    <AuthProvider>
      <DashboardFlowProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<ProtectedRoute><AppLayout /></ProtectedRoute>}>
            <Route index element={<ChatPage />} />
            <Route path="common-qa" element={<CommonQAPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
      </DashboardFlowProvider>
    </AuthProvider>
  );
}

export default App;
