'use client';

import { useEffect, useState } from 'react';
import { ShieldCheck, ShieldAlert, Copy, Check, Download, Loader2, KeyRound } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog';
import { InputOTP, InputOTPGroup, InputOTPSlot } from '@/components/ui/input-otp';
import { OtpQrCode } from '@/components/two-factor/otp-qr-code';
import { authService } from '@/lib/api-services';
import { isStepUpRequired } from '@/lib/api';
import type { TwoFactorSetupResult, TwoFactorStatus } from '@/lib/api-types';
import { useI18n } from '@/context/i18n-context';
import { useAuth } from '@/context/auth-context';
import { toast } from 'sonner';

type View =
  | { kind: 'loading' }
  | { kind: 'status'; status: TwoFactorStatus }
  | { kind: 'setupSecret'; setup: TwoFactorSetupResult }
  | { kind: 'setupVerify'; setup: TwoFactorSetupResult }
  | { kind: 'recoveryCodes'; codes: string[] };

/**
 * Step-up flow as a Promise: opens the dialog, the user types a code,
 * we POST /auth/step-up, and the dialog resolves with the token.
 * Reject if the user cancels.
 */
type StepUpState =
  | null
  | {
      resolve: (token: string) => void;
      reject: (reason: Error) => void;
    };

export function TwoFactorSection() {
  const { locale } = useI18n();
  const { applyAuth } = useAuth();
  const fr = locale === 'fr';

  const [view, setView] = useState<View>({ kind: 'loading' });
  const [submitting, setSubmitting] = useState(false);
  const [code, setCode] = useState('');
  const [secretCopied, setSecretCopied] = useState(false);
  const [savedConfirmed, setSavedConfirmed] = useState(false);

  // Step-up dialog state
  const [stepUpOpen, setStepUpOpen] = useState(false);
  const [stepUpResolver, setStepUpResolver] = useState<StepUpState>(null);
  const [stepUpCode, setStepUpCode] = useState('');
  const [stepUpUseRecovery, setStepUpUseRecovery] = useState(false);
  const [stepUpRecoveryCode, setStepUpRecoveryCode] = useState('');
  const [stepUpError, setStepUpError] = useState('');
  const [stepUpSubmitting, setStepUpSubmitting] = useState(false);

  // ── Load status on mount ──────────────────────────
  useEffect(() => {
    authService.getTwoFactorStatus()
      .then((status) => setView({ kind: 'status', status }))
      .catch((err) => {
        const fallback = fr ? 'Impossible de charger le statut 2FA' : 'Failed to load 2FA status';
        const message = err instanceof Error && err.message ? err.message : fallback;
        toast.error(message);
      });
  }, [fr]);

  // ── Step-up helper ────────────────────────────────

  /**
   * Opens the step-up dialog, returns a token once the user verifies.
   * Throws if the user cancels or the verification fails repeatedly.
   */
  const requestStepUp = (): Promise<string> => {
    return new Promise<string>((resolve, reject) => {
      setStepUpCode('');
      setStepUpRecoveryCode('');
      setStepUpUseRecovery(false);
      setStepUpError('');
      setStepUpResolver({ resolve, reject });
      setStepUpOpen(true);
    });
  };

  const submitStepUp = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!stepUpResolver) return;
    const submitted = stepUpUseRecovery ? stepUpRecoveryCode.trim().toLowerCase() : stepUpCode;
    if (!submitted) return;

    setStepUpSubmitting(true);
    setStepUpError('');
    try {
      const result = await authService.stepUp('Action', { code: submitted });
      stepUpResolver.resolve(result.token);
      setStepUpOpen(false);
      setStepUpResolver(null);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Invalid code.';
      setStepUpError(message);
      setStepUpCode('');
      setStepUpRecoveryCode('');
    } finally {
      setStepUpSubmitting(false);
    }
  };

  const cancelStepUp = () => {
    if (stepUpResolver) {
      stepUpResolver.reject(new Error('Step-up cancelled.'));
    }
    setStepUpOpen(false);
    setStepUpResolver(null);
  };

  /**
   * Wraps a call that needs an Action step-up: tries it raw, catches the
   * 403 step_up_required, opens the dialog, retries with the token.
   */
  const withStepUp = async <T,>(call: (token: string) => Promise<T>): Promise<T | null> => {
    try {
      // First attempt without a token — will 403 if step-up is required
      // (which it always is for these endpoints, but staying generic).
      return await call('');
    } catch (err) {
      if (!isStepUpRequired(err)) throw err;
      try {
        const token = await requestStepUp();
        return await call(token);
      } catch (inner) {
        // User cancelled or step-up failed — silent
        if (inner instanceof Error && inner.message === 'Step-up cancelled.') return null;
        throw inner;
      }
    }
  };

  // ── Action handlers ───────────────────────────────

  const startSetup = async () => {
    setSubmitting(true);
    try {
      const setup = await authService.setupTwoFactor();
      setView({ kind: 'setupSecret', setup });
    } catch {
      toast.error(fr ? 'Impossible de démarrer la configuration' : 'Failed to start setup');
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
      // CRITICAL: persist the new amr=mfa token so subsequent requests use
      // an up-to-date JWT (also resets the LastLogin server-side ticking
      // window). Without this we'd keep the old amr=pwd token around.
      applyAuth(result.auth);
      setCode('');
      setSavedConfirmed(false);
      setView({ kind: 'recoveryCodes', codes: result.recoveryCodes });
      toast.success(fr ? 'Authentification à deux facteurs activée' : 'Two-factor authentication enabled');
    } catch (err) {
      const message = err instanceof Error ? err.message : (fr ? 'Code invalide' : 'Invalid code');
      toast.error(message);
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
        // Successful disable
        const status = await authService.getTwoFactorStatus();
        setView({ kind: 'status', status });
        toast.success(fr ? 'Authentification à deux facteurs désactivée' : 'Two-factor authentication disabled');
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : (fr ? 'Erreur' : 'Error');
      toast.error(message);
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
        toast.success(fr ? 'Nouveaux codes de secours générés' : 'New recovery codes generated');
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : (fr ? 'Erreur' : 'Error');
      toast.error(message);
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
        'Althea Systems — Recovery Codes\n',
        'Generated: ' + new Date().toISOString() + '\n',
        '\n',
        (fr
          ? 'Chaque code peut être utilisé UNE seule fois si vous perdez accès à votre app d\'authentification.\n'
          : 'Each code can be used ONCE if you lose access to your authenticator.\n'),
        (fr
          ? 'Conservez ce fichier en lieu sûr.\n'
          : 'Keep this file in a secure location.\n'),
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
    <>
      <Card>
        <CardContent className="p-6">
          {/* Status (default) */}
          {view.kind === 'status' && view.status.enabled && (
            <>
              <div className="flex items-start gap-3 mb-4">
                <div className="w-10 h-10 rounded-full bg-success/10 flex items-center justify-center shrink-0">
                  <ShieldCheck className="w-5 h-5 text-success" />
                </div>
                <div>
                  <h3 className="font-semibold text-brand-dark">
                    {fr ? 'Authentification à deux facteurs activée' : 'Two-factor authentication enabled'}
                  </h3>
                  <p className="text-sm text-muted-foreground">
                    {fr
                      ? `Activée le ${view.status.enabledAt ? new Date(view.status.enabledAt).toLocaleDateString('fr-FR') : '—'}.`
                      : `Enabled on ${view.status.enabledAt ? new Date(view.status.enabledAt).toLocaleDateString('en-US') : '—'}.`}
                    {' '}
                    {fr
                      ? `${view.status.recoveryCodesRemaining} code(s) de secours restant(s).`
                      : `${view.status.recoveryCodesRemaining} recovery code(s) remaining.`}
                  </p>
                </div>
              </div>
              <div className="flex flex-wrap gap-2">
                <Button
                  variant="outline"
                  onClick={regenerate}
                  disabled={submitting}
                >
                  <KeyRound className="w-4 h-4 mr-2" />
                  {fr ? 'Régénérer les codes de secours' : 'Regenerate recovery codes'}
                </Button>
                <Button
                  variant="destructive"
                  onClick={disable}
                  disabled={submitting}
                >
                  {fr ? 'Désactiver' : 'Disable'}
                </Button>
              </div>
            </>
          )}

          {view.kind === 'status' && !view.status.enabled && (
            <>
              <div className="flex items-start gap-3 mb-4">
                <div className="w-10 h-10 rounded-full bg-amber-100 flex items-center justify-center shrink-0">
                  <ShieldAlert className="w-5 h-5 text-amber-700" />
                </div>
                <div>
                  <h3 className="font-semibold text-brand-dark">
                    {fr ? 'Authentification à deux facteurs désactivée' : 'Two-factor authentication disabled'}
                  </h3>
                  <p className="text-sm text-muted-foreground">
                    {fr
                      ? 'Ajoutez une couche de sécurité supplémentaire à votre compte.'
                      : 'Add an extra layer of security to your account.'}
                  </p>
                </div>
              </div>
              <Button
                onClick={startSetup}
                disabled={submitting}
                className="bg-brand-primary hover:bg-brand-hover text-white"
              >
                {submitting ? '…' : (fr ? 'Activer la 2FA' : 'Enable 2FA')}
              </Button>
            </>
          )}

          {/* Setup step 1: show secret */}
          {view.kind === 'setupSecret' && (
            <>
              <h3 className="font-semibold text-brand-dark mb-3">
                {fr ? 'Configuration de la 2FA — étape 1 sur 2' : '2FA setup — step 1 of 2'}
              </h3>
              <ol className="text-sm text-gray-700 space-y-1 mb-4 list-decimal list-inside">
                <li>{fr
                  ? 'Ouvrez votre app d\'authentification (Google Authenticator, Authy, 1Password, Bitwarden…).'
                  : 'Open your authenticator app (Google Authenticator, Authy, 1Password, Bitwarden…).'}</li>
                <li>{fr
                  ? 'Scannez le QR code ci-dessous — ou saisissez la clé manuellement.'
                  : 'Scan the QR code below — or type the secret manually.'}</li>
              </ol>

              <div className="flex justify-center mb-4">
                <OtpQrCode uri={view.setup.otpAuthUri} />
              </div>

              <details className="text-sm text-gray-700 mb-4">
                <summary className="cursor-pointer text-brand-primary hover:underline mb-2">
                  {fr ? 'Impossible de scanner ? Afficher la clé à saisir' : 'Can\'t scan? Show the secret to type manually'}
                </summary>
                <div className="bg-gray-50 border rounded-md p-4 mt-2">
                  <p className="text-xs text-muted-foreground mb-2">
                    {fr ? 'Clé secrète (nom de compte : AltheaSystems)' : 'Secret (account name: AltheaSystems)'}
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
                  {fr ? 'Annuler' : 'Cancel'}
                </Button>
                <Button
                  onClick={() => setView({ kind: 'setupVerify', setup: view.setup })}
                  className="bg-brand-primary hover:bg-brand-hover text-white"
                >
                  {fr ? 'Suivant' : 'Next'}
                </Button>
              </div>
            </>
          )}

          {/* Setup step 2: verify */}
          {view.kind === 'setupVerify' && (
            <form onSubmit={verifyAndEnable}>
              <h3 className="font-semibold text-brand-dark mb-3">
                {fr ? 'Configuration de la 2FA — étape 2 sur 2' : '2FA setup — step 2 of 2'}
              </h3>
              <p className="text-sm text-gray-700 mb-4">
                {fr
                  ? 'Saisissez le code à 6 chiffres généré par votre app :'
                  : 'Enter the 6-digit code from your authenticator:'}
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
                  {fr ? 'Retour' : 'Back'}
                </Button>
                <Button
                  type="submit"
                  disabled={submitting || code.length !== 6}
                  className="bg-brand-primary hover:bg-brand-hover text-white"
                >
                  {submitting ? '…' : (fr ? 'Vérifier et activer' : 'Verify and enable')}
                </Button>
              </div>
            </form>
          )}

          {/* Recovery codes (post-enable or post-regenerate) */}
          {view.kind === 'recoveryCodes' && (
            <>
              <h3 className="font-semibold text-brand-dark mb-3">
                {fr ? 'Codes de secours' : 'Recovery codes'}
              </h3>
              <div className="bg-amber-50 border border-amber-200 rounded-md p-3 mb-4 text-sm text-amber-900">
                {fr
                  ? 'Sauvegardez ces codes maintenant — ils permettent l\'accès si vous perdez votre app et ne seront plus affichés.'
                  : 'Save these codes now — they grant access if you lose your authenticator and won\'t be shown again.'}
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
                <Download className="w-4 h-4 mr-2" />
                {fr ? 'Télécharger en .txt' : 'Download as .txt'}
              </Button>
              <label className="flex items-start gap-2 mb-4 text-sm cursor-pointer">
                <input
                  type="checkbox"
                  checked={savedConfirmed}
                  onChange={(e) => setSavedConfirmed(e.target.checked)}
                  className="mt-0.5"
                />
                <span>
                  {fr
                    ? 'J\'ai sauvegardé ces codes en lieu sûr.'
                    : 'I\'ve stored these codes in a safe place.'}
                </span>
              </label>
              <Button
                type="button"
                onClick={finishRecoveryCodes}
                disabled={!savedConfirmed}
                className="w-full bg-brand-primary hover:bg-brand-hover text-white"
              >
                {fr ? 'Terminé' : 'Done'}
              </Button>
            </>
          )}
        </CardContent>
      </Card>

      {/* Step-up dialog (lazy-shown when a sensitive op needs proof) */}
      <Dialog open={stepUpOpen} onOpenChange={(open) => { if (!open) cancelStepUp(); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{fr ? 'Confirmation requise' : 'Confirmation required'}</DialogTitle>
            <DialogDescription>
              {stepUpUseRecovery
                ? (fr ? 'Saisissez un code de secours.' : 'Enter a recovery code.')
                : (fr
                  ? 'Saisissez le code à 6 chiffres de votre app pour confirmer cette action.'
                  : 'Enter the 6-digit code from your authenticator to confirm this action.')}
            </DialogDescription>
          </DialogHeader>
          <form onSubmit={submitStepUp} className="space-y-4">
            {!stepUpUseRecovery ? (
              <div className="flex justify-center">
                <InputOTP
                  maxLength={6}
                  value={stepUpCode}
                  onChange={(v) => { setStepUpCode(v); setStepUpError(''); }}
                  autoFocus
                >
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
                value={stepUpRecoveryCode}
                onChange={(e) => { setStepUpRecoveryCode(e.target.value); setStepUpError(''); }}
                placeholder="xxxx-xxxx-xxxx-xxxx"
                autoFocus
                className="font-mono tracking-wider text-center"
              />
            )}

            {stepUpError && <p className="text-error text-sm text-center">{stepUpError}</p>}

            <div className="flex gap-2">
              <Button type="button" variant="outline" onClick={cancelStepUp} className="flex-1">
                {fr ? 'Annuler' : 'Cancel'}
              </Button>
              <Button
                type="submit"
                disabled={
                  stepUpSubmitting ||
                  (stepUpUseRecovery ? !stepUpRecoveryCode.trim() : stepUpCode.length !== 6)
                }
                className="flex-1 bg-brand-primary hover:bg-brand-hover text-white"
              >
                {stepUpSubmitting ? '…' : (fr ? 'Confirmer' : 'Confirm')}
              </Button>
            </div>

            <button
              type="button"
              onClick={() => {
                setStepUpUseRecovery((v) => !v);
                setStepUpCode('');
                setStepUpRecoveryCode('');
                setStepUpError('');
              }}
              className="w-full text-sm text-brand-primary hover:underline"
            >
              {stepUpUseRecovery
                ? (fr ? 'Utiliser un code de l\'app' : 'Use authenticator code instead')
                : (fr ? 'Utiliser un code de secours' : 'Use a recovery code instead')}
            </button>
          </form>
        </DialogContent>
      </Dialog>
    </>
  );
}
