import React, { useState } from 'react';
import { useNavigate, NavLink } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useDashboardFlow } from '../context/DashboardFlowContext';
import RecentActivity from './RecentActivity';
import SimulateConfluenceModal from './SimulateConfluenceModal';

const Sidebar = () => {
  const { user } = useAuth();
  const { requestDashboardCreation } = useDashboardFlow();
  const navigate = useNavigate();
  const [simulateConfluenceOpen, setSimulateConfluenceOpen] = useState(false);

  const handleCreateDashboard = () => {
    requestDashboardCreation();
    navigate('/');
  };

  return (
    <aside className="sidebar">
      <div className="quick-access">
        {user && (
          <button onClick={handleCreateDashboard} data-icon="dashboard">
            <span className="icon-wrap"><span className="icon">📊</span></span>
            <span>Create Dashboard</span>
          </button>
        )}
        <NavLink to="/common-qa" className={({ isActive }) => `quick-access-btn-link ${isActive ? 'active' : ''}`} data-icon="qa">
          <span className="icon-wrap"><span className="icon">💬</span></span>
          <span>Common Q&A</span>
        </NavLink>
        {user && (
          <button onClick={() => setSimulateConfluenceOpen(true)} data-icon="confluence">
            <span className="icon-wrap"><span className="icon">📖</span></span>
            <span>Simulate Add to Confluence</span>
          </button>
        )}
      </div>
      <RecentActivity />
      <SimulateConfluenceModal isOpen={simulateConfluenceOpen} onClose={() => setSimulateConfluenceOpen(false)} />
    </aside>
  );
};

export default Sidebar;
