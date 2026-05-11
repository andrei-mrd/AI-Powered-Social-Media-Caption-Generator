import { useEffect, useState } from 'react';
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
  const [pendingSlug] = useState(() => sessionStorage.getItem('pendingPlanSlug') ?? '');

  // Poll every 3 s on success so we catch the plan update as soon as the
  // Stripe webhook fires and AssignPlanAsync updates the database.
  const { data: entitlement, loading } = useEntitlements(isSuccess ? 3000 : 0);

  // True only when the DB entitlement matches the plan that was purchased.
  const planActivated =
    isSuccess &&
    !!entitlement &&
    (!pendingSlug ||
      entitlement.planSlug.toLowerCase() === pendingSlug.toLowerCase());

  // Clear sessionStorage once confirmed so a hard-refresh doesn't re-show it.
  useEffect(() => {
    if (planActivated) {
      sessionStorage.removeItem('pendingPlanSlug');
    }
  }, [planActivated]);

  const [timedOut, setTimedOut] = useState(false);
  useEffect(() => {
    if (!isSuccess || planActivated) return;
    const id = globalThis.setTimeout(() => setTimedOut(true), 60_000);
    return () => globalThis.clearTimeout(id);
  }, [isSuccess, planActivated]);

  const title = isSuccess ? 'Payment confirmed' : 'Payment canceled';
  let subtitle = 'No charges were made. You can retry checkout anytime.';
  if (isSuccess && planActivated) {
    subtitle = 'Your plan is now active. Head to the dashboard to start creating.';
  } else if (isSuccess && timedOut) {
    subtitle = 'Taking longer than expected — your plan will update automatically once confirmed.';
  } else if (isSuccess) {
    subtitle = 'Activating your plan — this usually takes a few seconds…';
  }

  let planConfirmation = null;
  if (planActivated && entitlement) {
    planConfirmation = (
      <div className="plan-confirmed">
        <ShieldCheck size={20} className="plan-confirmed-icon" />
        <div>
          <strong>{entitlement.planName}</strong>
          <span>
            {entitlement.captionGenerationsPerMonth} captions/mo
            &nbsp;·&nbsp;
            {entitlement.mediaAssetsLimit} media assets
          </span>
        </div>
      </div>
    );
  } else if (!timedOut && loading) {
    planConfirmation = (
      <div className="plan-pending">
        <Loader size={16} className="spin" />
        <span>Waiting for plan confirmation…</span>
      </div>
    );
  }

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
            {planConfirmation}
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
