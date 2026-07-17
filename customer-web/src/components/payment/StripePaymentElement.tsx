'use client';

import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Elements, PaymentElement, useElements, useStripe } from '@stripe/react-stripe-js';
import { loadStripe } from '@stripe/stripe-js';
import { marketplaceApi } from '@/lib/api/apiClient';

type Props = {
  publishableKey: string;
  userId: string;
  packageId?: string;
  packageCode?: string;
  amountLabel: string;
  disabled?: boolean;
  onSuccess: (paymentIntentId: string) => Promise<void> | void;
  onError: (message: string) => void;
};

function StripeCheckoutForm({ amountLabel, disabled, onSuccess, onError }: Pick<Props, 'amountLabel' | 'disabled' | 'onSuccess' | 'onError'>) {
  const stripe = useStripe();
  const elements = useElements();
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!stripe || !elements || submitting || disabled) return;
    setSubmitting(true);
    onError('');
    const result = await stripe.confirmPayment({
      elements,
      confirmParams: { return_url: `${window.location.origin}/post` },
      redirect: 'if_required',
    });
    if (result.error) {
      onError(result.error.message ?? 'Stripe could not process the payment.');
      setSubmitting(false);
      return;
    }
    if (result.paymentIntent?.status !== 'succeeded') {
      onError(`Stripe payment status: ${result.paymentIntent?.status ?? 'unknown'}.`);
      setSubmitting(false);
      return;
    }
    try {
      await onSuccess(result.paymentIntent.id);
    } finally {
      setSubmitting(false);
    }
  }

  return <form className="stripe-elements-form-v2" onSubmit={handleSubmit}>
    <PaymentElement options={{ layout: 'tabs' }} />
    <button className="primary-button gateway-pay-button-v2" type="submit" disabled={!stripe || !elements || submitting || disabled}>
      {submitting || disabled ? 'Processing…' : `Pay ${amountLabel}`}
    </button>
    <small className="gateway-secure-copy-v2">Payment details are securely collected and tokenized by Stripe.</small>
  </form>;
}

export function StripePaymentElement(props: Props) {
  const stripePromise = useMemo(() => loadStripe(props.publishableKey), [props.publishableKey]);
  const [clientSecret, setClientSecret] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setClientSecret('');
    marketplaceApi.createStripePaymentIntent({ userId: props.userId, packageId: props.packageId, packageCode: props.packageCode })
      .then((result) => { if (!cancelled) setClientSecret(result.clientSecret); })
      .catch((error) => { if (!cancelled) props.onError(error?.message ?? 'Could not initialize Stripe.'); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [props.userId, props.packageId, props.packageCode]);

  if (loading) return <div className="gateway-loading-v2">Loading Stripe secure checkout…</div>;
  if (!clientSecret) return <div className="gateway-error-v2">Stripe checkout could not be initialized.</div>;

  return <Elements stripe={stripePromise} options={{ clientSecret, appearance: { theme: 'stripe' } }}>
    <StripeCheckoutForm amountLabel={props.amountLabel} disabled={props.disabled} onSuccess={props.onSuccess} onError={props.onError} />
  </Elements>;
}
