import { Link, useLocation } from 'react-router-dom';
import './CheckoutResult.css';

type Variant = 'success' | 'cancel';

function useVariant(): Variant {
  const { pathname } = useLocation();
  return pathname.includes('success') ? 'success' : 'cancel';
}

export default function CheckoutResult() {
  const variant = useVariant();
  const title = variant === 'success' ? 'Payment confirmed' : 'Payment canceled';
  const subtitle =
    variant === 'success'
      ? 'Your plan will be applied shortly. You can refresh entitlements from the dashboard.'
      : 'No charges were made. You can retry checkout anytime.';

  return (
    <div className="checkout-result">
      <div className="checkout-card">
        <div className={`status-badge ${variant}`}>
          {variant === 'success' ? 'Success' : 'Canceled'}
        </div>
        <h1>{title}</h1>
        <p>{subtitle}</p>
        <div className="actions">
          <Link className="btn-primary" to="/dashboard">Go to dashboard</Link>
          <Link className="ghost-btn" to="/">Back to home</Link>
        </div>
      </div>
    </div>
  );
}
