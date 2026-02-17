import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import AppLayout from './components/AppLayout';
import AdminPanel from './components/AdminPanel';
import ChatWindow from './components/ChatWindow';
import CommonQAPage from './components/CommonQAPage';
import ProtectedRoute from './components/ProtectedRoute';
import LoginPage from './components/LoginPage';
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
            <Route index element={<ChatWindow />} />
            <Route path="admin" element={<ProtectedRoute><AdminPanel /></ProtectedRoute>} />
            <Route path="common-qa" element={<CommonQAPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
      </DashboardFlowProvider>
    </AuthProvider>
  );
}

export default App;
