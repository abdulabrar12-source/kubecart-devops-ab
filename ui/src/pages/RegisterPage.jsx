import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { register as apiRegister } from '../api/authApi';
import { useAuth } from '../contexts/AuthContext';

export default function RegisterPage() {
  const { login } = useAuth();
  const navigate = useNavigate();

  const [form, setForm] = useState({ fullName: '', email: '', password: '', confirm: '' });
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);

  function handleChange(e) {
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError(null);

    if (form.password !== form.confirm) {
      setError('Passwords do not match.');
      return;
    }
    if (form.password.length < 6) {
      setError('Password must be at least 6 characters.');
      return;
    }

    setLoading(true);
    try {
      const data = await apiRegister(form.email, form.password, form.fullName);
      login(data);
      navigate('/', { replace: true });
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1 className="auth-title">Create account</h1>
        <p className="auth-subtitle">Join KubeCart today</p>

        {error && <div className="error-banner">{error}</div>}

        <form onSubmit={handleSubmit} className="auth-form">
          <label className="field">
            <span>Full name</span>
            <input
              type="text"
              name="fullName"
              value={form.fullName}
              onChange={handleChange}
              placeholder="Jane Smith"
              required
              autoFocus
            />
          </label>

          <label className="field">
            <span>Email</span>
            <input
              type="email"
              name="email"
              value={form.email}
              onChange={handleChange}
              placeholder="you@example.com"
              required
            />
          </label>

          <label className="field">
            <span>Password</span>
            <input
              type="password"
              name="password"
              value={form.password}
              onChange={handleChange}
              placeholder="Min. 6 characters"
              required
            />
          </label>

          <label className="field">
            <span>Confirm password</span>
            <input
              type="password"
              name="confirm"
              value={form.confirm}
              onChange={handleChange}
              placeholder="Re-enter password"
              required
            />
          </label>

          <button type="submit" className="btn btn-primary btn-full" disabled={loading}>
            {loading ? 'Creating account…' : 'Sign up'}
          </button>
        </form>

        <p className="auth-footer">
          Already have an account? <Link to="/login">Log in</Link>
        </p>
      </div>
    </div>
  );
}
