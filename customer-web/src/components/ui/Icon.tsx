import {
  ArrowRight,
  Bell,
  Bookmark,
  Briefcase,
  Car,
  Check,
  CreditCard,
  Eye,
  Filter,
  Grid3X3,
  Heart,
  Home,
  House,
  Image,
  List,
  Mail,
  Map,
  MapPin,
  Menu,
  MessageCircle,
  MessageSquare,
  Phone,
  Plus,
  Rocket,
  Search,
  Settings,
  Shield,
  ShoppingBag,
  Smartphone,
  Sparkles,
  Sprout,
  Star,
  Tag,
  User,
  Users,
  Wrench,
  type LucideIcon,
} from 'lucide-react';

export type IconName =
  | 'logo' | 'search' | 'pin' | 'plus' | 'heart' | 'message' | 'bell' | 'user'
  | 'home' | 'tag' | 'list' | 'saved' | 'category' | 'vehicle' | 'rental' | 'sale' | 'jobs'
  | 'services' | 'electronics' | 'garden' | 'community' | 'shield' | 'eye'
  | 'comment' | 'bookmark' | 'filter' | 'map' | 'star' | 'menu' | 'settings' | 'card' | 'grid' | 'image' | 'check' | 'rocket' | 'mail' | 'phone' | 'arrowRight' | 'sparkle';

const iconMap: Record<IconName, LucideIcon> = {
  logo: MapPin,
  search: Search,
  pin: MapPin,
  plus: Plus,
  heart: Heart,
  message: MessageSquare,
  bell: Bell,
  user: User,
  home: Home,
  tag: Tag,
  list: List,
  saved: Bookmark,
  category: Grid3X3,
  vehicle: Car,
  rental: House,
  sale: ShoppingBag,
  jobs: Briefcase,
  services: Wrench,
  electronics: Smartphone,
  garden: Sprout,
  community: Users,
  shield: Shield,
  eye: Eye,
  comment: MessageCircle,
  bookmark: Bookmark,
  filter: Filter,
  map: Map,
  star: Star,
  menu: Menu,
  settings: Settings,
  card: CreditCard,
  grid: Grid3X3,
  image: Image,
  check: Check,
  rocket: Rocket,
  mail: Mail,
  phone: Phone,
  arrowRight: ArrowRight,
  sparkle: Sparkles,
};

export function Icon({ name, size = 20, className = '' }: { name: IconName; size?: number; className?: string }) {
  const LucideIconComponent = iconMap[name] ?? iconMap.category;
  return (
    <LucideIconComponent
      className={`icon ${className}`.trim()}
      size={size}
      strokeWidth={2}
      aria-hidden="true"
    />
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
