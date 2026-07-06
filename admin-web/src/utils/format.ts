export function formatPrice(currency: unknown, price: unknown) {
  if (price === undefined || price === null || price === '') return '-';
  return `${currency || '$'}${Number(price).toLocaleString()}`;
}

export function getInitials(text: string) {
  return text
    .split(/[\s@._-]+/)
    .filter(Boolean)
    .map((x) => x[0])
    .join('')
    .slice(0, 2)
    .toUpperCase() || 'AD';
}

export function categoryInitial(text: unknown) {
  return String(text || 'Listing')
    .split(/\s+/)
    .map((x) => x[0])
    .join('')
    .slice(0, 2)
    .toUpperCase();
}

export function labelize(key: string) {
  return key.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/_/g, ' ');
}

export function normalizeListings(rows: any[]) {
  return rows.map((x) => ({
    title: x.title,
    price: formatPrice(x.currency, x.price),
    status: x.moderationStatus || x.status,
    city: x.city,
    category: x.category?.name || x.categoryName || x.category,
    created: x.createdAt ? new Date(x.createdAt).toLocaleDateString() : '',
  }));
}
