import React from 'react';
import { useAuth } from '../context/AuthContext';

const Header = () => {
  const { user, logout } = useAuth();
  return (
    <header className="header">
      <div className="header-left">
        <h2>Crate&Barrel</h2>
        <span>Quick Dashboard, Querries, & Confluence</span>
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
