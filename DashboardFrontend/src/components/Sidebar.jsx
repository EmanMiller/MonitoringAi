import React from 'react';
import { useNavigate, NavLink } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useDashboardFlow } from '../context/DashboardFlowContext';
import RecentActivity from './RecentActivity';

const Sidebar = ({ onQuickQuery }) => {
  const { user } = useAuth();
  const { requestDashboardCreation } = useDashboardFlow();
  const navigate = useNavigate();

  const handleCreateDashboard = () => {
    requestDashboardCreation();
    navigate('/');
  };

  return (
    <aside className="sidebar">
      <div className="quick-access">
        <NavLink to="/" end className={({ isActive }) => `quick-access-btn-link ${isActive ? 'active' : ''}`} data-icon="home">
          <span className="icon-wrap"><span className="icon">🏠</span></span>
          <span>Dashboard</span>
        </NavLink>
        {user && (
          <button onClick={handleCreateDashboard} data-icon="dashboard">
            <span className="icon-wrap"><span className="icon">📊</span></span>
            <span>Create Dashboard</span>
          </button>
        )}
        <button onClick={onQuickQuery} data-icon="query">
          <span className="icon-wrap"><span className="icon">🔍</span></span>
          <span>Quick Query</span>
        </button>
        <NavLink to="/query-builder" className={({ isActive }) => `quick-access-btn-link ${isActive ? 'active' : ''}`} data-icon="query-builder">
          <span className="icon-wrap"><span className="icon">⚡</span></span>
          <span>Query Builder</span>
        </NavLink>
        <NavLink to="/common-qa" className={({ isActive }) => `quick-access-btn-link ${isActive ? 'active' : ''}`} data-icon="qa">
          <span className="icon-wrap"><span className="icon">💬</span></span>
          <span>Common Q&A</span>
        </NavLink>
        <button data-icon="confluence">
          <span className="icon-wrap"><span className="icon">📖</span></span>
          <span>Go to Confluence</span>
        </button>
      </div>
      <RecentActivity />
    </aside>
  );
};

export default Sidebar;
