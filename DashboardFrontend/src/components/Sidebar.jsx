import React from 'react';

const Sidebar = ({ onStartWizard }) => {
  return (
    <aside className="sidebar">
      <div className="quick-access">
        <button onClick={onStartWizard}>
          <span className="icon">📊</span>
          <span>Create Dashboard</span>
        </button>
        <button>
          <span className="icon">🔍</span>
          <span>Quick Query</span>
        </button>
        <button>
          <span className="icon">💬</span>
          <span>Common Q&A</span>
        </button>
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
