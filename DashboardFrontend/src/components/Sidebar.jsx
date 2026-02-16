import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const ADMIN_ROLES = ['admin', 'senior_developer'];

const Sidebar = ({ onStartWizard, onQuickQuery }) => {
  const { user } = useAuth();
  const canAccessAdmin = user && ADMIN_ROLES.includes(user.role);

  return (
    <aside className="sidebar">
      <div className="quick-access">
        {canAccessAdmin && (
          <button onClick={onStartWizard}>
            <span className="icon">📊</span>
            <span>Create Dashboard</span>
          </button>
        )}
        <button onClick={onQuickQuery}>
          <span className="icon">🔍</span>
          <span>Quick Query</span>
        </button>
        {canAccessAdmin && (
          <Link to="/admin" className="quick-access-btn-link">
            <span className="icon">⚙️</span>
            <span>Admin</span>
          </Link>
        )}
        <Link to="/common-qa" className="quick-access-btn-link">
          <span className="icon">💬</span>
          <span>Common Q&A</span>
        </Link>
        <button>
          <span className="icon">📖</span>
          <span>Go to Confluence</span>
        </button>
      </div>
      <div className="recent-activity">
        <h2>Recent Activity</h2>
        <ul>
          <li>Dashboard ‘Sales Q3’ updated 2 hours ago</li>
          <li>Query ‘Inventory Check’ ran successfully</li>
          <li>New Confluence page: ‘Q4 Planning’</li>
        </ul>
      </div>
    </aside>
  );
};

export default Sidebar;
