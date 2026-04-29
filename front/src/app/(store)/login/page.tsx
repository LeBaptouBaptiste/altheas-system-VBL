'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { InputOTP, InputOTPGroup, InputOTPSlot } from '@/components/ui/input-otp';
import { useI18n } from '@/context/i18n-context';
import { useAuth } from '@/context/auth-context';
import { getErrorMessage } from '@/lib/api-errors';
import { toast } from 'sonner';

const SETUP_TOKEN_KEY = 'althea-setup-token';

type Stage =
  | { kind: 'credentials' }
  | { kind: 'twoFactor'; challengeToken: string };

export default function LoginPage() {
  const { t } = useI18n();
  const { login, completeTwoFactorChallenge } = useAuth();
  const router = useRouter();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [code, setCode] = useState('');
  const [useRecovery, setUseRecovery] = useState(false);
  const [recoveryCode, setRecoveryCode] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [stage, setStage] = useState<Stage>({ kind: 'credentials' });

  const handleCredentialsSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setSubmitting(true);
    try {
      const result = await login(email, password);
      switch (result.kind) {
        case 'success':
          toast.success(t('auth.login') + ' ✓');
          router.push('/');
          break;
        case 'twoFactorRequired':
          setStage({ kind: 'twoFactor', challengeToken: result.challengeToken });
          break;
        case 'mustSetupTwoFactor':
          // Stash the setupToken in sessionStorage so the /admin-setup page
          // can pick it up. sessionStorage > URL query: not logged in browser
          // history, server logs, or referer headers.
          sessionStorage.setItem(SETUP_TOKEN_KEY, result.setupToken);
          router.push('/admin-setup');
          break;
        case 'error':
          setError(getErrorMessage(result.error, t));
          break;
      }
    } finally {
      setSubmitting(false);
    }
  };

  const handleTwoFactorSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (stage.kind !== 'twoFactor') return;
    const submittedCode = useRecovery ? recoveryCode.trim().toLowerCase() : code;
    if (!submittedCode) return;

    setError('');
    setSubmitting(true);
    try {
      const result = await completeTwoFactorChallenge(stage.challengeToken, submittedCode);
      if (result.kind === 'success') {
        toast.success(t('auth.login') + ' ✓');
        router.push('/');
      } else if (result.kind === 'error') {
        setError(getErrorMessage(result.error, t));
        setCode('');
        setRecoveryCode('');
      }
    } finally {
      setSubmitting(false);
    }
  };

  // ── Stage: 2FA challenge ───────────────────────────
  if (stage.kind === 'twoFactor') {
    return (
      <div className="container mx-auto px-4 py-16 max-w-md">
        <Card>
          <CardContent className="p-6">
            <h1 className="text-2xl text-brand-dark mb-2 text-center">{t('auth.2fa_challenge_title')}</h1>
            <p className="text-sm text-muted-foreground text-center mb-6">
              {useRecovery ? t('auth.2fa_recovery_hint') : t('auth.2fa_authenticator_hint')}
            </p>

            <form onSubmit={handleTwoFactorSubmit} className="space-y-4">
              {!useRecovery ? (
                <div className="flex justify-center">
                  <InputOTP maxLength={6} value={code} onChange={setCode} autoFocus>
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
                  onChange={(e) => setRecoveryCode(e.target.value)}
                  placeholder={t('2fa.stepup_recovery_placeholder')}
                  autoFocus
                  dir="ltr"
                  className="font-mono tracking-wider text-center"
                />
              )}

              {error && <p className="text-sm text-error text-center" role="alert">{error}</p>}

              <Button
                type="submit"
                className="w-full bg-brand-primary hover:bg-brand-hover text-white"
                disabled={submitting || (useRecovery ? !recoveryCode.trim() : code.length !== 6)}
              >
                {submitting ? '…' : t('auth.2fa_verify')}
              </Button>

              <button
                type="button"
                onClick={() => {
                  setUseRecovery((v) => !v);
                  setCode('');
                  setRecoveryCode('');
                  setError('');
                }}
                className="w-full text-sm text-brand-primary hover:underline"
              >
                {useRecovery ? t('auth.2fa_use_authenticator') : t('auth.2fa_use_recovery')}
              </button>

              <button
                type="button"
                onClick={() => {
                  setStage({ kind: 'credentials' });
                  setCode('');
                  setRecoveryCode('');
                  setUseRecovery(false);
                  setError('');
                }}
                className="w-full text-sm text-muted-foreground hover:underline"
              >
                {t('auth.2fa_back_login')}
              </button>
            </form>
          </CardContent>
        </Card>
      </div>
    );
  }

  // ── Stage: credentials ─────────────────────────────
  return (
    <div className="container mx-auto px-4 py-16 max-w-md">
      <Card>
        <CardContent className="p-6">
          <h1 className="text-2xl text-brand-dark mb-6 text-center">{t('auth.login')}</h1>
          <form onSubmit={handleCredentialsSubmit} className="space-y-4">
            <div><Label htmlFor="email">{t('auth.email')}</Label><Input id="email" type="email" value={email} onChange={e => setEmail(e.target.value)} required /></div>
            <div><Label htmlFor="password">{t('auth.password')}</Label><Input id="password" type="password" value={password} onChange={e => setPassword(e.target.value)} required /></div>
            {error && <p className="text-sm text-error" role="alert">{error}</p>}
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2"><Checkbox id="remember" /><Label htmlFor="remember" className="text-sm font-normal">{t('auth.remember_me')}</Label></div>
              <Link href="/forgot-password" className="text-sm text-brand-primary hover:underline">{t('auth.forgot_password')}</Link>
            </div>
            <Button type="submit" className="w-full bg-brand-primary hover:bg-brand-hover text-white" disabled={submitting}>{submitting ? '…' : t('auth.login')}</Button>
          </form>
          <p className="text-sm text-center mt-4 text-muted-foreground">{t('auth.no_account')} <Link href="/register" className="text-brand-primary hover:underline">{t('auth.register')}</Link></p>
        </CardContent>
      </Card>
    </div>
  );
}
