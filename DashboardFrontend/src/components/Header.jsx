import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const Header = ({ backTo, backLabel = 'Back to Dashboard' }) => {
  const { user, logout } = useAuth();
  return (
    <header className="header">
      <div className="header-left">
        {backTo && (
          <Link to={backTo} className="header-back-link">
            ← {backLabel}
          </Link>
        )}
        <Link to="/" className="header-logo-link">
          <h2>Crate&Barrel</h2>
        </Link>
        <span>Quick Dashboard, Queries, & Confluence</span>
      </div>
      <div className="header-right">
        <div className="secure-connection">
          <span>🔒</span>
          <span>Secure Connection</span>
        </div>
        <div className="user-info">
          <span>👤</span>
          <span>Welcome, {user?.userName ?? 'Employee'}</span>
        </div>
        <button type="button" className="header-logout" onClick={logout} aria-label="Sign out">
          Sign out
        </button>
      </div>
    </header>
  );
};

export default Header;
