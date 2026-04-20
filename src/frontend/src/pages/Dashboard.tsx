import { useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { motion } from 'framer-motion';
import { PenTool, Target, Hash, Zap, Sparkles, ShieldCheck, Activity, RefreshCw, CheckCircle, ChevronRight } from 'lucide-react';
import { useEntitlements } from '../hooks/useEntitlements';
import { normalizeError } from '../utils/api';
import { startStripeCheckout } from '../payments/checkout';
import './Dashboard.css';

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { staggerChildren: 0.1 }
  }
};

const itemVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.5, ease: "easeOut" } }
};

export default function Dashboard() {
  const location = useLocation();
  const { data: entitlement, error: entitlementError, loading, refresh } = useEntitlements(20000);
  const [planSlug, setPlanSlug] = useState('influencer');
  const [checkoutError, setCheckoutError] = useState('');
  const [isCheckingOut, setIsCheckingOut] = useState(false);
  const [showPlanBanner, setShowPlanBanner] = useState(
    () => !!(location.state as { planJustUpdated?: boolean } | null)?.planJustUpdated
  );

  useEffect(() => {
    if (!showPlanBanner) return;
    const id = window.setTimeout(() => setShowPlanBanner(false), 5000);
    return () => window.clearTimeout(id);
  }, [showPlanBanner]);

  const formatDate = (value?: string | null) => {
    if (!value) return '—';
    const dt = new Date(value);
    return Number.isNaN(dt.getTime()) ? '—' : dt.toLocaleDateString();
  };

  const handleCheckout = async () => {
    setCheckoutError('');
    setIsCheckingOut(true);
    try {
      await startStripeCheckout(planSlug);
    } catch (err) {
      setCheckoutError(normalizeError(err, 'Unable to start checkout'));
    } finally {
      setIsCheckingOut(false);
    }
  };

  return (
    <motion.div 
      className="dashboard-container"
      initial="hidden"
      animate="visible"
      variants={containerVariants}
    >
      {showPlanBanner && (
        <motion.div variants={itemVariants} className="plan-updated-banner">
          <CheckCircle size={16} />
          {entitlement
            ? `Plan updated to ${entitlement.planName}`
            : 'Plan updated — fetching details…'}
        </motion.div>
      )}
      
      <motion.header variants={itemVariants} className="page-header">
        <div>
          <h1 className="page-title text-gradient">Dashboard</h1>
          <p className="page-subtitle">Ship captions without losing your voice</p>
        </div>
      </motion.header>

      <div className="bento-grid">
        <motion.div variants={itemVariants} className="bento-item welcome-box glass-panel">
          <div className="welcome-content">
            <span className="eyebrow text-gradient">Welcome back</span>
            <h2>Keep posts feeling human.</h2>
            <p>Rotate tones, test hooks, and keep captions concise without sounding generic.</p>
            <div className="action-row">
              <Link to="/generate" className="btn btn-primary">
                <PenTool size={18} /> Start Generating
              </Link>
              <Link to="/create-post" className="btn btn-secondary">
                <Sparkles size={18} /> Make a Post
              </Link>
            </div>
          </div>
          <div className="welcome-visual">
            <div className="floating-shape shape-1"></div>
            <div className="floating-shape shape-2"></div>
            <div className="floating-shape shape-3"></div>
          </div>
        </motion.div>

        <motion.div variants={itemVariants} className="bento-item features-box">
          <div className="feat-row">
            <div className="feat-icon-small"><Target size={16}/></div>
            <div className="feat-text">
              <strong>Tones</strong>
              <span>Presets for every mood</span>
            </div>
          </div>
          <div className="feat-row">
            <div className="feat-icon-small"><Zap size={16}/></div>
            <div className="feat-text">
              <strong>Length</strong>
              <span>Auto-optimized limits</span>
            </div>
          </div>
          <div className="feat-row">
            <div className="feat-icon-small"><Hash size={16}/></div>
            <div className="feat-text">
              <strong>Tags</strong>
              <span>Smart hashtag clusters</span>
            </div>
          </div>
        </motion.div>

        <motion.div variants={itemVariants} className="bento-item info-box highlight-box">
          <h4>Need inspiration?</h4>
          <p>Try giving us a rough idea, product name, or vibe—we will return 3 distinct angles with hashtags instantly.</p>
          <div className="view-templates-link">
            View Templates <ChevronRight size={14} />
          </div>
        </motion.div>

        <motion.div variants={itemVariants} className="bento-item plan-box glass-panel">
          <div className="plan-header">
            <div className="plan-title-wrapper">
              <div className="plan-icon"><ShieldCheck size={20} /></div>
              <div>
                <span className="eyebrow">Your plan</span>
                <h3>{entitlement?.planName ?? 'Loading…'}</h3>
                <p className="plan-subtitle">{entitlement?.planSlug ? entitlement.planSlug : ''}</p>
              </div>
            </div>
            <button className="plan-refresh" type="button" onClick={refresh} disabled={loading}>
              <RefreshCw size={14} className={loading ? 'spin' : ''} />
            </button>
          </div>
          {entitlementError ? (
            <p className="plan-error">{entitlementError}</p>
          ) : entitlement ? (
            <>
              <div className="plan-stats-grid">
                <div className="stat-card">
                  <span className="stat-label">Captions</span>
                  <div className="stat-value">{entitlement.captionsUsedThisPeriod} <span className="stat-max">/ {entitlement.captionGenerationsPerMonth}</span></div>
                  <div className="stat-bar"><div className="stat-fill" style={{width: `${Math.min(100, (entitlement.captionsUsedThisPeriod / entitlement.captionGenerationsPerMonth) * 100)}%`}}></div></div>
                </div>
                <div className="stat-card">
                  <span className="stat-label">Media Assets</span>
                  <div className="stat-value">{entitlement.mediaUsedThisPeriod} <span className="stat-max">/ {entitlement.mediaAssetsLimit}</span></div>
                  <div className="stat-bar"><div className="stat-fill blue" style={{width: `${Math.min(100, (entitlement.mediaUsedThisPeriod / entitlement.mediaAssetsLimit) * 100)}%`}}></div></div>
                </div>
              </div>
              
              <ul className="plan-list">
                <li><Activity size={14} /> Seats: <strong>{entitlement.seatsIncluded}</strong></li>
                <li><Activity size={14} /> Scheduling: <strong>{entitlement.schedulingEnabled ? 'On' : 'Off'}</strong></li>
                <li><Activity size={14} /> Auto-Improve: <strong>{entitlement.aiImproveEnabled ? 'On' : 'Off'}</strong></li>
                <li><Activity size={14} /> Renewal: <strong>{formatDate(entitlement.activeUntilUtc)}</strong></li>
              </ul>
              
              <div className="plan-checkout">
                <select
                  value={planSlug}
                  onChange={e => setPlanSlug(e.target.value)}
                  className="plan-select"
                  aria-label="Choose plan to purchase"
                  disabled={isCheckingOut}
                >
                  <option value="freelancer">Freelancer</option>
                  <option value="influencer">Influencer</option>
                  <option value="agency">Agency</option>
                </select>
                <button
                  type="button"
                  className="btn btn-primary plan-checkout-btn"
                  onClick={handleCheckout}
                  disabled={isCheckingOut}
                >
                  {isCheckingOut ? 'Redirecting…' : 'Upgrade Plan'}
                </button>
              </div>
              {checkoutError && <p className="plan-error">{checkoutError}</p>}
            </>
          ) : (
            <div className="skeleton-loader"></div>
          )}
        </motion.div>
      </div>
    </motion.div>
  );
}
