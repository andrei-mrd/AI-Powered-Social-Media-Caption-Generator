import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { PenTool, LayoutDashboard, LogOut, ChevronRight, History, Image } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import './AppLayout.css';

export default function AppLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/');
  };

  return (
    <div className="app-shell animate-fade-in">
      <nav className="glass-sidebar">
        <div className="brand">
          <div className="brand-dot" />
          <span>CaptionGen</span>
        </div>

        <div className="nav-links">
          <span className="nav-section">Application</span>
          <NavLink to="/dashboard" className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}>
            <LayoutDashboard size={18} />
            <span>Dashboard</span>
          </NavLink>

          <span className="nav-section mt-4">Tools</span>
          <NavLink to="/generate" className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}>
            <PenTool size={18} />
            <span>Generate Post</span>
          </NavLink>
          <NavLink to="/posts" className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}>
            <History size={18} />
            <span>My Posts</span>
          </NavLink>
          <NavLink to="/media" className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}>
            <Image size={18} />
            <span>Media Library</span>
          </NavLink>
        </div>

        <div className="nav-footer">
          {user && (
            <div className="user-info">
              <span className="user-email">{user.email}</span>
            </div>
          )}
          <button className="logout-btn" onClick={handleLogout}>
            <LogOut size={18} />
            <span>Sign out</span>
            <ChevronRight size={16} className="ml-auto" />
          </button>
        </div>
      </nav>

      <main className="content-area">
        <Outlet />
      </main>
    </div>
  );
}
