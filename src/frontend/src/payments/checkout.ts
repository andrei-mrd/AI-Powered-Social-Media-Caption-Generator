import { loadStripe } from '@stripe/stripe-js';
import { readApiError, normalizeError } from '../utils/api';

export async function startStripeCheckout(planSlug: string): Promise<void> {
  const publishableKey = import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY as string | undefined;
  if (!publishableKey) {
    throw new Error('Stripe publishable key is not configured.');
  }

  const stripe = await loadStripe(publishableKey);
  if (!stripe) {
    throw new Error('Unable to initialize Stripe.');
  }

  const res = await fetch('/api/payments/checkout-session', {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ planSlug })
  });

  if (!res.ok) {
    throw new Error(normalizeError(await readApiError(res, 'Unable to start checkout')));
  }

  const data: { sessionId: string; url?: string } = await res.json();

  if (data.url) {
    sessionStorage.setItem('pendingPlanSlug', planSlug);
    window.location.assign(data.url);
    return;
  }

  // Fallback: if URL not provided, try classic redirect when available.
  const redirect = (stripe as any).redirectToCheckout as
    | undefined
    | ((opts: { sessionId: string }) => Promise<{ error?: { message?: string } }>);

  if (!redirect) {
    throw new Error('Stripe checkout URL missing and redirect API unavailable.');
  }

  sessionStorage.setItem('pendingPlanSlug', planSlug);
  const { error } = await redirect({ sessionId: data.sessionId });
  if (error?.message) throw new Error(error.message);
}
