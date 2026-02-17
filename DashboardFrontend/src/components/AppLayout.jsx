import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import Header from './Header';
import Sidebar from './Sidebar';
import DashboardCreatorWizard from './DashboardCreatorWizard';
import QuickQueryPopup from './QuickQueryPopup';

function AppLayout() {
  const [isWizardOpen, setWizardOpen] = useState(false);
  const [isQuickQueryOpen, setQuickQueryOpen] = useState(false);

  const handleQueryReady = (queryText) => {
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
        <div className="main-content-area">
          <Outlet />
        </div>
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

export default AppLayout;
