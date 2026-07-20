import { appConfig } from '@/lib/config';
import { clearSession, getSessionToken, getSessionUser, redirectToLogin } from '@/lib/api/session';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const isFormData = typeof FormData !== 'undefined' && init?.body instanceof FormData;
  const token = getSessionToken();
  const baseHeaders: Record<string, string> = isFormData ? {} : { 'Content-Type': 'application/json' };
  if (token) baseHeaders.Authorization = `Bearer ${token}`;
  const headers = { ...baseHeaders, ...(init?.headers as Record<string, string> | undefined ?? {}) };
  const response = await fetch(`${appConfig.apiBaseUrl}${path}`, { cache: 'no-store', ...init, headers });
  const payload = await response.json().catch(() => null);
  if (response.status === 401) {
    const errorCode = String(payload?.error?.code ?? payload?.code ?? '').trim().toLowerCase();
    const authenticateHeader = response.headers.get('www-authenticate') ?? '';
    const isAuthenticationFailure =
      authenticateHeader.toLowerCase().includes('bearer') ||
      ['unauthorized', 'invalidtoken', 'tokenexpired', 'sessionexpired'].includes(errorCode);

    // Do not destroy a valid customer session when a downstream gateway such as
    // Stripe returns HTTP 401 for an invalid/missing provider credential.
    if (isAuthenticationFailure) {
      clearSession();
      redirectToLogin();
      throw new Error('Session expired. Please sign in again.');
    }
  }
  if (!response.ok) throw new Error(payload?.error?.message ?? `API request failed: ${response.status}`);
  if (payload && typeof payload === 'object' && 'success' in payload) {
    if (!payload.success) throw new Error(payload?.error?.message ?? 'API error');
    return payload.data as T;
  }
  return payload as T;
}

export const DEMO_CUSTOMER_ID = '01990000-0000-7000-8000-000000000001';
export const DEMO_SELLER_ID = '01990000-0000-7000-8000-000000000002';

export function currentUserId() {
  return getSessionUser()?.id ?? '';
}

export type CategoryDto = { id: string; code?: string; iconKey?: string; parentCode?: string | null; name?: string; slug?: string; description?: string; count?: number };
export type PackageDto = { id?: string; code?: string; name: string; slug?: string; description?: string; price?: number | string | null; durationDays?: number | null; features?: string[] | string | null; isActive?: boolean; sortOrder?: number; badge?: string; };
export type PaymentProviderDto = { id?: string; code: string; name: string; type: 'Test' | 'Manual' | 'Stripe' | 'PayPal' | string; displayName?: string; currency?: string; isTestMode?: boolean; sortOrder?: number; publicConfigJson?: string; configured?: boolean; mode?: string; publishableKey?: string; clientId?: string; };
export type CityDto = { id: string; name: string; stateCode: string; countryCode: string; latitude?: number | null; longitude?: number | null; sortOrder?: number; selectionCount?: number };
export type UserLocationDto = { id: string; label?: string; addressLine: string; city: string; state?: string; postalCode?: string; country?: string; latitude?: number | null; longitude?: number | null; useCount?: number; lastUsedAt?: string | null; isDefault?: boolean; distanceMiles?: number | null };
export type AdvertisementDto = { id: string; campaignId?: string; campaignName?: string; placement: string; title: string; description?: string; desktopImageUrl?: string; mobileImageUrl?: string; targetUrl?: string; openInNewTab?: boolean; sortOrder?: number };

export type ListingDto = {
  id: string;
  title: string;
  price?: number | string | null;
  currency?: string;
  location?: string | null;
  addressLine?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
  locationSource?: string | null;
  locationPrecision?: string | null;
  hideExactLocation?: boolean | null;
  categoryId?: string;
  categoryCode?: string | null;
  categoryName?: string | null;
  categoryIconKey?: string | null;
  status?: string | null;
  moderationStatus?: string | null;
  isFeatured?: boolean;
  isUrgent?: boolean;
  isPinned?: boolean;
  packageCode?: string | null;
  packageStatus?: string | null;
  packageStartsAt?: string | null;
  packageEndsAt?: string | null;
  imageUrl?: string | null;
  description?: string | null;
  createdAt?: string | null;
  viewCount?: number;
  favoriteCount?: number;
  likeCount?: number;
  commentCount?: number;
  sellerVerified?: boolean;
  latitude?: number | string | null;
  longitude?: number | string | null;
  lat?: number | string | null;
  lng?: number | string | null;
  images?: { url?: string | null }[];
};
export type PagedListings = { items: ListingDto[]; totalItems: number; page?: number; pageSize?: number; totalPages?: number };
export type HomeFeed = { listings: ListingDto[]; featuredListings: ListingDto[]; recentListings: ListingDto[]; categories: CategoryDto[] };

export const apiClient = {
  get<T>(path: string) { return request<T>(path); },
  post<T>(path: string, body: unknown) { return request<T>(path, { method: 'POST', body: body instanceof FormData ? body : JSON.stringify(body) }); },
  put<T>(path: string, body: unknown) { return request<T>(path, { method: 'PUT', body: body instanceof FormData ? body : JSON.stringify(body) }); },
  delete<T>(path: string) { return request<T>(path, { method: 'DELETE' }); },
};

export const marketplaceApi = {
  home: (params?: { latitude?: number; longitude?: number }) => { const qs = new URLSearchParams(); if (params?.latitude != null) qs.set('latitude', String(params.latitude)); if (params?.longitude != null) qs.set('longitude', String(params.longitude)); return apiClient.get<HomeFeed>(`/feed/home${qs.toString() ? `?${qs}` : ''}`); },
  cities: () => apiClient.get<CityDto[]>('/locations/cities?state=CA&limit=1000'),
  citySelected: (id: string) => apiClient.post(`/locations/cities/${id}/selected`, {}),
  userLocations: (userId: string, coords?: { latitude?: number; longitude?: number }) => { const qs = new URLSearchParams({ userId }); if (coords?.latitude != null) qs.set('latitude', String(coords.latitude)); if (coords?.longitude != null) qs.set('longitude', String(coords.longitude)); return apiClient.get<UserLocationDto[]>(`/user-locations?${qs}`); },
  saveUserLocation: (payload: { userId: string; label?: string; addressLine: string; city: string; state?: string; postalCode?: string; country?: string; latitude?: number | null; longitude?: number | null }) => apiClient.post<UserLocationDto>('/user-locations', payload),
  markUserLocationUsed: (id: string, userId: string) => apiClient.post(`/user-locations/${id}/used`, { userId }),
  categories: () => apiClient.get<CategoryDto[]>('/categories'),
  listings: (params?: Record<string, string | number | undefined>) => {
    const qs = new URLSearchParams();
    Object.entries(params ?? {}).forEach(([key, value]) => { if (value !== undefined && value !== '') qs.set(key, String(value)); });
    return apiClient.get<PagedListings>(`/listings${qs.toString() ? `?${qs}` : ''}`);
  },
  myListings: (userId: string) => marketplaceApi.listings({ sellerId: userId, status: 'All', pageSize: 50 }),
  listing: (id: string) => apiClient.get<{ listing: ListingDto; media: { url: string }[]; comments: any[]; seller?: any; category?: any }>(`/listings/${id}`),
  createListing: (payload: { sellerId: string; categoryId: string; title: string; description: string; price: number; currency: string; location: string; packageCode?: string; packageId?: string; addressLine?: string; city?: string; state?: string; postalCode?: string; country?: string; latitude?: number | null; longitude?: number | null; locationSource?: string; locationPrecision?: string; hideExactLocation?: boolean }) => apiClient.post<ListingDto>('/listings', payload),
  packages: () => apiClient.get<PackageDto[]>('/packages'),
  paymentProviders: async () => {
    try {
      const data = await apiClient.get<{ items: PaymentProviderDto[] } | PaymentProviderDto[]>('/billing/providers');
      const items = Array.isArray(data) ? data : (data.items ?? []);
      if (items.length) return items;
    } catch {
      // Fall through to the public settings endpoint for compatibility with older backend deployments.
    }

    const settings = await apiClient.get<any>('/payment/settings');
    const items: PaymentProviderDto[] = [];
    if (settings?.stripe?.enabled) items.push({
      code: 'STRIPE', name: 'Stripe', type: 'Stripe', displayName: 'Credit / Debit Card',
      currency: settings.currency ?? 'USD', sortOrder: 10, configured: Boolean(settings.stripe.configured),
      publishableKey: settings.stripe.publishableKey ?? ''
    });
    if (settings?.paypal?.enabled) items.push({
      code: 'PAYPAL', name: 'PayPal', type: 'PayPal', displayName: 'PayPal',
      currency: settings.currency ?? 'USD', sortOrder: 20, configured: Boolean(settings.paypal.configured),
      clientId: settings.paypal.clientId ?? '', mode: settings.paypal.mode ?? 'Sandbox'
    });
    if (settings?.manual?.enabled) items.push({
      code: 'MANUAL', name: 'Manual', type: 'Manual', displayName: 'Manual/Test payment',
      currency: settings.currency ?? 'USD', sortOrder: 90, configured: true, isTestMode: true
    });
    return items;
  },
  ads: async (placement: string) => { const data = await apiClient.get<{ items: AdvertisementDto[] } | AdvertisementDto[]>(`/ads?placement=${encodeURIComponent(placement)}`); return Array.isArray(data) ? data : (data.items ?? []); },
  adsPlacements: async (placements?: string[]) => { const qs = placements?.length ? `?placements=${encodeURIComponent(placements.join(','))}` : ''; return apiClient.get<Record<string, AdvertisementDto[]>>(`/ads/placements${qs}`); },
  adImpression: (creativeId: string) => apiClient.post(`/ads/${creativeId}/impression`, {}),
  adClick: (creativeId: string) => apiClient.post(`/ads/${creativeId}/click`, {}),
  uploadMedia: (formData: FormData) => apiClient.post<{ id: string; url: string; fileName: string; listingId?: string }>('/media/upload', formData),
  favorite: (id: string, userId = currentUserId()) => apiClient.post(`/listings/${id}/favorite`, { userId }),
  like: (id: string, userId = currentUserId()) => apiClient.post(`/listings/${id}/like`, { userId }),
  messageSeller: (id: string, body: string, userId = currentUserId() || DEMO_CUSTOMER_ID) => apiClient.post(`/listings/${id}/message`, { buyerId: userId, body }),
  favorites: (userId: string) => apiClient.get<{ items: ListingDto[]; totalItems: number }>(`/favorites?userId=${userId}`),
  notifications: (userId: string, params?: { type?: string; unreadOnly?: boolean }) => { const qs = new URLSearchParams({ userId }); if (params?.type && params.type !== 'All') qs.set('type', params.type); if (params?.unreadOnly) qs.set('unreadOnly', 'true'); return apiClient.get<{ items: any[]; unread: number; totalItems?: number }>(`/notifications?${qs}`); },
  markNotificationRead: (id: string, userId: string) => apiClient.post(`/notifications/${id}/read?userId=${userId}`, {}),
  markAllNotificationsRead: (userId: string) => apiClient.post('/notifications/read-all', { userId }),
  deleteNotification: (id: string, userId: string) => apiClient.delete(`/notifications/${id}?userId=${userId}`),
  conversations: (userId: string) => apiClient.get<{ items: any[] }>(`/messages?userId=${userId}`),
  conversationThread: (conversationId: string, userId = currentUserId() || DEMO_CUSTOMER_ID) => apiClient.get<any>(`/messages/${conversationId}?userId=${userId}`),
  sendConversationMessage: (conversationId: string, body: string, userId = currentUserId() || DEMO_CUSTOMER_ID) => apiClient.post<any>(`/messages/${conversationId}`, { senderId: userId, body }),
  markConversationRead: (conversationId: string, userId = currentUserId() || DEMO_CUSTOMER_ID) => apiClient.post<any>(`/messages/${conversationId}/read`, { userId }),
  billing: (userId = currentUserId()) => apiClient.get<{ orders: any[]; payments: any[]; invoices: any[] }>(`/billing?userId=${userId}`),
  checkout: (payload: { userId: string; listingId?: string; packageId?: string; packageCode?: string; amount: number; paymentMethod?: string; paymentToken?: string; providerCode?: string; providerStatus?: string; providerPayload?: string }) => apiClient.post<any>('/billing/checkout', payload),
  createStripePaymentIntent: (payload: { userId: string; packageId?: string; packageCode?: string }) => apiClient.post<{ paymentIntentId: string; clientSecret: string; amount: number; currency: string }>('/billing/stripe/payment-intent', payload),
  createPayPalOrder: (payload: { userId: string; packageId?: string; packageCode?: string }) => apiClient.post<{ orderId: string }>('/billing/paypal/orders', payload),
  capturePayPalOrder: (orderId: string) => apiClient.post<{ orderId: string; status: string }>(`/billing/paypal/orders/${encodeURIComponent(orderId)}/capture`, {}),
  me: (userId: string) => apiClient.get<any>(`/users/me?userId=${userId}`),
  saveMe: (userId: string, payload: any) => apiClient.put<any>(`/users/me?userId=${userId}`, payload),
  sendPhoneVerification: (userId: string) => apiClient.post<any>('/auth/send-phone-verification', { userId }),
  verifyPhone: (userId: string, code: string) => apiClient.post<any>('/auth/verify-phone', { userId, code }),
};
