import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import Header from './Header';
import Sidebar from './Sidebar';
import QuickQueryPopup from './QuickQueryPopup';

function AppLayout() {
  const [isQuickQueryOpen, setQuickQueryOpen] = useState(false);

  const handleQueryReady = (queryText) => {
    console.info('Query ready:', queryText?.slice(0, 80));
  };

  return (
    <div className="app-container">
      <Header />
      <div className="main-content">
        <Sidebar
          onQuickQuery={() => setQuickQueryOpen(true)}
        />
        <div className="main-content-area">
          <Outlet />
        </div>
      </div>
      <QuickQueryPopup
        isOpen={isQuickQueryOpen}
        onClose={() => setQuickQueryOpen(false)}
        onQueryReady={handleQueryReady}
      />
    </div>
  );
}

export default AppLayout;
