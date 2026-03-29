import { useState } from 'react';
import { Link } from 'react-router-dom';
import { PenTool, Target, Hash, Zap, Sparkles, ShieldCheck, Activity, RefreshCw } from 'lucide-react';
import { useEntitlements } from '../hooks/useEntitlements';
import { normalizeError } from '../utils/api';
import { startStripeCheckout } from '../payments/checkout';
import './Dashboard.css';

export default function Dashboard() {
  const { data: entitlement, error: entitlementError, loading, refresh } = useEntitlements(20000);
  const [planSlug, setPlanSlug] = useState('influencer');
  const [checkoutError, setCheckoutError] = useState('');
  const [isCheckingOut, setIsCheckingOut] = useState(false);

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
    <div className="dashboard-container animate-fade-in">
      <header className="page-header">
        <div>
          <h1 className="page-title">Dashboard</h1>
          <p className="page-subtitle">Ship captions without losing your voice</p>
        </div>
      </header>

      <div className="bento-grid">
        <div className="bento-item welcome-box animate-slide-up">
          <span className="eyebrow">Welcome back</span>
          <h2>Keep posts feeling human</h2>
          <p>Rotate tones, test hooks, and keep captions concise without sounding generic.</p>
          <div className="action-row">
            <Link to="/generate" className="btn-primary">
              <PenTool size={18} /> Start generating
            </Link>
            <Link to="/create-post" className="ghost-btn">
              <Sparkles size={18} /> Make a post
            </Link>
          </div>
        </div>

        <div className="bento-item features-box animate-slide-up animate-delay-100">
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
        </div>

        <div className="bento-item info-box animate-slide-up animate-delay-200">
          <h4>Need inspiration?</h4>
          <p>Try giving us a rough idea, product name, or vibe—we will return 3 distinct angles with hashtags instantly.</p>
        </div>

        <div className="bento-item plan-box animate-slide-up animate-delay-150">
          <div className="plan-header">
            <div className="plan-icon"><ShieldCheck size={18} /></div>
            <div>
              <span className="eyebrow">Your plan</span>
              <h3>{entitlement?.planName ?? 'Loading…'}</h3>
              <p className="plan-subtitle">{entitlement?.planSlug ? entitlement.planSlug : ''}</p>
            </div>
            <button className="plan-refresh" type="button" onClick={refresh} disabled={loading}>
              <RefreshCw size={16} className={loading ? 'spin' : ''} /> Refresh
            </button>
          </div>
          {entitlementError ? (
            <p className="plan-error">{entitlementError}</p>
          ) : entitlement ? (
            <>
              <ul className="plan-list">
                <li><Activity size={14} /> Captions/month: {entitlement.captionGenerationsPerMonth} • used {entitlement.captionsUsedThisPeriod}</li>
                <li><Activity size={14} /> Media limit: {entitlement.mediaAssetsLimit} • used {entitlement.mediaUsedThisPeriod}</li>
                <li><Activity size={14} /> Seats: {entitlement.seatsIncluded}</li>
                <li><Activity size={14} /> Scheduling: {entitlement.schedulingEnabled ? 'On' : 'Off'}</li>
                <li><Activity size={14} /> Improve captions: {entitlement.aiImproveEnabled ? 'On' : 'Off'}</li>
                <li><Activity size={14} /> Active until: {formatDate(entitlement.activeUntilUtc)}</li>
              </ul>
              <div className="plan-checkout">
                <label className="plan-select-label">
                  Choose plan
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
                </label>
                <button
                  type="button"
                  className="btn-primary plan-checkout-btn"
                  onClick={handleCheckout}
                  disabled={isCheckingOut}
                >
                  {isCheckingOut ? 'Redirecting…' : 'Upgrade via Stripe'}
                </button>
              </div>
              {checkoutError && <p className="plan-error">{checkoutError}</p>}
            </>
          ) : (
            <p className="plan-loading">Fetching plan…</p>
          )}
        </div>

        <div className="bento-item highlight-box animate-slide-up animate-delay-300">
          <h4>Plan a series</h4>
          <p>Save prompt snippets for campaigns so every post stays aligned across platforms.</p>
        </div>
      </div>
    </div>
  );
}
