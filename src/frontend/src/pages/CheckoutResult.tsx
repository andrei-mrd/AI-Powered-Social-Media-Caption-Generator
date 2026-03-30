import { useEffect, useRef, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { ShieldCheck, Loader } from 'lucide-react';
import { useEntitlements } from '../hooks/useEntitlements';
import './CheckoutResult.css';

type Variant = 'success' | 'cancel';

function useVariant(): Variant {
  const { pathname } = useLocation();
  return pathname.includes('success') ? 'success' : 'cancel';
}

export default function CheckoutResult() {
  const variant = useVariant();
  const navigate = useNavigate();
  const isSuccess = variant === 'success';

  // The plan slug stored in sessionStorage before the Stripe redirect.
  const pendingSlugRef = useRef(sessionStorage.getItem('pendingPlanSlug') ?? '');

  // Poll every 3 s on success so we catch the plan update as soon as the
  // Stripe webhook fires and AssignPlanAsync updates the database.
  const { data: entitlement, loading } = useEntitlements(isSuccess ? 3000 : 0);

  // True only when the DB entitlement matches the plan that was purchased.
  const planActivated =
    isSuccess &&
    !!entitlement &&
    (!pendingSlugRef.current ||
      entitlement.planSlug.toLowerCase() === pendingSlugRef.current.toLowerCase());

  // Clear sessionStorage once confirmed so a hard-refresh doesn't re-show it.
  useEffect(() => {
    if (planActivated) {
      sessionStorage.removeItem('pendingPlanSlug');
    }
  }, [planActivated]);

  const [timedOut, setTimedOut] = useState(false);
  useEffect(() => {
    if (!isSuccess || planActivated) return;
    const id = window.setTimeout(() => setTimedOut(true), 60_000);
    return () => window.clearTimeout(id);
  }, [isSuccess, planActivated]);

  const title = isSuccess ? 'Payment confirmed' : 'Payment canceled';
  const subtitle = isSuccess
    ? planActivated
      ? 'Your plan is now active. Head to the dashboard to start creating.'
      : timedOut
        ? 'Taking longer than expected — your plan will update automatically once confirmed.'
        : 'Activating your plan — this usually takes a few seconds…'
    : 'No charges were made. You can retry checkout anytime.';

  const handleGoToDashboard = () => {
    navigate('/dashboard', { state: { planJustUpdated: planActivated } });
  };

  return (
    <div className="checkout-result">
      <div className="checkout-card">
        <div className={`status-badge ${variant}`}>
          {isSuccess ? 'Success' : 'Canceled'}
        </div>
        <h1>{title}</h1>

        {isSuccess && (
          <div className="plan-confirmation">
            {planActivated ? (
              <div className="plan-confirmed">
                <ShieldCheck size={20} className="plan-confirmed-icon" />
                <div>
                  <strong>{entitlement!.planName}</strong>
                  <span>
                    {entitlement!.captionGenerationsPerMonth} captions/mo
                    &nbsp;·&nbsp;
                    {entitlement!.mediaAssetsLimit} media assets
                  </span>
                </div>
              </div>
            ) : !timedOut && loading ? (
              <div className="plan-pending">
                <Loader size={16} className="spin" />
                <span>Waiting for plan confirmation…</span>
              </div>
            ) : null}
          </div>
        )}

        <p>{subtitle}</p>
        <div className="actions">
          {isSuccess ? (
            <button type="button" className="btn-primary" onClick={handleGoToDashboard}>
              Go to dashboard
            </button>
          ) : (
            <Link className="btn-primary" to="/dashboard">Go to dashboard</Link>
          )}
          <Link className="ghost-btn" to="/">Back to home</Link>
        </div>
      </div>
    </div>
  );
}
