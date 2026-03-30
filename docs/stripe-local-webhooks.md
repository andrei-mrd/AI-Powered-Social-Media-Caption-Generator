# Stripe local webhooks

Use the Stripe CLI to forward checkout and subscription events to the API while testing plan upgrades locally.

## 1. Install and authenticate the Stripe CLI

Install the CLI from the official Stripe docs:

- [Stripe CLI install](https://docs.stripe.com/stripe-cli/install)

Then authenticate once:

```bash
stripe login
```

If you do not want to use the browser flow, Stripe also supports:

```bash
stripe login --interactive
```

## 2. Start the app locally

Run the API on `http://localhost:5000` and the frontend on `http://localhost:5173`.

## 3. Start webhook forwarding

From the repo root:

```bash
./scripts/stripe-listen.sh
```

This forwards:

- `checkout.session.completed`
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`

to:

```text
http://localhost:5000/api/payments/webhook
```

The Stripe CLI prints a signing secret like `whsec_...`.

Put that value in the API environment file:

```dotenv
Stripe__WebhookSecret=whsec_your_value_here
```

Use:

- `src/CaptionGen.Api/.env`

Restart the API after changing the webhook secret.

## 4. Test a real upgrade flow

1. Open the dashboard in the frontend.
2. Choose a paid plan.
3. Complete Stripe Checkout with a Stripe test card.
4. Wait for the success page to confirm the entitlement update.

The backend now listens for both the Checkout completion event and later subscription update events, so plan upgrades made through Stripe subscription changes are reflected in the app.
