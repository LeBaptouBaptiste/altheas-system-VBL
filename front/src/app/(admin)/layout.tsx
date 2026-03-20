'use client';

import { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import Link from 'next/link';
import {
  LayoutDashboard, Package, FolderOpen, ShoppingCart, FileText,
  Users, MessageSquare, Image, FileEdit, LogOut, ChevronLeft, Menu, Globe, Store,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';
import { Separator } from '@/components/ui/separator';
import { useI18n } from '@/context/i18n-context';
import { useAuth } from '@/context/auth-context';
import type { Locale } from '@/lib/i18n';

const navItems = [
  { href: '/admin', icon: LayoutDashboard, labelKey: 'admin.dashboard' },
  { href: '/admin/products', icon: Package, labelKey: 'admin.products' },
  { href: '/admin/categories', icon: FolderOpen, labelKey: 'admin.categories' },
  { href: '/admin/orders', icon: ShoppingCart, labelKey: 'admin.orders' },
  { href: '/admin/invoices', icon: FileText, labelKey: 'admin.invoices' },
  { href: '/admin/users', icon: Users, labelKey: 'admin.users' },
  { href: '/admin/messages', icon: MessageSquare, labelKey: 'admin.messages' },
  { href: '/admin/carousel', icon: Image, labelKey: 'admin.carousel' },
  { href: '/admin/pages', icon: FileEdit, labelKey: 'admin.static_pages' },
];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const { t, locale, setLocale } = useI18n();
  const { isAdmin, isAuthenticated, logout } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [twoFAVerified, setTwoFAVerified] = useState(false);
  const [twoFACode, setTwoFACode] = useState('');
  const [twoFAError, setTwoFAError] = useState(false);

  useEffect(() => {
    const verified = sessionStorage.getItem('admin_2fa_verified');
    if (verified === 'true') setTwoFAVerified(true);
  }, []);

  // If not admin, redirect
  useEffect(() => {
    if (!isAuthenticated || !isAdmin) {
      router.push('/login');
    }
  }, [isAuthenticated, isAdmin, router]);

  if (!isAuthenticated || !isAdmin) return null;

  // 2FA gate
  if (!twoFAVerified) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 p-4">
        <div className="w-full max-w-sm bg-white rounded-lg shadow-lg p-8">
          <div className="text-center mb-6">
            <div className="w-12 h-12 rounded-full bg-brand-primary mx-auto flex items-center justify-center mb-3">
              <LayoutDashboard className="w-6 h-6 text-white" />
            </div>
            <h1 className="text-xl font-semibold text-brand-dark">{t('admin.2fa_title')}</h1>
            <p className="text-sm text-muted-foreground mt-1">{t('admin.2fa_hint')}</p>
          </div>
          <form onSubmit={(e) => {
            e.preventDefault();
            if (twoFACode === '123456') {
              sessionStorage.setItem('admin_2fa_verified', 'true');
              setTwoFAVerified(true);
            } else {
              setTwoFAError(true);
            }
          }}>
            <input
              type="text"
              inputMode="numeric"
              maxLength={6}
              value={twoFACode}
              onChange={(e) => { setTwoFACode(e.target.value.replace(/\D/g, '')); setTwoFAError(false); }}
              className={`w-full text-center text-2xl tracking-[0.5em] py-3 border rounded-lg mb-4 outline-none focus:border-brand-primary ${twoFAError ? 'border-error' : 'border-gray-200'}`}
              placeholder="••••••"
              autoFocus
            />
            {twoFAError && <p className="text-error text-sm text-center mb-3">Code incorrect</p>}
            <Button type="submit" className="w-full bg-brand-primary hover:bg-brand-hover text-white" disabled={twoFACode.length !== 6}>
              {locale === 'fr' ? 'Vérifier' : 'Verify'}
            </Button>
          </form>
        </div>
      </div>
    );
  }

  const handleLogout = () => {
    sessionStorage.removeItem('admin_2fa_verified');
    logout();
    router.push('/login');
  };

  const SidebarContent = ({ onNavigate }: { onNavigate?: () => void }) => (
    <div className="flex flex-col h-full">
      {/* Logo */}
      <div className="p-4 flex items-center gap-3">
        <div className="w-9 h-9 rounded-lg bg-brand-primary flex items-center justify-center shrink-0">
          <span className="text-white font-bold text-lg">A</span>
        </div>
        {!collapsed && <span className="text-brand-dark font-semibold text-sm">Althea Admin</span>}
      </div>
      <Separator />

      {/* Nav */}
      <nav className="flex-1 py-2 overflow-y-auto">
        {navItems.map(item => {
          const Icon = item.icon;
          const isActive = pathname === item.href || (item.href !== '/admin' && pathname.startsWith(item.href));
          return (
            <Link
              key={item.href}
              href={item.href}
              onClick={onNavigate}
              className={`flex items-center gap-3 mx-2 px-3 py-2.5 rounded-md text-sm transition-colors ${
                isActive
                  ? 'bg-brand-primary/10 text-brand-primary font-medium'
                  : 'text-gray-600 hover:bg-gray-100'
              }`}
              title={collapsed ? t(item.labelKey) : undefined}
            >
              <Icon className="w-4 h-4 shrink-0" />
              {!collapsed && <span>{t(item.labelKey)}</span>}
            </Link>
          );
        })}
      </nav>

      {/* Bottom actions */}
      <div className="p-3 border-t space-y-2">
        <Link
          href="/"
          onClick={onNavigate}
          className="flex items-center gap-3 px-3 py-2 rounded-md text-sm text-gray-600 hover:bg-gray-100 transition-colors"
        >
          <Store className="w-4 h-4 shrink-0" />
          {!collapsed && <span>{locale === 'fr' ? 'Voir le site' : 'View store'}</span>}
        </Link>
        <button
          onClick={handleLogout}
          className="flex items-center gap-3 px-3 py-2 rounded-md text-sm text-destructive hover:bg-destructive/10 transition-colors w-full"
        >
          <LogOut className="w-4 h-4 shrink-0" />
          {!collapsed && <span>{t('nav.logout')}</span>}
        </button>
      </div>
    </div>
  );

  return (
    <div className="min-h-screen bg-gray-50 flex">
      {/* Desktop Sidebar */}
      <aside className={`hidden md:flex flex-col border-r bg-white shrink-0 transition-all duration-200 ${collapsed ? 'w-16' : 'w-56'}`}>
        <SidebarContent />
        <button
          onClick={() => setCollapsed(!collapsed)}
          className="p-2 border-t flex items-center justify-center hover:bg-gray-100"
          aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        >
          <ChevronLeft className={`w-4 h-4 text-gray-400 transition-transform ${collapsed ? 'rotate-180' : ''}`} />
        </button>
      </aside>

      {/* Main area */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Top bar */}
        <header className="h-14 border-b bg-white flex items-center justify-between px-4 shrink-0">
          <div className="flex items-center gap-2">
            <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
              <SheetTrigger asChild>
                <Button variant="ghost" size="icon" className="md:hidden">
                  <Menu className="w-5 h-5" />
                </Button>
              </SheetTrigger>
              <SheetContent side="left" className="w-[250px] p-0">
                <SidebarContent onNavigate={() => setMobileOpen(false)} />
              </SheetContent>
            </Sheet>
            <h2 className="text-sm font-medium text-brand-dark truncate">
              {navItems.find(n => pathname === n.href || (n.href !== '/admin' && pathname.startsWith(n.href)))
                ? t(navItems.find(n => pathname === n.href || (n.href !== '/admin' && pathname.startsWith(n.href)))!.labelKey)
                : t('admin.dashboard')}
            </h2>
          </div>
          <div className="flex items-center gap-2">
            <Select value={locale} onValueChange={(v) => setLocale(v as Locale)}>
              <SelectTrigger className="h-8 w-[90px] text-xs border-gray-200" aria-label="Language">
                <Globe className="w-3 h-3 mr-1" />
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="fr">FR</SelectItem>
                <SelectItem value="en">EN</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </header>

        {/* Content */}
        <main className="flex-1 overflow-y-auto p-4 md:p-6">
          {children}
        </main>
      </div>
    </div>
  );
}
