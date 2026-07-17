'use client';

import { useEffect, useRef, useState } from 'react';
import { marketplaceApi } from '@/lib/api/apiClient';

declare global {
  interface Window { paypal?: any; }
}

type Props = {
  clientId: string;
  currency: string;
  userId: string;
  packageId?: string;
  packageCode?: string;
  disabled?: boolean;
  onSuccess: (orderId: string) => Promise<void> | void;
  onError: (message: string) => void;
};

export function PayPalButtons(props: Props) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    const scriptId = 'paypal-js-sdk-vunoca';
    const render = () => {
      if (cancelled || !containerRef.current || !window.paypal) return;
      containerRef.current.innerHTML = '';
      window.paypal.Buttons({
        style: { layout: 'vertical', shape: 'rect', label: 'paypal', height: 48 },
        createOrder: async () => {
          props.onError('');
          const result = await marketplaceApi.createPayPalOrder({ userId: props.userId, packageId: props.packageId, packageCode: props.packageCode });
          return result.orderId;
        },
        onApprove: async (data: { orderID: string }) => {
          const capture = await marketplaceApi.capturePayPalOrder(data.orderID);
          if ((capture.status ?? '').toUpperCase() !== 'COMPLETED') throw new Error(`PayPal status: ${capture.status ?? 'unknown'}`);
          await props.onSuccess(data.orderID);
        },
        onCancel: () => props.onError('PayPal checkout was cancelled.'),
        onError: (error: any) => props.onError(error?.message ?? 'PayPal could not process the payment.'),
        onInit: (_data: any, actions: any) => {
          if (props.disabled) actions.disable(); else actions.enable();
        },
      }).render(containerRef.current).then(() => { if (!cancelled) setLoading(false); });
    };

    const existing = document.getElementById(scriptId) as HTMLScriptElement | null;
    if (existing) {
      if (window.paypal) render(); else existing.addEventListener('load', render, { once: true });
    } else {
      const script = document.createElement('script');
      script.id = scriptId;
      script.src = `https://www.paypal.com/sdk/js?client-id=${encodeURIComponent(props.clientId)}&currency=${encodeURIComponent(props.currency)}&intent=capture&components=buttons`;
      script.async = true;
      script.onload = render;
      script.onerror = () => props.onError('Could not load the PayPal checkout SDK.');
      document.head.appendChild(script);
    }
    return () => { cancelled = true; if (containerRef.current) containerRef.current.innerHTML = ''; };
  }, [props.clientId, props.currency, props.userId, props.packageId, props.packageCode, props.disabled]);

  return <div className="paypal-official-v2">
    {loading ? <div className="gateway-loading-v2">Loading PayPal checkout…</div> : null}
    <div ref={containerRef} />
    <small className="gateway-secure-copy-v2">You will approve the payment in PayPal’s secure checkout.</small>
  </div>;
}
