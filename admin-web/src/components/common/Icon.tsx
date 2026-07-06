import type React from 'react';
import type { LucideIcon } from 'lucide-react';
import {
  BadgeDollarSign,
  Bell,
  Check,
  ChevronLeft,
  ChevronRight,
  ClipboardCheck,
  Copy,
  CreditCard,
  Download,
  Ellipsis,
  Eye,
  FileText,
  Filter,
  Flag,
  FolderTree,
  Heart,
  History,
  LayoutDashboard,
  List,
  LogOut,
  Menu,
  Megaphone,
  MessageSquare,
  Moon,
  Package,
  PanelTop,
  Pause,
  Pencil,
  Play,
  Plus,
  RefreshCw,
  Search,
  Settings,
  ShieldCheck,
  Sun,
  ToggleLeft,
  ToggleRight,
  Trash2,
  Upload,
  User,
  Users,
  X,
} from 'lucide-react';

type IconProps = {
  name: string;
  size?: number;
  strokeWidth?: number;
  className?: string;
};

const icons: Record<string, LucideIcon> = {
  dashboard: LayoutDashboard,
  review: ClipboardCheck,
  check: Check,
  reject: X,
  x: X,
  close: X,
  list: Package,
  listings: Package,
  grid: FolderTree,
  categories: FolderTree,
  users: Users,
  user: User,
  message: MessageSquare,
  messages: MessageSquare,
  bell: Bell,
  notifications: Bell,
  ad: Megaphone,
  ads: Megaphone,
  banner: PanelTop,
  placement: PanelTop,
  placements: PanelTop,
  payment: CreditCard,
  payments: CreditCard,
  chart: FileText,
  reports: Flag,
  flag: Flag,
  settings: Settings,
  system: RefreshCw,
  health: ShieldCheck,
  audit: History,
  history: History,
  search: Search,
  filter: Filter,
  moon: Moon,
  sun: Sun,
  logout: LogOut,
  menu: Menu,
  dots: Ellipsis,
  more: Ellipsis,
  'chevron-left': ChevronLeft,
  'chevron-right': ChevronRight,
  view: Eye,
  eye: Eye,
  edit: Pencil,
  pencil: Pencil,
  delete: Trash2,
  trash: Trash2,
  toggleOn: ToggleRight,
  toggleOff: ToggleLeft,
  toggle: ToggleRight,
  plus: Plus,
  add: Plus,
  copy: Copy,
  refresh: RefreshCw,
  upload: Upload,
  download: Download,
  pause: Pause,
  play: Play,
  heart: Heart,
  revenue: BadgeDollarSign,
};

export function Icon({ name, size = 18, strokeWidth = 2, className = '' }: IconProps) {
  const LucideIcon = icons[name] ?? CircleFallback;
  return (
    <LucideIcon
      className={`admin-icon lucide-admin-icon ${className}`.trim()}
      size={size}
      strokeWidth={strokeWidth}
      aria-hidden="true"
    />
  );
}

function CircleFallback(props: React.ComponentProps<'svg'>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" {...props}>
      <circle cx="12" cy="12" r="8" />
    </svg>
  );
}
