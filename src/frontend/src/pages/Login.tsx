import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { LogIn } from 'lucide-react';
import { useAuth } from '../context/useAuth';
import { readApiError, normalizeError } from '../utils/api';
import './Auth.css';

type FormSubmitEvent = { preventDefault: () => void };

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();
  const { refresh } = useAuth();

  const handleLogin = async (e: FormSubmitEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError('');

    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ email, password })
      });

      if (res.ok) {
        await refresh(); // update AuthContext with the new cookie
        navigate('/dashboard');
      } else {
        setError(await readApiError(res, 'Invalid credentials'));
      }
    } catch (err) {
      setError(normalizeError(err, 'Unable to sign in'));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="auth-wrapper animate-fade-in">
      <div className="auth-card animate-slide-up">
        <div className="auth-header">
          <div className="auth-icon-box">
            <LogIn size={24} />
          </div>
          <h2>Welcome back</h2>
          <p>Enter your details to access your workspace.</p>
        </div>

        {error && <div className="alert error mb-4">{error}</div>}

        <form onSubmit={handleLogin} className="auth-form">
          <div className="form-group">
            <label htmlFor="email">Email address</label>
            <input 
              id="email" 
              type="email" 
              required 
              value={email}
              onChange={e => setEmail(e.target.value)}
              placeholder="you@example.com" 
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input 
              id="password" 
              type="password" 
              required 
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="••••••••" 
            />
          </div>

          <button type="submit" className="btn-auth" disabled={isLoading || !email || !password}>
            {isLoading ? <div className="pulse-loader" /> : 'Sign in'}
          </button>
        </form>

        <div className="auth-footer">
          <p>Don't have an account? <Link to="/register">Sign up</Link></p>
        </div>
      </div>
    </div>
  );
}
