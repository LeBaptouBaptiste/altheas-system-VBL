'use client';

import { I18nProvider } from '@/context/i18n-context';
import { AuthProvider } from '@/context/auth-context';
import { CartProvider } from '@/context/cart-context';
import { StepUpProvider } from '@/components/two-factor/step-up-provider';
import { Toaster } from '@/components/ui/sonner';
import { HtmlDirSync } from '@/components/HtmlDirSync';

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <I18nProvider>
      <HtmlDirSync />
      <AuthProvider>
        <CartProvider>
          <StepUpProvider>
            {children}
            <Toaster position="top-right" />
          </StepUpProvider>
        </CartProvider>
      </AuthProvider>
    </I18nProvider>
  );
}
