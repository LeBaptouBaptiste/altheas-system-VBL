'use client';

import { I18nProvider } from '@/context/i18n-context';
import { AuthProvider } from '@/context/auth-context';
import { CartProvider } from '@/context/cart-context';
import { Toaster } from '@/components/ui/sonner';

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <I18nProvider>
      <AuthProvider>
        <CartProvider>
          {children}
          <Toaster position="top-right" />
        </CartProvider>
      </AuthProvider>
    </I18nProvider>
  );
}
