'use client';

import { Suspense, useState } from 'react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { AlertCircle, CheckCircle2, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { useI18n } from '@/context/i18n-context';
import { authService } from '@/lib/api-services';
import { ApiError } from '@/lib/api';

/**
 * Target of the password-reset link mailed at /forgot-password.
 *
 * URL shape: /reset-password?token=...
 *
 * On submit, POSTs /auth/reset-password and branches on the server's
 * `reason` field (returned alongside 400):
 *   - invalid_token / token_consumed / token_expired → terminal error screen
 *   - weak_password / passwords_mismatch             → inline form error
 *   - 200 → success screen with "go to login" CTA
 */
function ResetPasswordInner() {
  const { t, locale } = useI18n();
  const router = useRouter();
  const searchParams = useSearchParams();
  const token = searchParams.get('token');

  type Status =
    | 'form'                       // initial — show the password fields
    | 'success'                    // 200 from server — show success screen
    | 'token_invalid'              // terminal — needs a fresh /forgot-password run
    | 'token_consumed'             // terminal — already used
    | 'token_expired';             // terminal — too old
  const [status, setStatus] = useState<Status>(token ? 'form' : 'token_invalid');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;
    setError('');

    if (password !== confirm) {
      setError(locale === 'fr' ? 'Les mots de passe ne correspondent pas.' : 'Passwords do not match.');
      return;
    }
    if (password.length < 8) {
      setError(t('auth.password_rules'));
      return;
    }

    setSubmitting(true);
    try {
      await authService.resetPassword(token, password, confirm);
      setStatus('success');
    } catch (err) {
      if (err instanceof ApiError) {
        // Terminal token failures → switch to a dedicated screen so the
        // user doesn't keep typing into a form that can never succeed.
        if (err.reason === 'invalid_token') setStatus('token_invalid');
        else if (err.reason === 'token_consumed') setStatus('token_consumed');
        else if (err.reason === 'token_expired') setStatus('token_expired');
        else if (err.reason === 'weak_password') {
          setError(t('auth.password_rules'));
        } else if (err.reason === 'passwords_mismatch') {
          setError(locale === 'fr' ? 'Les mots de passe ne correspondent pas.' : 'Passwords do not match.');
        } else {
          setError(err.message);
        }
      } else {
        setError(t('common.error'));
      }
    } finally {
      setSubmitting(false);
    }
  };

  // ── Terminal token states ──────────────────────────
  if (status === 'token_invalid' || status === 'token_consumed' || status === 'token_expired') {
    const copy = {
      token_invalid: {
        title: locale === 'fr' ? 'Lien invalide' : 'Invalid link',
        body: locale === 'fr'
          ? "Ce lien n'est pas reconnu. Recommencez la procédure depuis l'écran « Mot de passe oublié »."
          : 'This link is not recognised. Restart from the "Forgot password" screen.',
      },
      token_consumed: {
        title: locale === 'fr' ? 'Lien déjà utilisé' : 'Link already used',
        body: locale === 'fr'
          ? 'Ce lien a déjà servi à réinitialiser votre mot de passe. Connectez-vous, ou demandez-en un nouveau si vous avez oublié à nouveau.'
          : 'This link has already been used. Log in, or request a new one if you forgot again.',
      },
      token_expired: {
        title: locale === 'fr' ? 'Lien expiré' : 'Link expired',
        body: locale === 'fr'
          ? "Ce lien a expiré (durée de validité : 30 min). Demandez-en un nouveau."
          : 'This link has expired (30 min validity). Request a new one.',
      },
    }[status];

    return (
      <div className="container mx-auto px-4 py-16 max-w-md">
        <Card><CardContent className="p-8 text-center space-y-4">
          <AlertCircle className="w-14 h-14 mx-auto text-warning" />
          <h1 className="text-2xl text-brand-dark">{copy.title}</h1>
          <p className="text-muted-foreground">{copy.body}</p>
          <div className="flex flex-col gap-2">
            <Button asChild className="bg-brand-primary hover:bg-brand-hover text-white">
              <Link href="/forgot-password">
                {locale === 'fr' ? 'Demander un nouveau lien' : 'Request a new link'}
              </Link>
            </Button>
            <Button asChild variant="outline">
              <Link href="/login">{locale === 'fr' ? 'Se connecter' : 'Log in'}</Link>
            </Button>
          </div>
        </CardContent></Card>
      </div>
    );
  }

  // ── Success ────────────────────────────────────────
  if (status === 'success') {
    return (
      <div className="container mx-auto px-4 py-16 max-w-md">
        <Card><CardContent className="p-8 text-center space-y-4">
          <CheckCircle2 className="w-14 h-14 mx-auto text-success" />
          <h1 className="text-2xl text-brand-dark">
            {locale === 'fr' ? 'Mot de passe mis à jour' : 'Password updated'}
          </h1>
          <p className="text-muted-foreground">
            {locale === 'fr'
              ? 'Vous pouvez désormais vous connecter avec votre nouveau mot de passe.'
              : 'You can now log in with your new password.'}
          </p>
          <Button
            onClick={() => router.push('/login')}
            className="bg-brand-primary hover:bg-brand-hover text-white w-full"
          >
            {locale === 'fr' ? 'Se connecter' : 'Log in'}
          </Button>
        </CardContent></Card>
      </div>
    );
  }

  // ── Form ───────────────────────────────────────────
  return (
    <div className="container mx-auto px-4 py-16 max-w-md">
      <Card><CardContent className="p-6">
        <h1 className="text-2xl text-brand-dark mb-2 text-center">
          {locale === 'fr' ? 'Choisissez un nouveau mot de passe' : 'Choose a new password'}
        </h1>
        <p className="text-sm text-muted-foreground text-center mb-6">
          {locale === 'fr'
            ? 'Saisissez et confirmez votre nouveau mot de passe.'
            : 'Enter and confirm your new password.'}
        </p>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <Label htmlFor="password">{locale === 'fr' ? 'Nouveau mot de passe' : 'New password'}</Label>
            <Input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoFocus
            />
            <p className="text-xs text-muted-foreground mt-1">{t('auth.password_rules')}</p>
          </div>
          <div>
            <Label htmlFor="confirm">{locale === 'fr' ? 'Confirmer le mot de passe' : 'Confirm password'}</Label>
            <Input
              id="confirm"
              type="password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              required
            />
          </div>

          {error && <p className="text-sm text-error" role="alert">{error}</p>}

          <Button
            type="submit"
            disabled={submitting || !password || !confirm}
            className="w-full bg-brand-primary hover:bg-brand-hover text-white"
          >
            {submitting
              ? '…'
              : (locale === 'fr' ? 'Réinitialiser' : 'Reset password')}
          </Button>
        </form>
      </CardContent></Card>
    </div>
  );
}

export default function ResetPasswordPage() {
  // useSearchParams() requires a Suspense boundary in the App Router.
  return (
    <Suspense fallback={
      <div className="container mx-auto px-4 py-16 max-w-md text-center">
        <Loader2 className="w-10 h-10 mx-auto text-brand-primary animate-spin" />
      </div>
    }>
      <ResetPasswordInner />
    </Suspense>
  );
}
