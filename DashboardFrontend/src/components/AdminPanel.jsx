import React, { useState } from 'react';
import AdminView from './AdminView';
import QueryLibraryView from './QueryLibraryView';

const TABS = [
  { id: 'mappings', label: 'Log Mappings' },
  { id: 'query-library', label: 'Query Library' },
];

function AdminPanel() {
  const [activeTab, setActiveTab] = useState('query-library');

  return (
    <div className="admin-panel">
      <div className="admin-tabs">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            className={`admin-tab ${activeTab === tab.id ? 'active' : ''}`}
            onClick={() => setActiveTab(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>
      <div className="admin-tab-content">
        {activeTab === 'mappings' && <AdminView />}
        {activeTab === 'query-library' && <QueryLibraryView />}
      </div>
    </div>
  );
}

export default AdminPanel;
