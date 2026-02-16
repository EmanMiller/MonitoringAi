import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import RecentActivity from './RecentActivity';

const Sidebar = ({ onStartWizard, onQuickQuery }) => {
  const { user } = useAuth();

  return (
    <aside className="sidebar">
      <div className="quick-access">
        {user && (
          <button onClick={onStartWizard}>
            <span className="icon">📊</span>
            <span>Create Dashboard</span>
          </button>
        )}
        <button onClick={onQuickQuery}>
          <span className="icon">🔍</span>
          <span>Quick Query</span>
        </button>
        <Link to="/common-qa" className="quick-access-btn-link">
          <span className="icon">💬</span>
          <span>Common Q&A</span>
        </Link>
        <button>
          <span className="icon">📖</span>
          <span>Go to Confluence</span>
        </button>
      </div>
      <RecentActivity />
    </aside>
  );
};

export default Sidebar;
