#!/usr/bin/env bash
set -euo pipefail

stripe_bin="${STRIPE_BIN:-}"

if [[ -z "$stripe_bin" ]]; then
  if command -v stripe >/dev/null 2>&1; then
    stripe_bin="$(command -v stripe)"
  elif [[ -x "$HOME/.local/bin/stripe" ]]; then
    stripe_bin="$HOME/.local/bin/stripe"
  fi
fi

if [[ -z "$stripe_bin" ]]; then
  echo "Stripe CLI not found. Install it first, then rerun this script." >&2
  exit 1
fi

forward_to="${1:-http://localhost:5000/api/payments/webhook}"
events="${STRIPE_WEBHOOK_EVENTS:-checkout.session.completed,customer.subscription.created,customer.subscription.updated,customer.subscription.deleted}"

echo "Forwarding Stripe events to: $forward_to"
echo "Copy the signing secret from the Stripe CLI output into Stripe__WebhookSecret in your API .env file."

exec "$stripe_bin" listen \
  --events "$events" \
  --forward-to "$forward_to"
