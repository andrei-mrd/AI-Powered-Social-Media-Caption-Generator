import { Outlet, NavLink } from 'react-router-dom';
import { PenTool, Home, LayoutDashboard, LogIn, ChevronRight } from 'lucide-react';
import './MainLayout.css';

export default function MainLayout() {
  return (
    <div className="app-shell animate-fade-in">
      <nav className="glass-sidebar">
        <div className="brand">
          <div className="brand-dot" />
          <span>CaptionGen</span>
        </div>
        
        <div className="nav-links">
          <span className="nav-section">Application</span>
          <NavLink to="/" className={({isActive}) => isActive ? "nav-item active" : "nav-item"}>
            <Home size={18} />
            <span>Overview</span>
          </NavLink>
          
          <NavLink to="/dashboard" className={({isActive}) => isActive ? "nav-item active" : "nav-item"}>
            <LayoutDashboard size={18} />
            <span>Dashboard</span>
          </NavLink>

          <span className="nav-section mt-4">Tools</span>
          <NavLink to="/generate" className={({isActive}) => isActive ? "nav-item active" : "nav-item"}>
            <PenTool size={18} />
            <span>Generate Post</span>
          </NavLink>
        </div>

        <div className="nav-footer">
          <NavLink to="/login" className="login-btn">
            <LogIn size={18} />
            <span>Sign In</span>
            <ChevronRight size={16} className="ml-auto" />
          </NavLink>
        </div>
      </nav>

      <main className="content-area">
        <Outlet />
      </main>
    </div>
  );
}
