'use client';

import { I18nProvider } from '@/context/i18n-context';
import { AuthProvider } from '@/context/auth-context';
import { CartProvider } from '@/context/cart-context';
import { Toaster } from '@/components/ui/sonner';
import { HtmlDirSync } from '@/components/HtmlDirSync';

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <I18nProvider>
      <HtmlDirSync />
      <AuthProvider>
        <CartProvider>
          {children}
          <Toaster position="top-right" />
        </CartProvider>
      </AuthProvider>
    </I18nProvider>
  );
}
