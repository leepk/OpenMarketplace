import { Icon } from '../common/Icon';
import type { NavItem } from '../../types';
import { adminRoutes } from '../../routes';

export const navItems: NavItem[] = [
  { id: 'dashboard', path: adminRoutes.dashboard, label: 'Dashboard', icon: <Icon name="dashboard" />, description: 'Overview, metrics and recent marketplace activity.' },
  { id: 'pending', path: adminRoutes.pending, label: 'Listing Review', icon: <Icon name="review" />, description: 'Approve or reject listings waiting for moderation.' },
  { id: 'listings', path: adminRoutes.listings, label: 'All Listings', icon: <Icon name="list" />, description: 'Search and manage every listing in the marketplace.' },
  { id: 'categories', path: adminRoutes.categories, label: 'Categories', icon: <Icon name="grid" />, description: 'Manage categories and listing groups.' },
  { id: 'users', path: adminRoutes.users, label: 'Users', icon: <Icon name="users" />, description: 'Manage user roles, source and status.' },
  { id: 'messages', path: adminRoutes.messages, label: 'User Messages', icon: <Icon name="message" />, description: 'Review conversations and moderate unsafe messages.' },
  { id: 'notifications', path: adminRoutes.notifications, label: 'Send Notifications', icon: <Icon name="bell" />, description: 'Send direct or broadcast notifications.' },
  { id: 'banners', path: adminRoutes.banners, label: 'Ads & Banners', icon: <Icon name="ad" />, description: 'Review ads, placements, clicks and impressions.' },
  { id: 'payments', path: adminRoutes.payments, label: 'Payments', icon: <Icon name="payment" />, description: 'Review transactions and package purchases.' },
  { id: 'reports', path: adminRoutes.reports, label: 'Reports & Analytics', icon: <Icon name="chart" />, description: 'Marketplace trends, revenue and moderation stats.' },
  { id: 'settings', path: adminRoutes.settings, label: 'Package Manage', icon: <Icon name="settings" />, description: 'Manage listing packages and pricing.' },
  { id: 'siteSettings', path: adminRoutes.siteSettings, label: 'Site Settings', icon: <Icon name="site" />, description: 'Manage customer website logo, colors, contact links and SEO.' },
  { id: 'health', path: adminRoutes.health, label: 'System', icon: <Icon name="system" />, description: 'API health and diagnostic information.' },
];
