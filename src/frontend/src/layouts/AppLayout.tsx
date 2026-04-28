import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { motion, type Variants } from 'framer-motion';
import { LayoutDashboard, LogOut, ChevronRight, History, PenTool, Settings } from 'lucide-react';
import { useAuth } from '../context/useAuth';
import './AppLayout.css';

const sidebarVariants: Variants = {
  hidden: { x: -20, opacity: 0 },
  visible: { 
    x: 0, 
    opacity: 1,
    transition: { 
      duration: 0.5, 
      ease: "easeOut",
      staggerChildren: 0.1 
    }
  }
};

const itemVariants: Variants = {
  hidden: { x: -10, opacity: 0 },
  visible: { x: 0, opacity: 1 }
};

export default function AppLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/');
  };

  return (
    <div className="app-shell bg-gradient-mesh">
      <motion.nav 
        className="glass-sidebar"
        initial="hidden"
        animate="visible"
        variants={sidebarVariants}
      >
        <div className="brand">
          <div className="brand-dot" />
          <span className="text-gradient">CaptionGen</span>
        </div>

        <div className="nav-links">
          <motion.span variants={itemVariants} className="nav-section">Application</motion.span>
          <motion.div variants={itemVariants}>
            <NavLink to="/dashboard" className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}>
              <LayoutDashboard size={18} />
              <span>Overview</span>
            </NavLink>
          </motion.div>

          <motion.span variants={itemVariants} className="nav-section mt-4">Tools</motion.span>
          <motion.div variants={itemVariants}>
            <NavLink to="/create-post" className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}>
              <PenTool size={18} />
              <span>Content Flow</span>
            </NavLink>
          </motion.div>
          <motion.div variants={itemVariants}>
            <NavLink to="/posts" className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}>
              <History size={18} />
              <span>Library</span>
            </NavLink>
          </motion.div>
          <motion.div variants={itemVariants}>
            <NavLink to="/settings" className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}>
              <Settings size={18} />
              <span>Connections</span>
            </NavLink>
          </motion.div>
        </div>

        <motion.div variants={itemVariants} className="nav-footer">
          {user && (
            <div className="user-profile-card">
              <div className="user-avatar">{user.email?.charAt(0).toUpperCase() || 'U'}</div>
              <div className="user-info">
                <span className="user-email">{user.email}</span>
                <span className="user-role">Creator</span>
              </div>
            </div>
          )}
          <button className="logout-btn" onClick={handleLogout}>
            <LogOut size={18} />
            <span>Sign out</span>
            <ChevronRight size={16} className="ml-auto opacity-50" />
          </button>
        </motion.div>
      </motion.nav>

      <main className="content-area animate-fade-in">
        <Outlet />
      </main>
    </div>
  );
}
