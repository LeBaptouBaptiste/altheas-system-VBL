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
  const { t } = useI18n();
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
      setError(t('auth.passwords_no_match'));
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
          setError(t('auth.passwords_no_match'));
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
        title: t('auth.link_invalid_title'),
        body: t('auth.reset_link_invalid_body'),
      },
      token_consumed: {
        title: t('auth.link_used_title'),
        body: t('auth.reset_link_consumed_body'),
      },
      token_expired: {
        title: t('auth.link_expired_title'),
        body: t('auth.reset_link_expired_body'),
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
                {t('auth.request_new_link')}
              </Link>
            </Button>
            <Button asChild variant="outline">
              <Link href="/login">{t('auth.log_in')}</Link>
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
            {t('auth.password_updated_title')}
          </h1>
          <p className="text-muted-foreground">
            {t('auth.password_updated_body')}
          </p>
          <Button
            onClick={() => router.push('/login')}
            className="bg-brand-primary hover:bg-brand-hover text-white w-full"
          >
            {t('auth.log_in')}
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
          {t('auth.choose_new_password')}
        </h1>
        <p className="text-sm text-muted-foreground text-center mb-6">
          {t('auth.choose_new_password_hint')}
        </p>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <Label htmlFor="password">{t('auth.new_password')}</Label>
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
            <Label htmlFor="confirm">{t('auth.confirm_password_label')}</Label>
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
              : t('auth.reset_button')}
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
