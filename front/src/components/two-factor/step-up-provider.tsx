'use client';

import { createContext, useCallback, useContext, useState, type ReactNode } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { InputOTP, InputOTPGroup, InputOTPSlot } from '@/components/ui/input-otp';
import { useI18n } from '@/context/i18n-context';
import { authService } from '@/lib/api-services';
import { isStepUpRequired } from '@/lib/api';
import { getErrorMessage } from '@/lib/api-errors';

/**
 * Promise-based step-up flow exposed app-wide. A single `<StepUpDialog>` is
 * mounted by the provider; any descendant can call `requestStepUp()` to
 * obtain a fresh action step-up token, or `withStepUp(call)` to wrap a
 * sensitive API call (try raw → on 403 step_up_required, prompt → retry
 * with token).
 */
interface StepUpContextValue {
  /** Opens the dialog and resolves with the step-up token once the user verifies. Rejects on cancel. */
  requestStepUp: () => Promise<string>;
  /**
   * Wraps a sensitive call. Returns `null` if the user cancels the dialog
   * (so callers can distinguish "cancelled" from "failed").
   */
  withStepUp: <T>(call: (token: string) => Promise<T>) => Promise<T | null>;
}

const StepUpContext = createContext<StepUpContextValue | null>(null);

type Resolver = {
  resolve: (token: string) => void;
  reject: (reason: Error) => void;
};

export function StepUpProvider({ children }: { children: ReactNode }) {
  const { t } = useI18n();

  const [open, setOpen] = useState(false);
  const [resolver, setResolver] = useState<Resolver | null>(null);
  const [code, setCode] = useState('');
  const [useRecovery, setUseRecovery] = useState(false);
  const [recoveryCode, setRecoveryCode] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const resetForm = () => {
    setCode('');
    setUseRecovery(false);
    setRecoveryCode('');
    setError('');
  };

  const requestStepUp = useCallback((): Promise<string> => {
    return new Promise<string>((resolve, reject) => {
      resetForm();
      setResolver({ resolve, reject });
      setOpen(true);
    });
  }, []);

  const withStepUp = useCallback(
    async <T,>(call: (token: string) => Promise<T>): Promise<T | null> => {
      try {
        return await call('');
      } catch (err) {
        if (!isStepUpRequired(err)) throw err;
        try {
          const token = await requestStepUp();
          return await call(token);
        } catch (inner) {
          if (inner instanceof Error && inner.message === 'Step-up cancelled.') return null;
          throw inner;
        }
      }
    },
    [requestStepUp],
  );

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resolver) return;
    const submitted = useRecovery ? recoveryCode.trim().toLowerCase() : code;
    if (!submitted) return;

    setSubmitting(true);
    setError('');
    try {
      const result = await authService.stepUp('Action', { code: submitted });
      resolver.resolve(result.token);
      setOpen(false);
      setResolver(null);
    } catch (err) {
      setError(getErrorMessage(err, t));
      setCode('');
      setRecoveryCode('');
    } finally {
      setSubmitting(false);
    }
  };

  const cancel = useCallback(() => {
    if (resolver) {
      resolver.reject(new Error('Step-up cancelled.'));
    }
    setOpen(false);
    setResolver(null);
  }, [resolver]);

  return (
    <StepUpContext.Provider value={{ requestStepUp, withStepUp }}>
      {children}

      <Dialog open={open} onOpenChange={(o) => { if (!o) cancel(); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('2fa.stepup_title')}</DialogTitle>
            <DialogDescription>
              {useRecovery
                ? t('2fa.stepup_description_recovery')
                : t('2fa.stepup_description_authenticator')}
            </DialogDescription>
          </DialogHeader>
          <form onSubmit={submit} className="space-y-4">
            {!useRecovery ? (
              <div className="flex justify-center">
                <InputOTP
                  maxLength={6}
                  value={code}
                  onChange={(v) => { setCode(v); setError(''); }}
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
                value={recoveryCode}
                onChange={(e) => { setRecoveryCode(e.target.value); setError(''); }}
                placeholder={t('2fa.stepup_recovery_placeholder')}
                autoFocus
                className="font-mono tracking-wider text-center"
              />
            )}

            {error && <p className="text-error text-sm text-center">{error}</p>}

            <div className="flex gap-2">
              <Button type="button" variant="outline" onClick={cancel} className="flex-1">
                {t('common.cancel')}
              </Button>
              <Button
                type="submit"
                disabled={
                  submitting || (useRecovery ? !recoveryCode.trim() : code.length !== 6)
                }
                className="flex-1 bg-brand-primary hover:bg-brand-hover text-white"
              >
                {submitting ? '…' : t('2fa.stepup_confirm')}
              </Button>
            </div>

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
              {useRecovery
                ? t('2fa.stepup_use_authenticator')
                : t('2fa.stepup_use_recovery')}
            </button>
          </form>
        </DialogContent>
      </Dialog>
    </StepUpContext.Provider>
  );
}

export function useStepUp(): StepUpContextValue {
  const ctx = useContext(StepUpContext);
  if (!ctx) throw new Error('useStepUp must be used within StepUpProvider');
  return ctx;
}
