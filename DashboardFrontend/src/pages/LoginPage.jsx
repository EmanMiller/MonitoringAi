import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    if (!userName.trim()) { setError('User name is required.'); return; }
    if (!password) { setError('Password is required.'); return; }
    setSubmitting(true);
    const result = await login(userName.trim(), password);
    setSubmitting(false);
    if (result.ok) navigate('/', { replace: true });
    else setError(result.error || 'Login failed.');
  };

  return (
    <div className="login-page">
      <div className="login-card">
        <h1>Sumo Logic Dashboard</h1>
        <p className="login-subtitle">Sign in to continue</p>
        <form onSubmit={handleSubmit}>
          <div className="login-field">
            <label htmlFor="login-username">User name</label>
            <input id="login-username" type="text" autoComplete="username" value={userName} onChange={(e) => setUserName(e.target.value)} placeholder="User name" disabled={submitting} />
          </div>
          <div className="login-field">
            <label htmlFor="login-password">Password</label>
            <input id="login-password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="Password" disabled={submitting} />
          </div>
          {error && <p className="login-error" role="alert">{error}</p>}
          <button type="submit" className="button-primary login-submit" disabled={submitting}>{submitting ? 'Signing in…' : 'Sign in'}</button>
        </form>
      </div>
    </div>
  );
}
