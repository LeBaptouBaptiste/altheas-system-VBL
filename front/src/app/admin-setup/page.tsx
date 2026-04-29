'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { InputOTP, InputOTPGroup, InputOTPSlot } from '@/components/ui/input-otp';
import { OtpQrCode } from '@/components/two-factor/otp-qr-code';
import { useAuth } from '@/context/auth-context';
import { authService } from '@/lib/api-services';
import type { TwoFactorSetupResult } from '@/lib/api-types';
import { toast } from 'sonner';
import { Copy, Check, Download, ShieldCheck } from 'lucide-react';

const SETUP_TOKEN_KEY = 'althea-setup-token';

type Step = 'loading' | 'showSecret' | 'verify' | 'recoveryCodes';

/**
 * Forced 2FA enrollment for admins. Lives at the top-level (outside the
 * (admin) route group) so its layout doesn't run the admin guard — the
 * admin doesn't have a usable access token yet, only a setupToken.
 *
 * Flow:
 *   1. Read setupToken from sessionStorage (set by the login page after a
 *      `mustSetupTwoFactor` outcome).
 *   2. POST /auth/2fa/setup using setupToken as bearer -> { secret, otpAuthUri }
 *   3. Show the secret (admin types it into their authenticator app).
 *   4. Admin types the resulting 6-digit code; we POST /auth/2fa/enable
 *      -> { recoveryCodes, auth }. applyAuth() stores the real access token.
 *   5. Show the recovery codes ONCE; admin clicks "I saved them" -> /admin.
 *
 * NOTE: a QR-code rendering will land in a follow-up commit; for now the
 * admin types the base32 secret manually.
 */
export default function AdminSetupPage() {
  const router = useRouter();
  const { applyAuth } = useAuth();

  const [setupToken, setSetupToken] = useState<string | null>(null);
  const [step, setStep] = useState<Step>('loading');
  const [setup, setSetup] = useState<TwoFactorSetupResult | null>(null);
  const [code, setCode] = useState('');
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [secretCopied, setSecretCopied] = useState(false);
  const [savedConfirmed, setSavedConfirmed] = useState(false);

  // 1. Resolve setupToken + start setup
  useEffect(() => {
    const token = sessionStorage.getItem(SETUP_TOKEN_KEY);
    if (!token) {
      router.replace('/login');
      return;
    }
    setSetupToken(token);

    authService.setupTwoFactor(token)
      .then((result) => {
        setSetup(result);
        setStep('showSecret');
      })
      .catch(() => {
        toast.error('Setup token expired. Please log in again.');
        sessionStorage.removeItem(SETUP_TOKEN_KEY);
        router.replace('/login');
      });
  }, [router]);

  const copySecret = async () => {
    if (!setup) return;
    await navigator.clipboard.writeText(setup.secret);
    setSecretCopied(true);
    setTimeout(() => setSecretCopied(false), 2000);
  };

  const handleVerify = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!setupToken) return;
    setSubmitting(true);
    try {
      const result = await authService.enableTwoFactor(code, setupToken);
      setRecoveryCodes(result.recoveryCodes);
      // The setupToken is now consumed; clear it.
      sessionStorage.removeItem(SETUP_TOKEN_KEY);
      // Real session starts here.
      applyAuth(result.auth);
      setStep('recoveryCodes');
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Invalid code.';
      toast.error(message);
      setCode('');
    } finally {
      setSubmitting(false);
    }
  };

  const downloadCodes = () => {
    const blob = new Blob(
      [
        'Althea Systems — Recovery Codes\n',
        'Generated: ' + new Date().toISOString() + '\n',
        '\n',
        'Each code can be used ONCE if you lose access to your authenticator.\n',
        'Keep this file in a secure location.\n',
        '\n',
        ...recoveryCodes.map((c) => c + '\n'),
      ],
      { type: 'text/plain' },
    );
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'althea-recovery-codes.txt';
    a.click();
    URL.revokeObjectURL(url);
  };

  const finish = () => {
    router.replace('/admin');
  };

  // ── Renders ────────────────────────────────────────

  if (step === 'loading' || !setup) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="w-8 h-8 border-4 border-brand-primary border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 p-4">
      <Card className="w-full max-w-lg">
        <CardContent className="p-8">
          <div className="flex items-center gap-3 mb-6">
            <div className="w-10 h-10 rounded-full bg-brand-primary flex items-center justify-center">
              <ShieldCheck className="w-5 h-5 text-white" />
            </div>
            <div>
              <h1 className="text-xl font-semibold text-brand-dark">Two-factor setup</h1>
              <p className="text-sm text-muted-foreground">
                Required for admin access.
              </p>
            </div>
          </div>

          {step === 'showSecret' && (
            <>
              <ol className="text-sm text-gray-700 space-y-2 mb-4 list-decimal list-inside">
                <li>Open your authenticator app (Google Authenticator, Authy, 1Password, Bitwarden…).</li>
                <li>Scan the QR code below — or type the secret manually.</li>
                <li>Enter the 6-digit code your app generates.</li>
              </ol>

              <div className="flex justify-center mb-4">
                <OtpQrCode uri={setup.otpAuthUri} />
              </div>

              <details className="text-sm text-gray-700 mb-2">
                <summary className="cursor-pointer text-brand-primary hover:underline mb-2">
                  Can&apos;t scan? Show the secret to type manually
                </summary>
                <div className="bg-gray-50 border rounded-md p-4 mt-2">
                  <p className="text-xs text-muted-foreground mb-2">Secret (account name: AltheaSystems)</p>
                  <div className="flex items-center gap-2">
                    <code className="flex-1 font-mono text-sm tracking-wider break-all">
                      {setup.secret}
                    </code>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={copySecret}
                      aria-label="Copy secret"
                    >
                      {secretCopied ? <Check className="w-4 h-4 text-green-600" /> : <Copy className="w-4 h-4" />}
                    </Button>
                  </div>
                </div>
              </details>

              <Button
                type="button"
                onClick={() => setStep('verify')}
                className="w-full bg-brand-primary hover:bg-brand-hover text-white mt-4"
              >
                I&apos;ve added it — next
              </Button>
            </>
          )}

          {step === 'verify' && (
            <form onSubmit={handleVerify}>
              <p className="text-sm text-gray-700 mb-4">
                Type the 6-digit code from your authenticator:
              </p>
              <div className="flex justify-center mb-6">
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
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setStep('showSecret')}
                  className="flex-1"
                >
                  Back
                </Button>
                <Button
                  type="submit"
                  disabled={code.length !== 6 || submitting}
                  className="flex-1 bg-brand-primary hover:bg-brand-hover text-white"
                >
                  {submitting ? 'Verifying…' : 'Verify and enable'}
                </Button>
              </div>
            </form>
          )}

          {step === 'recoveryCodes' && (
            <>
              <div className="bg-amber-50 border border-amber-200 rounded-md p-3 mb-4 text-sm text-amber-900">
                Save these recovery codes <strong>now</strong> — they grant access if you lose your
                authenticator and won&apos;t be shown again.
              </div>

              <div className="grid grid-cols-2 gap-2 font-mono text-sm bg-gray-50 border rounded-md p-4 mb-4">
                {recoveryCodes.map((c) => (
                  <div key={c} className="text-center">{c}</div>
                ))}
              </div>

              <Button
                type="button"
                variant="outline"
                onClick={downloadCodes}
                className="w-full mb-3"
              >
                <Download className="w-4 h-4 mr-2" />
                Download as .txt
              </Button>

              <label className="flex items-start gap-2 mb-4 text-sm cursor-pointer">
                <input
                  type="checkbox"
                  checked={savedConfirmed}
                  onChange={(e) => setSavedConfirmed(e.target.checked)}
                  className="mt-0.5"
                />
                <span>I&apos;ve stored these codes in a safe place.</span>
              </label>

              <Button
                type="button"
                onClick={finish}
                disabled={!savedConfirmed}
                className="w-full bg-brand-primary hover:bg-brand-hover text-white"
              >
                Continue to admin
              </Button>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
