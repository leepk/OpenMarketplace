import type React from 'react';

export type AdminTab =
  | 'dashboard'
  | 'pending'
  | 'listings'
  | 'categories'
  | 'users'
  | 'messages'
  | 'notifications'
  | 'banners'
  | 'payments'
  | 'reports'
  | 'settings'
  | 'health';

export type Theme = 'light' | 'dark';

export type NavItem = {
  id: AdminTab;
  label: string;
  icon: React.ReactNode;
  description: string;
  path?: string;
};
