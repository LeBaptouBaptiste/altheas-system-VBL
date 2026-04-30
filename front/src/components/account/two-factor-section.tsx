'use client';

import { useEffect, useState } from 'react';
import { ShieldCheck, ShieldAlert, Copy, Check, Download, Loader2, KeyRound } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { InputOTP, InputOTPGroup, InputOTPSlot } from '@/components/ui/input-otp';
import { OtpQrCode } from '@/components/two-factor/otp-qr-code';
import { useStepUp } from '@/components/two-factor/step-up-provider';
import { authService } from '@/lib/api-services';
import type { TwoFactorSetupResult, TwoFactorStatus } from '@/lib/api-types';
import { getErrorMessage } from '@/lib/api-errors';
import { useI18n } from '@/context/i18n-context';
import { useAuth } from '@/context/auth-context';
import { toast } from 'sonner';

type View =
  | { kind: 'loading' }
  | { kind: 'status'; status: TwoFactorStatus }
  | { kind: 'setupSecret'; setup: TwoFactorSetupResult }
  | { kind: 'setupVerify'; setup: TwoFactorSetupResult }
  | { kind: 'recoveryCodes'; codes: string[] };

export function TwoFactorSection() {
  const { t, locale } = useI18n();
  const { applyAuth } = useAuth();
  const { withStepUp } = useStepUp();

  const [view, setView] = useState<View>({ kind: 'loading' });
  const [submitting, setSubmitting] = useState(false);
  const [code, setCode] = useState('');
  const [secretCopied, setSecretCopied] = useState(false);
  const [savedConfirmed, setSavedConfirmed] = useState(false);

  // ── Load status on mount ──────────────────────────
  useEffect(() => {
    authService.getTwoFactorStatus()
      .then((status) => setView({ kind: 'status', status }))
      .catch((err) => toast.error(getErrorMessage(err, t)));
  }, [t]);

  // ── Action handlers ───────────────────────────────

  const startSetup = async () => {
    setSubmitting(true);
    try {
      const setup = await authService.setupTwoFactor();
      setView({ kind: 'setupSecret', setup });
    } catch {
      toast.error(t('2fa.section_setup_failed'));
    } finally {
      setSubmitting(false);
    }
  };

  const verifyAndEnable = async (e: React.FormEvent) => {
    e.preventDefault();
    if (view.kind !== 'setupVerify') return;
    setSubmitting(true);
    try {
      const result = await authService.enableTwoFactor(code);
      // Persist the new amr=mfa token so subsequent requests use an
      // up-to-date JWT (and LastLogin gets refreshed server-side).
      applyAuth(result.auth);
      setCode('');
      setSavedConfirmed(false);
      setView({ kind: 'recoveryCodes', codes: result.recoveryCodes });
      toast.success(t('2fa.section_enabled_toast'));
    } catch (err) {
      toast.error(getErrorMessage(err, t));
      setCode('');
    } finally {
      setSubmitting(false);
    }
  };

  const disable = async () => {
    setSubmitting(true);
    try {
      const out = await withStepUp((token) => authService.disableTwoFactor(token));
      if (out !== null) {
        const status = await authService.getTwoFactorStatus();
        setView({ kind: 'status', status });
        toast.success(t('2fa.section_disabled_toast'));
      }
    } catch (err) {
      toast.error(getErrorMessage(err, t));
    } finally {
      setSubmitting(false);
    }
  };

  const regenerate = async () => {
    setSubmitting(true);
    try {
      const out = await withStepUp((token) => authService.regenerateRecoveryCodes(token));
      if (out !== null) {
        setView({ kind: 'recoveryCodes', codes: out.recoveryCodes });
        setSavedConfirmed(false);
        toast.success(t('2fa.section_codes_regenerated_toast'));
      }
    } catch (err) {
      toast.error(getErrorMessage(err, t));
    } finally {
      setSubmitting(false);
    }
  };

  const finishRecoveryCodes = async () => {
    const status = await authService.getTwoFactorStatus();
    setView({ kind: 'status', status });
  };

  const copySecret = async () => {
    if (view.kind !== 'setupSecret') return;
    await navigator.clipboard.writeText(view.setup.secret);
    setSecretCopied(true);
    setTimeout(() => setSecretCopied(false), 2000);
  };

  const downloadCodes = (codes: string[]) => {
    const blob = new Blob(
      [
        t('2fa.recovery_codes_file_header') + '\n',
        'Generated: ' + new Date().toISOString() + '\n',
        '\n',
        t('2fa.recovery_codes_file_intro_use') + '\n',
        t('2fa.recovery_codes_file_intro_secure') + '\n',
        '\n',
        ...codes.map((c) => c + '\n'),
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

  // ─────────────────────────────────────────────────
  //  Renders
  // ─────────────────────────────────────────────────

  if (view.kind === 'loading') {
    return (
      <Card><CardContent className="p-6 flex justify-center">
        <Loader2 className="w-6 h-6 animate-spin text-brand-primary" />
      </CardContent></Card>
    );
  }

  return (
    <Card>
      <CardContent className="p-6">
        {/* Status — enabled */}
        {view.kind === 'status' && view.status.enabled && (
          <>
            <div className="flex items-start gap-3 mb-4">
              <div className="w-10 h-10 rounded-full bg-success/10 flex items-center justify-center shrink-0">
                <ShieldCheck className="w-5 h-5 text-success" />
              </div>
              <div>
                <h3 className="font-semibold text-brand-dark">
                  {t('2fa.section_enabled_title')}
                </h3>
                <p className="text-sm text-muted-foreground">
                  {t('2fa.section_enabled_at')}{' '}
                  {view.status.enabledAt
                    ? new Date(view.status.enabledAt).toLocaleDateString(locale === 'fr' ? 'fr-FR' : 'en-US')
                    : '—'}
                  .{' '}
                  {view.status.recoveryCodesRemaining}{' '}
                  {view.status.recoveryCodesRemaining === 1
                    ? t('2fa.section_codes_remaining_one')
                    : t('2fa.section_codes_remaining_other')}
                </p>
              </div>
            </div>
            <div className="flex flex-wrap gap-2">
              <Button variant="outline" onClick={regenerate} disabled={submitting}>
                <KeyRound className="w-4 h-4 me-2" />
                {t('2fa.section_regenerate_button')}
              </Button>
              <Button variant="destructive" onClick={disable} disabled={submitting}>
                {t('2fa.section_disable_button')}
              </Button>
            </div>
          </>
        )}

        {/* Status — disabled */}
        {view.kind === 'status' && !view.status.enabled && (
          <>
            <div className="flex items-start gap-3 mb-4">
              <div className="w-10 h-10 rounded-full bg-amber-100 flex items-center justify-center shrink-0">
                <ShieldAlert className="w-5 h-5 text-amber-700" />
              </div>
              <div>
                <h3 className="font-semibold text-brand-dark">
                  {t('2fa.section_disabled_title')}
                </h3>
                <p className="text-sm text-muted-foreground">
                  {t('2fa.section_add_security')}
                </p>
              </div>
            </div>
            <Button
              onClick={startSetup}
              disabled={submitting}
              className="bg-brand-primary hover:bg-brand-hover text-white"
            >
              {submitting ? '…' : t('2fa.section_enable_button')}
            </Button>
          </>
        )}

        {/* Setup step 1: show secret + QR */}
        {view.kind === 'setupSecret' && (
          <>
            <h3 className="font-semibold text-brand-dark mb-3">
              {t('2fa.setup_step1_title')}
            </h3>
            <ol className="text-sm text-gray-700 space-y-1 mb-4 list-decimal list-inside">
              <li>{t('2fa.setup_intro_open_app')}</li>
              <li>{t('2fa.setup_intro_scan_or_type')}</li>
            </ol>

            <div className="flex justify-center mb-4">
              <OtpQrCode uri={view.setup.otpAuthUri} />
            </div>

            <details className="text-sm text-gray-700 mb-4">
              <summary className="cursor-pointer text-brand-primary hover:underline mb-2">
                {t('2fa.setup_cant_scan')}
              </summary>
              <div className="bg-gray-50 border rounded-md p-4 mt-2">
                <p className="text-xs text-muted-foreground mb-2">
                  {t('2fa.setup_secret_label')}
                </p>
                <div className="flex items-center gap-2">
                  <code className="flex-1 font-mono text-sm tracking-wider break-all">
                    {view.setup.secret}
                  </code>
                  <Button type="button" variant="ghost" size="icon" onClick={copySecret}>
                    {secretCopied ? <Check className="w-4 h-4 text-green-600" /> : <Copy className="w-4 h-4" />}
                  </Button>
                </div>
              </div>
            </details>

            <div className="flex gap-2">
              <Button
                variant="outline"
                onClick={() => authService.getTwoFactorStatus().then((s) => setView({ kind: 'status', status: s }))}
              >
                {t('common.cancel')}
              </Button>
              <Button
                onClick={() => setView({ kind: 'setupVerify', setup: view.setup })}
                className="bg-brand-primary hover:bg-brand-hover text-white"
              >
                {t('common.next')}
              </Button>
            </div>
          </>
        )}

        {/* Setup step 2: verify */}
        {view.kind === 'setupVerify' && (
          <form onSubmit={verifyAndEnable}>
            <h3 className="font-semibold text-brand-dark mb-3">
              {t('2fa.setup_step2_title')}
            </h3>
            <p className="text-sm text-gray-700 mb-4">
              {t('2fa.setup_enter_code')}
            </p>
            <div className="flex justify-center mb-4">
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
                onClick={() => setView({ kind: 'setupSecret', setup: view.setup })}
              >
                {t('common.back')}
              </Button>
              <Button
                type="submit"
                disabled={submitting || code.length !== 6}
                className="flex-1 bg-brand-primary hover:bg-brand-hover text-white"
              >
                {submitting ? '…' : t('2fa.setup_verify_enable')}
              </Button>
            </div>
          </form>
        )}

        {/* Recovery codes (post-enable or post-regenerate) */}
        {view.kind === 'recoveryCodes' && (
          <>
            <h3 className="font-semibold text-brand-dark mb-3">
              {t('2fa.recovery_codes_title')}
            </h3>
            <div className="bg-amber-50 border border-amber-200 rounded-md p-3 mb-4 text-sm text-amber-900">
              {t('2fa.recovery_codes_warning')}
            </div>
            <div className="grid grid-cols-2 gap-2 font-mono text-sm bg-gray-50 border rounded-md p-4 mb-4">
              {view.codes.map((c) => (
                <div key={c} className="text-center">{c}</div>
              ))}
            </div>
            <Button
              type="button"
              variant="outline"
              onClick={() => downloadCodes(view.codes)}
              className="w-full mb-3"
            >
              <Download className="w-4 h-4 me-2" />
              {t('2fa.recovery_codes_download')}
            </Button>
            <label className="flex items-start gap-2 mb-4 text-sm cursor-pointer">
              <input
                type="checkbox"
                checked={savedConfirmed}
                onChange={(e) => setSavedConfirmed(e.target.checked)}
                className="mt-0.5"
              />
              <span>{t('2fa.recovery_codes_confirm_saved')}</span>
            </label>
            <Button
              type="button"
              onClick={finishRecoveryCodes}
              disabled={!savedConfirmed}
              className="w-full bg-brand-primary hover:bg-brand-hover text-white"
            >
              {t('2fa.recovery_codes_done')}
            </Button>
          </>
        )}
      </CardContent>
    </Card>
  );
}
