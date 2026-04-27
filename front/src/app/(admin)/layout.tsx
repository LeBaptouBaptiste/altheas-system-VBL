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
import { InputOTP, InputOTPGroup, InputOTPSlot } from '@/components/ui/input-otp';
import { Input } from '@/components/ui/input';
import { useI18n } from '@/context/i18n-context';
import { useAuth } from '@/context/auth-context';
import { setAmbientStepUpToken } from '@/lib/api';
import { authService } from '@/lib/api-services';
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
  const { isAdmin, isAuthenticated, loading: authLoading, logout } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  // Admin step-up token (reusable, ~30 min). Lives in component state ONLY:
  // never persisted, so leaving the admin area / closing the tab requires
  // a re-prompt. Cleared on unmount via the cleanup effect below.
  const [adminStepUpToken, setAdminStepUpToken] = useState<string | null>(null);
  const [stepUpCode, setStepUpCode] = useState('');
  const [useRecovery, setUseRecovery] = useState(false);
  const [recoveryCode, setRecoveryCode] = useState('');
  const [stepUpError, setStepUpError] = useState('');
  const [stepUpSubmitting, setStepUpSubmitting] = useState(false);

  // Sync the ambient token to api.ts so every call from admin pages includes
  // X-Step-Up-Token automatically. We do the actual write SYNCHRONOUSLY at
  // the call sites (submitStepUp / handleLogout) so that the children's
  // first render with adminStepUpToken set already sees the ambient. This
  // useEffect only handles the unmount case (leaving the admin area), to
  // make sure no stale token leaks into a non-admin context.
  useEffect(() => {
    return () => { setAmbientStepUpToken(null); };
  }, []);

  // If not admin, redirect (wait for auth to finish loading first)
  useEffect(() => {
    if (!authLoading && (!isAuthenticated || !isAdmin)) {
      router.push('/login');
    }
  }, [authLoading, isAuthenticated, isAdmin, router]);

  if (authLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="w-8 h-8 border-4 border-brand-primary border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (!isAuthenticated || !isAdmin) return null;

  // Admin step-up gate: every entry into the admin area must obtain a fresh
  // admin step-up token by re-verifying with the authenticator (or a recovery
  // code). The token is reusable for ~30 min while the admin stays here.
  if (!adminStepUpToken) {
    const submitStepUp = async (e: React.FormEvent) => {
      e.preventDefault();
      const submitted = useRecovery ? recoveryCode.trim().toLowerCase() : stepUpCode;
      if (!submitted) return;

      setStepUpSubmitting(true);
      setStepUpError('');
      try {
        const result = await authService.stepUp('Admin', { code: submitted });
        // CRITICAL ORDERING: ambient must be set BEFORE the state update,
        // otherwise children's useEffects (which fire bottom-up before the
        // parent's) will fetch admin endpoints without X-Step-Up-Token.
        setAmbientStepUpToken(result.token);
        setAdminStepUpToken(result.token);
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Invalid code.';
        setStepUpError(message);
        setStepUpCode('');
        setRecoveryCode('');
      } finally {
        setStepUpSubmitting(false);
      }
    };

    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 p-4">
        <div className="w-full max-w-sm bg-white rounded-lg shadow-lg p-8">
          <div className="text-center mb-6">
            <div className="w-12 h-12 rounded-full bg-brand-primary mx-auto flex items-center justify-center mb-3">
              <LayoutDashboard className="w-6 h-6 text-white" />
            </div>
            <h1 className="text-xl font-semibold text-brand-dark">{t('admin.2fa_title')}</h1>
            <p className="text-sm text-muted-foreground mt-1">
              {useRecovery
                ? 'Enter a recovery code (xxxx-xxxx-xxxx-xxxx).'
                : t('admin.2fa_hint')}
            </p>
          </div>
          <form onSubmit={submitStepUp} className="space-y-4">
            {!useRecovery ? (
              <div className="flex justify-center">
                <InputOTP
                  maxLength={6}
                  value={stepUpCode}
                  onChange={(v) => { setStepUpCode(v); setStepUpError(''); }}
                  autoFocus
                >
                  <InputOTPGroup>
                    <InputOTPSlot index={0} />
                    <InputOTPSlot index={1} />
                    <InputOTPSlot index={2} />
                    <InputOTPSlot index={3} />
                    <InputOTPSlot index={4} />
                    <InputOTPSlot index={5} />
                  </InputOTPGroup>
                </InputOTP>
              </div>
            ) : (
              <Input
                value={recoveryCode}
                onChange={(e) => { setRecoveryCode(e.target.value); setStepUpError(''); }}
                placeholder="xxxx-xxxx-xxxx-xxxx"
                autoFocus
                className="font-mono tracking-wider text-center"
              />
            )}

            {stepUpError && <p className="text-error text-sm text-center">{stepUpError}</p>}

            <Button
              type="submit"
              className="w-full bg-brand-primary hover:bg-brand-hover text-white"
              disabled={stepUpSubmitting || (useRecovery ? !recoveryCode.trim() : stepUpCode.length !== 6)}
            >
              {stepUpSubmitting ? '…' : (locale === 'fr' ? 'Vérifier' : 'Verify')}
            </Button>

            <button
              type="button"
              onClick={() => {
                setUseRecovery((v) => !v);
                setStepUpCode('');
                setRecoveryCode('');
                setStepUpError('');
              }}
              className="w-full text-sm text-brand-primary hover:underline"
            >
              {useRecovery ? 'Use authenticator code instead' : 'Use a recovery code instead'}
            </button>
          </form>
        </div>
      </div>
    );
  }

  const handleLogout = () => {
    setAmbientStepUpToken(null);
    setAdminStepUpToken(null);
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
              <SelectTrigger className="h-8 w-[140px] text-xs border-gray-200" aria-label="Language">
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
