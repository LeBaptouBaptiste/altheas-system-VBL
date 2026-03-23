'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { ShoppingCart, Search, Menu, Globe, User, LogOut, Settings, Package, FileText, Info, MessageSquare, Bot } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { useI18n } from '@/context/i18n-context';
import { useAuth } from '@/context/auth-context';
import { useCart } from '@/context/cart-context';
import type { Locale } from '@/lib/i18n';

export function Header() {
  const { t, locale, setLocale } = useI18n();
  const { user, isAuthenticated, isAdmin, logout } = useAuth();
  const { itemCount } = useCart();
  const router = useRouter();
  const [searchQuery, setSearchQuery] = useState('');
  const [mobileOpen, setMobileOpen] = useState(false);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      router.push(`/search?q=${encodeURIComponent(searchQuery.trim())}`);
    }
  };

  const handleLogout = () => {
    logout();
    setMobileOpen(false);
    router.push('/');
  };

  return (
    <header className="sticky top-0 z-50 border-b bg-white shadow-sm">
      {/* Top Bar */}
      <div className="bg-brand-dark text-white py-2 px-4">
        <div className="container mx-auto flex justify-between items-center text-sm">
          <span className="hidden sm:inline">{t('header.top_banner')}</span>
          <span className="sm:hidden text-xs">Althea Systems</span>
          <div className="flex items-center gap-4">
            {isAdmin && (
              <Link href="/admin" className="text-xs hover:text-brand-primary transition-colors">
                {t('nav.admin')}
              </Link>
            )}
            <Select value={locale} onValueChange={(v) => setLocale(v as Locale)}>
              <SelectTrigger className="h-7 w-[140px] border-white/20 bg-transparent text-white text-xs" aria-label="Language">
                <Globe className="w-3 h-3 mr-1" />
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="fr">Français</SelectItem>
                <SelectItem value="en">English</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </div>

      {/* Main Header */}
      <div className="container mx-auto px-4 py-3">
        <div className="flex items-center justify-between gap-4">
          {/* Logo */}
          <Link href="/" className="flex items-center gap-2 shrink-0">
            <div className="w-10 h-10 rounded-lg bg-brand-primary flex items-center justify-center">
              <span className="text-white font-bold text-xl" style={{ fontFamily: 'var(--font-heading)' }}>A</span>
            </div>
            <div className="hidden md:block">
              <h1 className="text-lg text-brand-dark m-0 leading-tight" style={{ fontFamily: 'var(--font-heading)' }}>
                Althea Systems
              </h1>
              <p className="text-xs text-muted-foreground m-0">Medical Excellence</p>
            </div>
          </Link>

          {/* Search Bar (Desktop) */}
          <form onSubmit={handleSearch} className="hidden md:flex flex-1 max-w-2xl">
            <div className="relative w-full">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
              <Input
                type="search"
                placeholder={t('header.search_placeholder')}
                className="w-full pl-10 pr-4"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                aria-label={t('common.search')}
              />
            </div>
          </form>

          {/* Actions */}
          <div className="flex items-center gap-1">
            {/* Account (Desktop) */}
            <div className="hidden md:flex items-center gap-1">
              {isAuthenticated ? (
                <>
                  <Link href="/account">
                    <Button variant="ghost" size="sm" className="text-brand-dark hover:bg-brand-light">
                      <User className="w-4 h-4 mr-1" />
                      <span className="max-w-[100px] truncate">{user?.name?.split(' ')[0]}</span>
                    </Button>
                  </Link>
                  <Button variant="ghost" size="icon" onClick={handleLogout} className="hover:bg-brand-light" aria-label={t('nav.logout')}>
                    <LogOut className="w-4 h-4 text-brand-dark" />
                  </Button>
                </>
              ) : (
                <Link href="/login">
                  <Button variant="ghost" size="sm" className="text-brand-dark hover:bg-brand-light">
                    <User className="w-4 h-4 mr-1" />
                    {t('nav.login')}
                  </Button>
                </Link>
              )}
            </div>

            {/* Cart */}
            <Link href="/cart">
              <Button variant="ghost" size="icon" className="relative hover:bg-brand-light" aria-label={`${t('nav.cart')} (${itemCount})`}>
                <ShoppingCart className="w-5 h-5 text-brand-dark" />
                {itemCount > 0 && (
                  <Badge className="absolute -top-1 -right-1 h-5 w-5 flex items-center justify-center p-0 bg-brand-primary text-white text-xs">
                    {itemCount}
                  </Badge>
                )}
              </Button>
            </Link>

            {/* Mobile Menu */}
            <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
              <SheetTrigger asChild>
                <Button variant="ghost" size="icon" className="md:hidden" aria-label="Menu">
                  <Menu className="w-5 h-5 text-brand-dark" />
                </Button>
              </SheetTrigger>
              <SheetContent side="right" className="w-[300px] p-0">
                {/* Mobile Search */}
                <div className="p-4">
                  <form onSubmit={(e) => { handleSearch(e); setMobileOpen(false); }}>
                    <div className="relative">
                      <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                      <Input
                        type="search"
                        placeholder={t('header.search_placeholder')}
                        className="pl-10"
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                      />
                    </div>
                  </form>
                </div>
                <Separator />
                <nav className="flex flex-col p-4 gap-1">
                  <MobileLink href="/" icon={<Package className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.home')}</MobileLink>
                  <MobileLink href="/categories" icon={<Package className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.categories')}</MobileLink>
                  <MobileLink href="/search" icon={<Search className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.search')}</MobileLink>
                  <MobileLink href="/contact" icon={<MessageSquare className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.contact')}</MobileLink>
                  <MobileLink href="/chatbot" icon={<Bot className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.chatbot')}</MobileLink>

                  <Separator className="my-2" />

                  {isAuthenticated ? (
                    <>
                      <MobileLink href="/account" icon={<Settings className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.settings')}</MobileLink>
                      <MobileLink href="/account/orders" icon={<Package className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.orders')}</MobileLink>
                      <Separator className="my-2" />
                      <MobileLink href="/cgu" icon={<FileText className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.cgu')}</MobileLink>
                      <MobileLink href="/mentions-legales" icon={<FileText className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.legal')}</MobileLink>
                      <MobileLink href="/a-propos" icon={<Info className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.about')}</MobileLink>
                      <Separator className="my-2" />
                      <button onClick={handleLogout} className="flex items-center gap-3 px-3 py-2 rounded-md text-destructive hover:bg-destructive/10 transition-colors">
                        <LogOut className="w-4 h-4" />
                        {t('nav.logout')}
                      </button>
                    </>
                  ) : (
                    <>
                      <MobileLink href="/login" icon={<User className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.login')}</MobileLink>
                      <MobileLink href="/register" icon={<User className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.register')}</MobileLink>
                      <Separator className="my-2" />
                      <MobileLink href="/cgu" icon={<FileText className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.cgu')}</MobileLink>
                      <MobileLink href="/mentions-legales" icon={<FileText className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.legal')}</MobileLink>
                      <MobileLink href="/contact" icon={<MessageSquare className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.contact')}</MobileLink>
                      <MobileLink href="/a-propos" icon={<Info className="w-4 h-4" />} onClick={() => setMobileOpen(false)}>{t('nav.about')}</MobileLink>
                    </>
                  )}
                </nav>
              </SheetContent>
            </Sheet>
          </div>
        </div>

        {/* Desktop Nav Links */}
        <nav className="hidden md:flex items-center gap-6 mt-2 text-sm" aria-label="Main navigation">
          <Link href="/" className="text-brand-dark hover:text-brand-primary transition-colors font-medium">{t('nav.home')}</Link>
          <Link href="/categories" className="text-brand-dark hover:text-brand-primary transition-colors font-medium">{t('nav.categories')}</Link>
          <Link href="/search" className="text-brand-dark hover:text-brand-primary transition-colors font-medium">{t('nav.search')}</Link>
          <Link href="/contact" className="text-brand-dark hover:text-brand-primary transition-colors font-medium">{t('nav.contact')}</Link>
          <Link href="/chatbot" className="text-brand-dark hover:text-brand-primary transition-colors font-medium">{t('nav.chatbot')}</Link>
        </nav>
      </div>
    </header>
  );
}

function MobileLink({ href, icon, children, onClick }: { href: string; icon: React.ReactNode; children: React.ReactNode; onClick?: () => void }) {
  return (
    <Link href={href} onClick={onClick} className="flex items-center gap-3 px-3 py-2 rounded-md hover:bg-brand-light transition-colors text-brand-dark">
      {icon}
      <span>{children}</span>
    </Link>
  );
}
