'use client';

import { useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import { marketplaceApi, type CategoryDto, type PackageDto, type PaymentProviderDto } from '@/lib/api/apiClient';
import { getSessionUser } from '@/lib/api/session';
import { Icon } from '@/components/ui/Icon';
import { useI18n } from '@/lib/i18n/client';

const steps = [
  { id: 1, title: 'Details', hint: 'Title & category' },
  { id: 2, title: 'Photos', hint: 'Add gallery' },
  { id: 3, title: 'Package', hint: 'Price & promotion' },
  { id: 4, title: 'Payment', hint: 'Checkout' },
  { id: 5, title: 'Review', hint: 'Publish' },
];

type PreviewPhoto = { file: File; url: string };

const defaultPackages: PackageDto[] = [
  { code: 'FREE', name: 'Free', description: 'Basic listing in local search.', price: 0, durationDays: 30, features: ['Standard placement', 'Up to 8 photos', 'Buyer messages'] },
  { code: 'FEATURED', name: 'Featured', description: 'Highlight your listing in feed and detail pages.', price: 9.99, durationDays: 7, badge: 'Popular', features: ['Featured badge', 'Promoted slider', 'More visibility'] },
  { code: 'URGENT', name: 'Urgent', description: 'Add urgent styling for faster buyer attention.', price: 14.99, durationDays: 7, badge: 'Fast sale', features: ['Urgent badge', 'Priority visual style', 'Top search boost'] },
  { code: 'PREMIUM', name: 'Premium', description: 'Best exposure across home, search, and promoted cards.', price: 29.99, durationDays: 14, badge: 'Best value', features: ['Premium card', 'Sponsored rail', 'Top carousel'] },
];

const defaultPaymentProviders: PaymentProviderDto[] = [
  { code: 'TEST', name: 'TEST', type: 'TEST', displayName: 'Manual/Test payment', isTestMode: true },
];

function providerIcon(type?: string) {
  const t = (type ?? '').toLowerCase();
  if (t.includes('manual') || t.includes('test')) return '✓';
  return '✓';
}

function isRealPaymentProvider(provider?: PaymentProviderDto | null) {
  const value = `${provider?.type ?? ''} ${provider?.code ?? ''} ${provider?.name ?? ''}`.toLowerCase();
  return value.includes('stripe') || value.includes('paypal');
}

function packageCode(pkg?: PackageDto | null) {
  return (pkg?.code ?? pkg?.slug ?? pkg?.name ?? 'FREE').toString().trim().toUpperCase().replace(/\s+/g, '_');
}

function packagePriceNumber(pkg?: PackageDto | null) {
  return Number(pkg?.price ?? 0) || 0;
}

function packagePrice(pkg: PackageDto, freeLabel = 'Free') {
  const value = packagePriceNumber(pkg);
  return value > 0 ? `$${value.toFixed(value % 1 === 0 ? 0 : 2)}` : freeLabel;
}

function packageDurationDays(pkg?: PackageDto | null) {
  const raw = Number(pkg?.durationDays ?? 0);
  return Number.isFinite(raw) && raw > 0 ? Math.ceil(raw) : 0;
}

function packageDurationText(pkg: PackageDto | null | undefined, t?: (key: string) => string) {
  const days = packageDurationDays(pkg);
  if (!days) return t ? t('noExpirationSet') : 'No expiration set';
  if (days === 1) return t ? t('oneDayListing') : '1 day listing';
  return `${days} ${t ? t('daysListing') : 'days listing'}`;
}

function packageFeatures(pkg: PackageDto) {
  if (Array.isArray(pkg.features)) return pkg.features;
  if (typeof pkg.features === 'string' && pkg.features.trim()) {
    return pkg.features.split(/[|,\n]/).map(x => x.trim()).filter(Boolean);
  }
  return ['Local search visibility', 'Buyer messages', 'Listing management'];
}

function buildLocationLabel(city: string, state: string, postalCode: string, fallback: string) {
  const parts = [city, state, postalCode].map(x => x.trim()).filter(Boolean);
  return parts.length ? parts.join(', ') : fallback.trim();
}

function mapsQuery(location: string, lat?: number | null, lng?: number | null) {
  if (lat != null && lng != null) return `${lat},${lng}`;
  return location || 'San Jose, CA';
}

type PostForm = {
  title: string;
  categoryId: string;
  condition: string;
  description: string;
  price: string;
  location: string;
  addressLine: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  latitude: string;
  longitude: string;
  locationSource: string;
  locationPrecision: string;
  hideExactLocation: boolean;
  packageCode: string;
  contactPreference: string;
};

export default function PostListingPage() {
  const router = useRouter();
  const { t, category, packageLabel, paymentProviderLabel } = useI18n();
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [packages, setPackages] = useState<PackageDto[]>([]);
  const [packagesLoading, setPackagesLoading] = useState(true);
  const [paymentProviders, setPaymentProviders] = useState<PaymentProviderDto[]>(defaultPaymentProviders);
  const [selectedProviderCode, setSelectedProviderCode] = useState('TEST');
  const [saved, setSaved] = useState('');
  const [savedId, setSavedId] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [step, setStep] = useState(1);
  const [photos, setPhotos] = useState<PreviewPhoto[]>([]);
  const [payment, setPayment] = useState({
    cardName: '',
    cardNumber: '',
    expiry: '',
    cvc: '',
    zip: '',
    testStatus: 'success',
    testReference: '',
    paypalEmail: '',
  });
  const [form, setForm] = useState<PostForm>({
    title: '',
    categoryId: '',
    condition: 'Like New',
    description: '',
    price: '',
    location: 'San Jose, CA',
    addressLine: '',
    city: 'San Jose',
    state: 'CA',
    postalCode: '',
    country: 'US',
    latitude: '37.3382',
    longitude: '-121.8863',
    locationSource: 'Manual',
    locationPrecision: 'ApproximateCity',
    hideExactLocation: true,
    packageCode: 'FREE',
    contactPreference: 'Message in app',
  });

  useEffect(() => {
    marketplaceApi.categories()
      .then((items) => {
        setCategories(items);
        setForm((current) => current.categoryId || !items[0]?.id ? current : { ...current, categoryId: items[0].id });
      })
      .catch(() => setCategories([]));

    marketplaceApi.packages()
      .then((items) => {
        const active = (items ?? []).filter((p) => p.isActive !== false);
        setPackages(active.length ? active : defaultPackages);
        setForm((current) => current.packageCode ? current : { ...current, packageCode: packageCode(active[0] ?? defaultPackages[0]) });
      })
      .catch(() => setPackages(defaultPackages))
      .finally(() => setPackagesLoading(false));

    marketplaceApi.paymentProviders()
      .then((items) => {
        const enabled = (items ?? []).filter((p) => p.code && !isRealPaymentProvider(p));
        setPaymentProviders(enabled.length ? enabled : defaultPaymentProviders);
        setSelectedProviderCode((current) => enabled.some(p => p.code === current) ? current : (enabled[0]?.code ?? 'TEST'));
      })
      .catch(() => setPaymentProviders(defaultPaymentProviders));
  }, []);

  useEffect(() => () => photos.forEach(p => URL.revokeObjectURL(p.url)), [photos]);

  const categoryOptions = useMemo(
    () => categories.length ? categories : [{ id: '', name: 'Loading categories...', slug: '' }],
    [categories]
  );

  const selectedCategory = categories.find(c => c.id === form.categoryId)?.name ?? 'Category';
  const selectedPackage = useMemo(
    () => (packages.length ? packages : defaultPackages).find(p => packageCode(p) === form.packageCode) ?? defaultPackages[0],
    [packages, form.packageCode]
  );
  const selectedPackagePrice = packagePriceNumber(selectedPackage);
  const enabledPaymentProviders = paymentProviders.length ? paymentProviders : defaultPaymentProviders;
  const selectedProvider = enabledPaymentProviders.find((p) => p.code === selectedProviderCode) ?? enabledPaymentProviders[0] ?? defaultPaymentProviders[0];
  const localizedSteps = [
    { id: 1, title: t('detailsStep'), hint: t('detailsHint') },
    { id: 2, title: t('photosStep'), hint: t('photosHint') },
    { id: 3, title: t('packageStep'), hint: t('packageHint') },
    { id: 4, title: t('paymentStep'), hint: t('paymentHint') },
    { id: 5, title: t('reviewStep'), hint: t('reviewHint') },
  ];
  const packageDisplayName = (pkg?: PackageDto | null) => packageLabel(packageCode(pkg));
  const packageDescription = (pkg?: PackageDto | null) => { const c = packageCode(pkg); if (c === 'FREE') return t('freeDesc'); if (c === 'FEATURED') return t('featuredDesc'); if (c === 'URGENT') return t('urgentDesc'); if (c === 'PREMIUM') return t('premiumDesc'); return pkg?.description ?? t('promoteYourListing'); };
  const packageBadgeText = (pkg?: PackageDto | null) => { const c = packageCode(pkg); if (c === 'FEATURED') return t('popular'); if (c === 'URGENT') return t('fastSale'); if (c === 'PREMIUM') return t('bestValue'); return pkg?.badge; };
  const packageFeatureList = (pkg: PackageDto) => { const c = packageCode(pkg); if (c === 'FREE') return [t('standardPlacement'), t('upToPhotos'), t('buyerMessages')]; if (c === 'FEATURED') return [t('featuredBadge'), t('promotedSlider'), t('moreVisibility')]; if (c === 'URGENT') return [t('urgentBadge'), t('priorityVisualStyle'), t('topSearchBoost')]; if (c === 'PREMIUM') return [t('premiumCard'), t('sponsoredRail'), t('topCarousel')]; return packageFeatures(pkg); };
  const latitudeNumber = Number(form.latitude);
  const longitudeNumber = Number(form.longitude);
  const hasCoordinates = Number.isFinite(latitudeNumber) && Number.isFinite(longitudeNumber);
  const publicLocation = buildLocationLabel(form.city, form.state, form.postalCode, form.location);
  const directionUrl = `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(mapsQuery(publicLocation, hasCoordinates ? latitudeNumber : null, hasCoordinates ? longitudeNumber : null))}`;

  function update<K extends keyof PostForm>(key: K, value: PostForm[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function geocodeLocation() {
    const query = [form.addressLine, form.city, form.state, form.postalCode, form.country].filter(Boolean).join(', ') || form.location;
    if (!query.trim()) return;
    setError('');
    try {
      const response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&limit=1&q=${encodeURIComponent(query)}`);
      const results = await response.json();
      const first = Array.isArray(results) ? results[0] : null;
      if (!first?.lat || !first?.lon) { setError('Could not find this location. Please adjust city, ZIP, or pin coordinates.'); return; }
      setForm((current) => ({
        ...current,
        latitude: String(Number(first.lat).toFixed(6)),
        longitude: String(Number(first.lon).toFixed(6)),
        location: buildLocationLabel(current.city, current.state, current.postalCode, current.location),
        locationSource: 'Geocoded',
        locationPrecision: current.hideExactLocation ? 'ApproximateCity' : 'Exact',
      }));
    } catch {
      setError('Location search is unavailable right now. You can still type city/ZIP and save.');
    }
  }

  function useCurrentLocation() {
    if (!navigator.geolocation) { setError('Current location is not available in this browser.'); return; }
    navigator.geolocation.getCurrentPosition(
      (pos) => setForm((current) => ({
        ...current,
        latitude: String(pos.coords.latitude.toFixed(6)),
        longitude: String(pos.coords.longitude.toFixed(6)),
        location: buildLocationLabel(current.city, current.state, current.postalCode, current.location),
        locationSource: 'CurrentLocation',
        locationPrecision: current.hideExactLocation ? 'ApproximateCity' : 'Exact',
      })),
      () => setError('Could not read current location. Please search by address/city.'),
      { enableHighAccuracy: true, timeout: 10000 }
    );
  }

  function nudgePin(latDelta: number, lngDelta: number) {
    const lat = hasCoordinates ? latitudeNumber : 37.3382;
    const lng = hasCoordinates ? longitudeNumber : -121.8863;
    setForm((current) => ({
      ...current,
      latitude: String((lat + latDelta).toFixed(6)),
      longitude: String((lng + lngDelta).toFixed(6)),
      locationSource: 'PinAdjusted',
      locationPrecision: current.hideExactLocation ? 'ApproximateCity' : 'Exact',
    }));
  }

  function handlePhotos(files: FileList | null) {
    if (!files?.length) return;
    setPhotos((current) => {
      current.forEach(p => URL.revokeObjectURL(p.url));
      return Array.from(files).slice(0, 8).map(file => ({ file, url: URL.createObjectURL(file) }));
    });
  }

  function canMoveNext() {
    if (step === 1) return Boolean(form.title.trim() && form.categoryId && form.description.trim());
    if (step === 3) return Boolean(form.price && form.location.trim() && form.packageCode);
    if (step === 4 && selectedPackagePrice > 0) {
      const type = (selectedProvider?.type ?? '').toLowerCase();
      if (type.includes('test') || type.includes('manual') || !type) return Boolean(payment.testStatus);
      return false;
    }
    return true;
  }

  async function submit() {
    setError('');
    setSaved('');
    setSavedId('');

    const user = getSessionUser();
    if (!user?.id) { setError(t('pleaseLoginPost')); return; }
    if (!form.categoryId) { setError(t('chooseCategoryError')); setStep(1); return; }
    if (!form.title.trim() || !form.description.trim()) { setError(t('completeTitleDescription')); setStep(1); return; }
    if (!form.price || Number(form.price) < 0) { setError(t('validPriceError')); setStep(3); return; }

    setBusy(true);
    try {
      const listing = await marketplaceApi.createListing({
        sellerId: user.id,
        categoryId: form.categoryId,
        title: form.title.trim(),
        description: `${form.description.trim()}\n\nCondition: ${form.condition}\nContact preference: ${form.contactPreference}\nPackage: ${selectedPackage?.name ?? form.packageCode}`,
        price: Number(form.price || 0),
        currency: 'USD',
        location: publicLocation || form.location.trim() || 'San Jose, CA',
        packageCode: form.packageCode,
        packageId: selectedPackage?.id,
        addressLine: form.hideExactLocation ? '' : form.addressLine.trim(),
        city: form.city.trim(),
        state: form.state.trim(),
        postalCode: form.postalCode.trim(),
        country: form.country.trim() || 'US',
        latitude: hasCoordinates ? latitudeNumber : null,
        longitude: hasCoordinates ? longitudeNumber : null,
        locationSource: form.locationSource,
        locationPrecision: form.hideExactLocation ? 'ApproximateCity' : form.locationPrecision,
        hideExactLocation: form.hideExactLocation,
      });

      const providerType = (selectedProvider?.type ?? 'Test').toLowerCase();
      const checkout = await marketplaceApi.checkout({
        userId: user.id,
        listingId: listing.id,
        packageId: selectedPackage?.id,
        packageCode: form.packageCode,
        amount: selectedPackagePrice,
        paymentMethod: selectedPackagePrice > 0 ? selectedProvider?.type ?? selectedProviderCode : 'Free',
        providerCode: selectedPackagePrice > 0 ? selectedProviderCode : 'FREE',
        providerStatus: providerType.includes('test') ? payment.testStatus : 'success',
        paymentToken: selectedPackagePrice > 0 ? `${selectedProviderCode.toLowerCase()}-${Date.now()}` : 'free',
        providerPayload: JSON.stringify({
          provider: selectedProviderCode,
          type: selectedProvider?.type,
          testReference: payment.testReference,
          paymentNote: 'Real Stripe/PayPal checkout is not enabled yet.',
        }),
      });

      for (const { file } of photos) {
        const fd = new FormData();
        // Use exact property names expected by ASP.NET form model binding.
        fd.append('File', file);
        fd.append('ListingId', listing.id);
        fd.append('OwnerId', user.id);
        await marketplaceApi.uploadMedia(fd);
      }

      setSavedId(listing.id);
      setSaved(`Payment ${checkout?.payment?.status ?? 'completed'}. Listing submitted. ${photos.length ? `${photos.length} photo(s) uploaded. ` : ''}It is now in My Listings with status ${listing.status ?? 'Pending'}.`);
      setStep(5);
      window.setTimeout(() => router.push('/my-listings'), 900);
    } catch (ex: any) {
      setError(ex?.message ?? t('couldNotSaveListing'));
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="post-v2 shell-wide">
      <section className="post-v2-main">
        <div className="post-hero-v2">
          <span className="search-eyebrow">{t('postListingTitle')}</span>
          <h1>{t('sellLocallyTitle')}</h1>
          <p>{t('sellLocallyText')}</p>
        </div>

        <div className="post-form-v2">
          <div className="post-stepper-v2">
            {localizedSteps.map((s) => (
              <button key={s.id} type="button" onClick={() => setStep(s.id)} className={step === s.id ? 'active' : ''}>
                <b>{s.id}</b><span>{s.title}<small>{s.hint}</small></span>
              </button>
            ))}
          </div>

          <div className="post-card-v2">
            {step === 1 && <div className="post-grid-v2">
              <label className="full">{t('listingTitle')} *<input value={form.title} onChange={(e)=>update('title', e.target.value)} required placeholder={t('listingTitlePlaceholder')} /></label>
              <label>{t('category')} *<select value={form.categoryId} onChange={(e)=>update('categoryId', e.target.value)} required>{categoryOptions.map(c => { const code = c.code ?? c.slug ?? c.name; return <option key={c.id || c.name} value={c.id}>{category(code)}</option>; })}</select></label>
              <label>{t('condition')}<select value={form.condition} onChange={(e)=>update('condition', e.target.value)}><option value="New">{t('new')}</option><option value="Like New">{t('likeNew')}</option><option value="Used">{t('used')}</option><option value="Fair">{t('fair')}</option></select></label>
              <label className="full">{t('description')} *<textarea value={form.description} onChange={(e)=>update('description', e.target.value)} required placeholder={t('descriptionPlaceholder')} /></label>
            </div>}

            {step === 2 && <div className="photo-uploader-v2">
              <label className="dropzone-v2">
                <Icon name="image" size={34} />
                <strong>{t('uploadListingPhotos')}</strong>
                <p>{t('uploadListingPhotosText')}</p>
                <input type="file" accept="image/*" multiple onChange={(e) => handlePhotos(e.currentTarget.files)} />
              </label>
              <div className="photo-grid-v2">
                {photos.length ? photos.map((photo, i) => <div key={photo.url} className="photo-thumb-v2"><img src={photo.url} alt={`Listing photo ${i + 1}`} />{i === 0 && <b>{t('cover')}</b>}</div>) : <><span /><span /><span /><span /></>}
              </div>
            </div>}

            {step === 3 && <div className="post-step-three-v2">
              <div className="post-grid-v2 compact">
                <label>{t('price')} *<input value={form.price} onChange={(e)=>update('price', e.target.value)} type="number" step="0.01" placeholder={t('pricePlaceholder')} required /></label>
                <label>{t('contactPreference')}<select value={form.contactPreference} onChange={(e)=>update('contactPreference', e.target.value)}><option value="Message in app">{t('messageInApp')}</option><option value="Phone">{t('phone')}</option><option value="Email">{t('email')}</option></select></label>
              </div>
              <div className="listing-location-picker-v2">
                <div className="location-picker-head-v2"><div><span>{t('listingLocation')}</span><h3>{t('showBuyersLocation')}</h3></div><button type="button" className="secondary-button" onClick={useCurrentLocation}>{t('useCurrentLocation')}</button></div>
                <div className="post-grid-v2 compact">
                  <label className="full">{t('addressPickupArea')}<input value={form.addressLine} onChange={(e)=>update('addressLine', e.target.value)} placeholder={t('addressPickupPlaceholder')} /></label>
                  <label>{t('city')} *<input value={form.city} onChange={(e)=>{ update('city', e.target.value); update('location', buildLocationLabel(e.target.value, form.state, form.postalCode, form.location)); }} required /></label>
                  <label>{t('state')}<input value={form.state} onChange={(e)=>{ update('state', e.target.value); update('location', buildLocationLabel(form.city, e.target.value, form.postalCode, form.location)); }} /></label>
                  <label>ZIP<input value={form.postalCode} onChange={(e)=>{ update('postalCode', e.target.value); update('location', buildLocationLabel(form.city, form.state, e.target.value, form.location)); }} /></label>
                  <label>{t('country')}<input value={form.country} onChange={(e)=>update('country', e.target.value)} /></label>
                </div>
                <div className="location-tools-v2">
                  <button type="button" className="secondary-button" onClick={geocodeLocation}>{t('findOnMap')}</button>
                  <label className="location-hide-toggle-v2"><input type="checkbox" checked={form.hideExactLocation} onChange={(e)=>setForm(current => ({ ...current, hideExactLocation: e.target.checked, locationPrecision: e.target.checked ? 'ApproximateCity' : 'Exact' }))} /> {t('hideExactAddress')}</label>
                </div>
                <div className="location-map-preview-v2">
                  <div className="location-map-grid-v2" aria-label={t('locationMapPreview')}>
                    <button type="button" className="location-pin-v2" title={t('listingPin')}>📍</button>
                    <div className="location-road-v2 one" /><div className="location-road-v2 two" /><div className="location-road-v2 three" />
                  </div>
                  <div className="location-map-side-v2">
                    <strong>{publicLocation || 'Location not set'}</strong>
                    <span>{hasCoordinates ? `${latitudeNumber.toFixed(5)}, ${longitudeNumber.toFixed(5)}` : 'No coordinates yet'}</span>
                    <small>{t('pinSource')}: {form.locationSource}. {t('publicPrecision')}: {form.hideExactLocation ? t('cityAreaOnly') : t('exactAddress')}.</small>
                    <div className="pin-nudge-v2"><button type="button" onClick={() => nudgePin(0.001, 0)}>↑</button><button type="button" onClick={() => nudgePin(0, -0.001)}>←</button><button type="button" onClick={() => nudgePin(0, 0.001)}>→</button><button type="button" onClick={() => nudgePin(-0.001, 0)}>↓</button></div>
                    <a href={directionUrl} target="_blank" rel="noreferrer">{t('previewDirections')}</a>
                  </div>
                </div>
              </div>
              <div className="package-picker-v2">
                <div className="package-picker-head-v2">
                  <div><span>{t('promotionPackage')}</span><h3>{t('chooseListingAppearance')}</h3></div>
                  {packagesLoading ? <small>{t('loadingPackages')}</small> : <small>{packages.length} {t('options')}</small>}
                </div>
                <div className="package-card-grid-v2">
                  {(packages.length ? packages : defaultPackages).map((pkg) => {
                    const code = packageCode(pkg);
                    const active = form.packageCode === code;
                    return <button key={pkg.id ?? code} type="button" className={`package-card-v2 ${active ? 'active' : ''}`} onClick={() => update('packageCode', code)}>
                      {packageBadgeText(pkg) ? <em>{packageBadgeText(pkg)}</em> : code === 'FREE' ? <em className="soft">{t('starter')}</em> : null}
                      <strong>{packageDisplayName(pkg)}</strong>
                      <div className="package-price-row-v2"><b>{packagePrice(pkg, t('free'))}</b><small>{packageDurationText(pkg)}</small></div>
                      <p>{packageDescription(pkg)}</p>
                      <ul>{packageFeatureList(pkg).slice(0, 4).map((feature) => <li key={feature}><Icon name="check" size={14} />{feature}</li>)}</ul>
                      <span>{active ? t('selected') : t('choosePackage')}</span>
                    </button>;
                  })}
                </div>
              </div>
            </div>}

            {step === 4 && <div className="checkout-step-v2 checkout-provider-layout">
              <div className="checkout-summary-v2">
                <span>{t('checkout')}</span>
                <h3>{packageDisplayName(selectedPackage)} {t('packageLabel')}</h3>
                <p>{packageDescription(selectedPackage)}</p>
                <div className="checkout-package-duration-v2"><Icon name="tag" size={14} /> {packageDurationText(selectedPackage, t)}</div>
                <div className="checkout-total-v2"><small>{t('totalDueToday')}</small><b>{selectedPackage ? packagePrice(selectedPackage, t('free')) : t('free')}</b></div>
                <ul>{packageFeatureList(selectedPackage).slice(0, 4).map(feature => <li key={feature}><Icon name="check" size={14} />{feature}</li>)}</ul>
              </div>

              {selectedPackagePrice > 0 ? <div className="checkout-provider-panel-v2">
                <div className="checkout-card-head-v2"><Icon name="shield" size={20} /><div><strong>{t('choosePaymentMethod')}</strong><small>{t('providersLoaded')}</small></div></div>
                <div className="provider-card-list-v2">
                  {enabledPaymentProviders.map((provider) => {
                    const active = provider.code === selectedProviderCode;
                    return <button key={provider.code} type="button" className={`provider-option-v2 ${active ? 'active' : ''}`} onClick={() => setSelectedProviderCode(provider.code)}>
                      <span>{providerIcon(provider.type)}</span>
                      <div><strong>{paymentProviderLabel(provider.displayName ?? provider.code ?? provider.name)}</strong><small>{t('paymentNotActivated')}</small></div>
                      <b>{active ? t('selected') : t('select')}</b>
                    </button>;
                  })}
                </div>

                <div className="provider-form-v2 test-provider-form-v2">
                  <div className="provider-form-title"><strong>{t('manualTestPayment')}</strong><small>{t('manualPaymentHelp')}</small></div>
                  <label>{t('status')}<select value={payment.testStatus} onChange={(e)=>setPayment({...payment, testStatus:e.target.value})}><option value="success">{t('success')}</option><option value="pending">{t('pending')}</option><option value="failed">{t('failed')}</option></select></label>
                  <label>{t('reference')}<input value={payment.testReference} onChange={(e)=>setPayment({...payment, testReference:e.target.value})} placeholder={t('manualOrderPlaceholder')} /></label>
                  <div className="provider-coming-soon-v2"><Icon name="shield" size={16} /> {t('realCheckoutComingSoon')}</div>
                </div>
              </div> : <div className="checkout-free-v2"><Icon name="check" size={26} /><strong>{t('noPaymentRequired')}</strong><p>{t('freePublishText')}</p></div>}
            </div>}

            {step === 5 && <div className="review-box-v2">
              <Icon name="shield" size={32} />
              <strong>{saved ? t('listingSubmitted') : t('readyForReview')}</strong>
              <p>{saved || t('readyForReviewText')}</p>
              {savedId ? <div className="post-success-actions"><a className="primary-button" href="/my-listings">{t('goToMyListings')}</a><a className="secondary-button" href={`/listings/${savedId}`}>{t('viewListing')}</a></div> : null}
            </div>}

            {error && <p className="form-error">{error}</p>}
            {saved && <p className="success-message-v2">{saved}</p>}
            <div className="post-actions-v2">
              <button type="button" className="secondary-button" disabled={step <= 1 || busy} onClick={() => setStep(step - 1)}>{t('back')}</button>
              {step < 4 ? <button type="button" className="primary-button" disabled={busy || !canMoveNext()} onClick={() => setStep(step + 1)}>{t('nextStep')}</button> : step === 4 ? <button type="button" className="primary-button" disabled={busy || !canMoveNext() || Boolean(saved)} onClick={submit}>{busy ? t('processing') : selectedPackagePrice > 0 ? t('payPublish') : t('publishFreeListing')}</button> : <button type="button" className="primary-button" onClick={() => router.push('/my-listings')}>{t('goToMyListings')}</button>}
            </div>
          </div>
        </div>
      </section>

      <aside className="post-v2-side">
        <div className="post-preview-card-v2">
          <span className="badge badge-featured">{t('preview')}</span>
          <div className="preview-image-v2">{photos[0] ? <img src={photos[0].url} alt={t('coverPreview')} /> : <Icon name="image" size={36} />}</div>
          <div className="preview-content-v2"><h3>{form.title || t('yourListingTitle')}</h3><strong>{Number(form.price || 0) ? `$${Number(form.price).toLocaleString()}` : t('price')}</strong><p><Icon name="pin" size={13} /> {publicLocation}</p><small>{selectedCategory} · {form.packageCode} · {packageDurationText(selectedPackage, t)}</small></div>
        </div>
        <div className="post-tips-v2">
          <h3>{t('tipsForBetterListing')}</h3>
          <p><Icon name="check" size={16} /> {t('tipPhotos')}</p>
          <p><Icon name="check" size={16} /> {t('tipPrice')}</p>
          <p><Icon name="check" size={16} /> {t('tipSafePay')}</p>
        </div>
      </aside>
    </main>
  );
}
