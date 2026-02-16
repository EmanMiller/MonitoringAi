import React from 'react';

const Header = () => {
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
          <span>Welcome, Employee</span>
        </div>
      </div>
    </header>
  );
};

export default Header;
