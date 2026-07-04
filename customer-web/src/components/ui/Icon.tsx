export type IconName =
  | 'logo' | 'search' | 'pin' | 'plus' | 'heart' | 'message' | 'bell' | 'user'
  | 'home' | 'tag' | 'list' | 'saved' | 'category' | 'vehicle' | 'rental' | 'sale' | 'jobs'
  | 'services' | 'electronics' | 'garden' | 'community' | 'shield' | 'eye'
  | 'comment' | 'bookmark' | 'filter' | 'map' | 'star' | 'menu' | 'settings' | 'card' | 'grid' | 'image' | 'check' | 'rocket' | 'mail' | 'phone' | 'arrowRight' | 'sparkle';

const paths: Record<IconName, string[]> = {
  logo: ['M12 22s7-5.6 7-12A7 7 0 1 0 5 10c0 6.4 7 12 7 12Z', 'M12 13.2a3.2 3.2 0 1 0 0-6.4 3.2 3.2 0 0 0 0 6.4Z'],
  search: ['M11 19a8 8 0 1 0 0-16 8 8 0 0 0 0 16Z', 'm21 21-4.35-4.35'],
  pin: ['M12 22s7-5.6 7-12A7 7 0 1 0 5 10c0 6.4 7 12 7 12Z', 'M12 13a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z'],
  plus: ['M12 5v14', 'M5 12h14'],
  heart: ['M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.6l-1-1a5.5 5.5 0 1 0-7.8 7.8l1 1L12 21l7.8-7.6 1-1a5.5 5.5 0 0 0 0-7.8Z'],
  message: ['M21 15a4 4 0 0 1-4 4H8l-5 3V7a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4v8Z'],
  bell: ['M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9Z', 'M13.7 21a2 2 0 0 1-3.4 0'],
  user: ['M20 21a8 8 0 0 0-16 0', 'M12 12a5 5 0 1 0 0-10 5 5 0 0 0 0 10Z'],
  home: ['M3 11 12 3l9 8', 'M5 10v10h14V10', 'M9 20v-6h6v6'],
  tag: ['M20.6 13.4 13.4 20.6a2 2 0 0 1-2.8 0L3 13V3h10l7.6 7.6a2 2 0 0 1 0 2.8Z', 'M7.5 7.5h.01'],
  list: ['M8 6h13', 'M8 12h13', 'M8 18h13', 'M3 6h.01', 'M3 12h.01', 'M3 18h.01'],
  saved: ['M6 3h12a1 1 0 0 1 1 1v17l-7-4-7 4V4a1 1 0 0 1 1-1Z'],
  category: ['M4 4h7v7H4z', 'M13 4h7v7h-7z', 'M4 13h7v7H4z', 'M13 13h7v7h-7z'],
  vehicle: ['M5 17h14l-1.4-6.2A3 3 0 0 0 14.7 8H9.3a3 3 0 0 0-2.9 2.8L5 17Z', 'M7 17v2', 'M17 17v2', 'M7.5 13h9'],
  rental: ['M3 11 12 3l9 8', 'M5 10v10h14V10', 'M9 20v-6h6v6'],
  sale: ['M20 12v8H4v-8', 'M2 7h20l-2 5H4L2 7Z', 'M6 7a6 6 0 0 1 12 0'],
  jobs: ['M10 6V5a2 2 0 0 1 2-2h0a2 2 0 0 1 2 2v1', 'M3 7h18v12H3z', 'M3 12h18'],
  services: ['M14.7 6.3a4 4 0 0 0-5 5L3 18l3 3 6.7-6.7a4 4 0 0 0 5-5l-3 3-3-3 3-3Z'],
  electronics: ['M7 2h10a2 2 0 0 1 2 2v16a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2Z', 'M11 18h2'],
  garden: ['M12 22V12', 'M12 12c-5 0-7-4-7-8 5 0 7 4 7 8Z', 'M12 12c5 0 7-4 7-8-5 0-7 4-7 8Z'],
  community: ['M16 21v-2a4 4 0 0 0-8 0v2', 'M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8Z', 'M22 21v-2a4 4 0 0 0-3-3.87', 'M2 21v-2a4 4 0 0 1 3-3.87'],
  shield: ['M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z'],
  eye: ['M2 12s3.5-7 10-7 10 7-3.5 7-10 7Z', 'M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z'],
  comment: ['M21 15a4 4 0 0 1-4 4H8l-5 3V7a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4v8Z'],
  bookmark: ['M6 3h12a1 1 0 0 1 1 1v17l-7-4-7 4V4a1 1 0 0 1 1-1Z'],
  filter: ['M3 6h18', 'M7 12h10', 'M10 18h4'],
  map: ['M9 18l-6 3V6l6-3 6 3 6-3v15l-6 3-6-3Z', 'M9 3v15', 'M15 6v15'],
  star: ['M12 2l3 6 6 .9-4.4 4.3 1.1 6.1L12 16.8l-5.5 3.4 1.1-6.1L3.2 8.9 9 8l3-6Z'],
  menu: ['M4 6h16', 'M4 12h16', 'M4 18h16'],
  settings: ['M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6Z', 'M19 12h3', 'M2 12h3', 'M12 2v3', 'M12 19v3'],
  card: ['M3 6h18v12H3z', 'M3 10h18', 'M7 15h3'],
  grid: ['M4 4h6v6H4z', 'M14 4h6v6h-6z', 'M4 14h6v6H4z', 'M14 14h6v6h-6z'],
  image: ['M4 5h16a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2Z', 'M8 11a2 2 0 1 0 0-4 2 2 0 0 0 0 4Z', 'm22 15-5-5L5 22'],
  check: ['M20 6 9 17l-5-5'],
  rocket: ['M4.5 16.5c-1.5 1.3-2 3.3-2 5 1.7 0 3.7-.5 5-2', 'M9 15 4 20', 'M15 9l5-5', 'M14 4h6v6', 'M9 15l-1-4 8-8 4 4-8 8-3-1Z'],
  mail: ['M4 4h16v16H4z', 'm22 6-10 7L2 6'],
  phone: ['M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.4 19.4 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1.9.3 1.8.6 2.6a2 2 0 0 1-.5 2.1L8.9 9.7a16 16 0 0 0 5.4 5.4l1.3-1.3a2 2 0 0 1 2.1-.5c.8.3 1.7.5 2.6.6a2 2 0 0 1 1.7 2Z'],
  arrowRight: ['M5 12h14', 'm12 5 7 7-7 7'],
  sparkle: ['M12 3l1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8L12 3Z', 'M19 15l.8 2.2L22 18l-2.2.8L19 21l-.8-2.2L16 18l2.2-.8L19 15Z'],
};

export function Icon({ name, size = 20, className = '' }: { name: IconName; size?: number; className?: string }) {
  return (
    <svg className={`icon ${className}`} width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      {paths[name].map((d, i) => <path key={i} d={d} />)}
    </svg>
  );
}

export function categoryIcon(name?: string): IconName {
  const n = (name ?? '').toLowerCase();
  if (n.includes('vehicle') || n.includes('car')) return 'vehicle';
  if (n.includes('property') || n.includes('rent')) return 'rental';
  if (n.includes('sale')) return 'sale';
  if (n.includes('job')) return 'jobs';
  if (n.includes('service')) return 'services';
  if (n.includes('electronic') || n.includes('phone')) return 'electronics';
  if (n.includes('home') || n.includes('garden')) return 'garden';
  if (n.includes('community')) return 'community';
  return 'category';
}
