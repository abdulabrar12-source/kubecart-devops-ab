import { Link, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export default function Navbar() {
  const { auth, logout } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/');
  }

  return (
    <nav className="navbar">
      <div className="navbar-inner">
        <Link to="/" className="navbar-brand">🛒 KubeCart</Link>

        <div className="navbar-links">
          <NavLink to="/" end className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Shop
          </NavLink>

          {auth && (
            <>
              <NavLink to="/cart" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
                Cart
              </NavLink>
              <NavLink to="/orders" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
                My Orders
              </NavLink>
            </>
          )}

          {auth?.isAdmin && (
            <>
              <NavLink to="/admin/products" className={({ isActive }) => isActive ? 'nav-link nav-link-admin active' : 'nav-link nav-link-admin'}>
                Products
              </NavLink>
              <NavLink to="/admin/orders" className={({ isActive }) => isActive ? 'nav-link nav-link-admin active' : 'nav-link nav-link-admin'}>
                Orders
              </NavLink>
            </>
          )}
        </div>

        <div className="navbar-auth">
          {auth ? (
            <>
              <span className="navbar-user">Hi, {auth.fullName.split(' ')[0]}</span>
              <button onClick={handleLogout} className="btn btn-outline btn-sm">Log out</button>
            </>
          ) : (
            <>
              <Link to="/login" className="btn btn-outline btn-sm">Log in</Link>
              <Link to="/register" className="btn btn-primary btn-sm">Sign up</Link>
            </>
          )}
        </div>
      </div>
    </nav>
  );
}
