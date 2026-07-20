import { useEffect, useState } from 'react';
import { Icon } from '../common/Icon';
import { AdminButton, AdminIconButton, AdminTextBox } from '../common/AdminControls';
import { navItems } from './navItems';
import { apiClient, type AdminSessionUser } from '../../lib/api/apiClient';
import { DashboardPage } from '../../pages/DashboardPage';
import { ListingReviewPage } from '../../pages/ListingReviewPage';
import { AllListingsPage } from '../../pages/AllListingsPage';
import { CategoriesPage } from '../../pages/CategoriesPage';
import { UsersPage } from '../../pages/UsersPage';
import { MessagesPage } from '../../pages/MessagesPage';
import { NotificationsPage } from '../../pages/NotificationsPage';
import { AdsPage } from '../../pages/AdsPage';
import { PaymentsPage } from '../../pages/PaymentsPage';
import { ReportsPage } from '../../pages/ReportsPage';
import { SettingsPage } from '../../pages/SettingsPage';
import { SiteSettingsPage } from '../../pages/SiteSettingsPage';
import { ExternalProvidersPage } from '../../pages/ExternalProvidersPage';
import { LocalitiesPage } from '../../pages/LocalitiesPage';
import { BlockedWordsPage } from '../../pages/BlockedWordsPage';
import { HealthPage } from '../../pages/HealthPage';
import { getTabFromPath, getTabPath, navigateTo } from '../../routes';
import type { AdminTab, Theme } from '../../types';
import { getInitials } from '../../utils/format';

const navGroups: Array<{ title: string; ids: AdminTab[] }> = [
  { title: 'Overview', ids: ['dashboard'] },
  { title: 'Marketplace', ids: ['pending', 'listings', 'categories', 'banners'] },
  { title: 'Users & Comms', ids: ['users', 'messages', 'notifications'] },
  { title: 'Operations', ids: ['payments', 'reports', 'settings', 'siteSettings', 'externalProviders', 'localities', 'blockedWords', 'health'] },
];

export function AdminShell({ user, theme, setTheme, onLogout }: { user: AdminSessionUser; theme: Theme; setTheme: (t: Theme) => void; onLogout: () => void }) {
  const [tab, setTab] = useState<AdminTab>(() => getTabFromPath(window.location.pathname));
  const [collapsed, setCollapsed] = useState(false);
  const current = navItems.find((t) => t.id === tab) ?? navItems[0];

  useEffect(() => {
    const syncRoute = () => setTab(getTabFromPath(window.location.pathname));
    syncRoute();
    window.addEventListener('popstate', syncRoute);
    return () => window.removeEventListener('popstate', syncRoute);
  }, []);

  function openPage(nextTab: AdminTab) {
    setTab(nextTab);
    navigateTo(getTabPath(nextTab));
  }
  const initials = getInitials(user.name || user.email || 'AD');

  function logout() {
    apiClient.logout();
    onLogout();
  }

  return (
    <div className={`admin-app ${collapsed ? 'sidebar-collapsed' : ''}`}>
      <aside className="admin-sidebar">
        <div className="brand">
          <span>
            <Icon name="dashboard" />
          </span>
          <div>
            <b>OpenMarketplace</b>
            <small>ADMIN SYSTEM</small>
          </div>
        </div>
        <nav aria-label="Admin navigation">
          {navGroups.map((group) => (
            <div className="nav-group" key={group.title}>
              <p>{group.title}</p>
              {group.ids.map((id) => {
                const item = navItems.find((n) => n.id === id);
                if (!item) return null;
                return (
                  <a
                    key={item.id}
                    href={item.path}
                    className={tab === item.id ? 'active' : ''}
                    onClick={(event) => {
                      event.preventDefault();
                      openPage(item.id);
                    }}
                    title={item.label}
                  >
                    {item.icon}
                    <span>{item.label}</span>
                  </a>
                );
              })}
            </div>
          ))}
        </nav>
        <div className="admin-user">
          <div className="avatar">{initials}</div>
          <div>
            <strong>{user.name || 'Admin'}</strong>
            <small>{user.role || 'Admin'}</small>
          </div>
          <AdminIconButton className="inverse" icon="logout" label="Logout" onClick={logout} />
        </div>
      </aside>
      <main className="admin-main">
        <header className="topbar">
          <AdminIconButton className="menu-toggle" icon="menu" label="Toggle sidebar" onClick={() => setCollapsed((v) => !v)} />
          <div className="admin-profile-chip">
            <div className="avatar small">{initials}</div>
            <div>
              <b>{user.name || 'System Administrator'}</b>
              <span>{user.role || 'Admin'}</span>
            </div>
          </div>
          <label className="global-search">
            <Icon name="search" />
            <AdminTextBox placeholder="Search listings, users, messages, ads..." />
          </label>
          <div className="top-actions">
            <ThemeToggle theme={theme} setTheme={setTheme} />
            <AdminIconButton icon="bell" label="Notifications" />
          </div>
        </header>
        <div className="admin-page">
          <PageRenderer tab={tab} title={current.label} description={current.description} />
        </div>
      </main>
    </div>
  );
}

function PageRenderer({ tab }: { tab: AdminTab; title: string; description: string }) {
  if (tab === 'dashboard') return <DashboardPage />;
  if (tab === 'pending') return <ListingReviewPage />;
  if (tab === 'listings') return <AllListingsPage />;
  if (tab === 'categories') return <CategoriesPage />;
  if (tab === 'users') return <UsersPage />;
  if (tab === 'messages') return <MessagesPage />;
  if (tab === 'notifications') return <NotificationsPage />;
  if (tab === 'banners') return <AdsPage />;
  if (tab === 'payments') return <PaymentsPage />;
  if (tab === 'reports') return <ReportsPage />;
  if (tab === 'settings') return <SettingsPage />;
  if (tab === 'siteSettings') return <SiteSettingsPage />;
  if (tab === 'externalProviders') return <ExternalProvidersPage />;
  if (tab === 'localities') return <LocalitiesPage />;
  if (tab === 'blockedWords') return <BlockedWordsPage />;
  return <HealthPage />;
}

function ThemeToggle({ theme, setTheme }: { theme: Theme; setTheme: (t: Theme) => void }) {
  return (
    <AdminButton className="theme-toggle" onClick={() => setTheme(theme === 'light' ? 'dark' : 'light')} title="Toggle theme">
      <Icon name={theme === 'light' ? 'moon' : 'sun'} />
      <span>{theme === 'light' ? 'Dark' : 'Light'}</span>
    </AdminButton>
  );
}
