'use client';

import { useEffect, useState, Suspense } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { CheckCircle2, AlertCircle, Loader2, Mail } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { useI18n } from '@/context/i18n-context';
import { authService } from '@/lib/api-services';
import { ApiError } from '@/lib/api';

type Status = 'pending' | 'success' | 'invalid' | 'consumed' | 'expired' | 'error';

/**
 * Confirmation link target — `/confirm-email?token=…`. Reads the token,
 * POSTs to /auth/confirm-email and renders one of four UX states:
 *
 *   - success   → "you're confirmed, go log in"
 *   - consumed  → "this link was already used, go log in"  (idempotent UX)
 *   - expired   → "this link has expired, request a new one"
 *   - invalid   → "wrong / missing token" — same copy as expired-ish
 *   - error     → network / 500 — show a generic retry message
 *
 * The page is a thin shell because the real branching is server-side via
 * the `reason` field on the 400 response.
 */
function ConfirmEmailInner() {
  const { locale } = useI18n();
  const searchParams = useSearchParams();
  const token = searchParams.get('token');
  const [status, setStatus] = useState<Status>('pending');

  useEffect(() => {
    if (!token) {
      setStatus('invalid');
      return;
    }
    let cancelled = false;
    authService.confirmEmail(token)
      .then(() => { if (!cancelled) setStatus('success'); })
      .catch((err: unknown) => {
        if (cancelled) return;
        // ApiError carries the server's machine-readable `reason` field —
        // that's what lets us pick the right copy without parsing messages.
        if (err instanceof ApiError) {
          const reason = err.reason;
          if (reason === 'token_consumed') setStatus('consumed');
          else if (reason === 'token_expired') setStatus('expired');
          else if (reason === 'invalid_token') setStatus('invalid');
          else setStatus('error');
        } else {
          setStatus('error');
        }
      });
    return () => { cancelled = true; };
  }, [token]);

  return (
    <div className="container mx-auto px-4 py-16 max-w-md">
      <Card><CardContent className="p-8 text-center space-y-4">
        {status === 'pending' && (
          <>
            <Loader2 className="w-12 h-12 mx-auto text-brand-primary animate-spin" />
            <h1 className="text-xl text-brand-dark">
              {locale === 'fr' ? 'Confirmation en cours…' : 'Confirming…'}
            </h1>
          </>
        )}

        {status === 'success' && (
          <>
            <CheckCircle2 className="w-14 h-14 mx-auto text-success" />
            <h1 className="text-2xl text-brand-dark">
              {locale === 'fr' ? 'Email confirmé' : 'Email confirmed'}
            </h1>
            <p className="text-muted-foreground">
              {locale === 'fr'
                ? 'Votre compte est prêt — vous pouvez vous connecter.'
                : 'Your account is ready — you can now log in.'}
            </p>
            <Button asChild className="bg-brand-primary hover:bg-brand-hover text-white w-full">
              <Link href="/login">
                {locale === 'fr' ? 'Se connecter' : 'Log in'}
              </Link>
            </Button>
          </>
        )}

        {status === 'consumed' && (
          <>
            <Mail className="w-14 h-14 mx-auto text-brand-primary" />
            <h1 className="text-2xl text-brand-dark">
              {locale === 'fr' ? 'Lien déjà utilisé' : 'Link already used'}
            </h1>
            <p className="text-muted-foreground">
              {locale === 'fr'
                ? 'Ce lien de confirmation a déjà été utilisé. Vous pouvez vous connecter.'
                : 'This confirmation link has already been used. You can log in.'}
            </p>
            <Button asChild className="bg-brand-primary hover:bg-brand-hover text-white w-full">
              <Link href="/login">
                {locale === 'fr' ? 'Se connecter' : 'Log in'}
              </Link>
            </Button>
          </>
        )}

        {status === 'expired' && (
          <>
            <AlertCircle className="w-14 h-14 mx-auto text-warning" />
            <h1 className="text-2xl text-brand-dark">
              {locale === 'fr' ? 'Lien expiré' : 'Link expired'}
            </h1>
            <p className="text-muted-foreground">
              {locale === 'fr'
                ? 'Ce lien de confirmation a expiré. Connectez-vous pour en recevoir un nouveau.'
                : 'This confirmation link has expired. Log in to request a new one.'}
            </p>
            <Button asChild className="bg-brand-primary hover:bg-brand-hover text-white w-full">
              <Link href="/login">
                {locale === 'fr' ? 'Se connecter' : 'Log in'}
              </Link>
            </Button>
          </>
        )}

        {(status === 'invalid' || status === 'error') && (
          <>
            <AlertCircle className="w-14 h-14 mx-auto text-error" />
            <h1 className="text-2xl text-brand-dark">
              {locale === 'fr' ? 'Lien invalide' : 'Invalid link'}
            </h1>
            <p className="text-muted-foreground">
              {locale === 'fr'
                ? "Ce lien n'est pas reconnu. Demandez un nouveau lien depuis l'écran de connexion."
                : 'This link is not recognised. Request a new one from the login screen.'}
            </p>
            <Button asChild variant="outline" className="w-full">
              <Link href="/login">
                {locale === 'fr' ? 'Aller à la connexion' : 'Go to login'}
              </Link>
            </Button>
          </>
        )}
      </CardContent></Card>
    </div>
  );
}

export default function ConfirmEmailPage() {
  // useSearchParams() requires a Suspense boundary in the App Router; the
  // fallback shows the same spinner as the in-flight state so there's no
  // visible flash.
  return (
    <Suspense fallback={
      <div className="container mx-auto px-4 py-16 max-w-md text-center">
        <Loader2 className="w-10 h-10 mx-auto text-brand-primary animate-spin" />
      </div>
    }>
      <ConfirmEmailInner />
    </Suspense>
  );
}
