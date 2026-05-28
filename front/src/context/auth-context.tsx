'use client';

import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import { setToken, clearToken } from '@/lib/api';
import { authService, usersService } from '@/lib/api-services';
import type { AuthResponse, UserDto } from '@/lib/api-types';

/**
 * Discriminated result of a login attempt. The store login page branches
 * on `kind`:
 *   - 'success'              -> tokens are stored, user is authenticated
 *   - 'twoFactorRequired'    -> show the 6-digit code form, then call
 *                               completeTwoFactorChallenge()
 *   - 'mustSetupTwoFactor'   -> redirect to the admin setup wizard with
 *                               `setupToken` as the bearer
 *   - 'error'                -> display `error`
 */
export type LoginResult =
  | { kind: 'success' }
  | {
      kind: 'twoFactorRequired';
      challengeToken: string;
      /**
       * Phase 4b: 1 = Authenticator, 2 = Email, null = unknown (legacy
       * server, treat as Authenticator). Drives the challenge-screen copy.
       */
      method: number | null;
    }
  | { kind: 'mustSetupTwoFactor'; setupToken: string }
  /**
   * Phase 2: the user authenticated but hasn't clicked the confirmation
   * link mailed at registration. Front shows a "check your inbox" screen
   * with a "Resend" CTA. We echo the email so the resend call can target it.
   */
  | { kind: 'emailConfirmationRequired'; email: string }
  /** `error` is the raw caught value — pass it to `getErrorMessage()` for a localized string. */
  | { kind: 'error'; error: unknown };

interface AuthContextType {
  user: UserDto | null;
  loading: boolean;
  isAuthenticated: boolean;
  isAdmin: boolean;
  login: (email: string, password: string) => Promise<LoginResult>;
  /** Called by the login page after a `twoFactorRequired` outcome. */
  completeTwoFactorChallenge: (challengeToken: string, code: string) => Promise<LoginResult>;
  /** Used by /2fa/enable callers (admin forced setup or voluntary opt-in) to
   *  finalize the session once the API returns a fresh AuthResponse. */
  applyAuth: (auth: AuthResponse) => void;
  /**
   * Phase 2: registration no longer auto-logs in. Returns success when the
   * account is created (the user must check their inbox), or an error if
   * the email is taken / validation failed.
   */
  register: (name: string, email: string, password: string) => Promise<{ success: boolean; error?: string }>;
  /** Re-issue the confirmation link. Always succeeds from the caller's POV. */
  resendConfirmation: (email: string) => Promise<void>;
  logout: () => void;
  confirmEmail: (token: string) => Promise<void>;
  updateUser: (updates: { name?: string; email?: string }) => Promise<void>;
  /**
   * Anonymises the current user (GDPR delete equivalent). Backend requires
   * a step-up Action token — caller obtains one via `useStepUp().withStepUp()`
   * and passes it here. After success, the local session is wiped.
   */
  anonymizeAccount: (stepUpToken: string) => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null);
  const [loading, setLoading] = useState(true);

  // On mount: check if token exists and validate it
  useEffect(() => {
    const token = typeof window !== 'undefined' ? localStorage.getItem('althea-token') : null;
    if (!token) {
      setLoading(false);
      return;
    }

    authService.getMe()
      .then((u) => setUser(u))
      .catch(() => {
        clearToken();
        // Also clean up legacy localStorage key
        localStorage.removeItem('althea-user');
      })
      .finally(() => setLoading(false));
  }, []);

  const applyAuth = useCallback((auth: AuthResponse) => {
    setToken(auth.accessToken);
    setUser(auth.user);
    localStorage.removeItem('althea-user');
  }, []);

  const login = useCallback(async (email: string, password: string): Promise<LoginResult> => {
    try {
      const response = await authService.login(email, password);

      switch (response.outcome) {
        case 'Authenticated':
          if (!response.auth) {
            return { kind: 'error', error: 'Malformed login response.' };
          }
          applyAuth(response.auth);
          return { kind: 'success' };

        case 'TwoFactorRequired':
          if (!response.challengeToken) {
            return { kind: 'error', error: 'Malformed login response.' };
          }
          return {
            kind: 'twoFactorRequired',
            challengeToken: response.challengeToken,
            method: response.twoFactorMethod,
          };

        case 'TwoFactorSetupRequired':
          if (!response.setupToken) {
            return { kind: 'error', error: 'Malformed login response.' };
          }
          return { kind: 'mustSetupTwoFactor', setupToken: response.setupToken };

        case 'EmailConfirmationRequired':
          // The email the user typed IS the one to resend to — no need to
          // bounce through the API. Caller will show the "Check your inbox"
          // screen with a resend CTA.
          return { kind: 'emailConfirmationRequired', email };
      }
    } catch (err: unknown) {
      return { kind: 'error', error: err };
    }
  }, [applyAuth]);

  const completeTwoFactorChallenge = useCallback(
    async (challengeToken: string, code: string): Promise<LoginResult> => {
      try {
        const auth = await authService.verifyTwoFactorChallenge(challengeToken, code);
        applyAuth(auth);
        return { kind: 'success' };
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'common.error';
        return { kind: 'error', error: message };
      }
    },
    [applyAuth],
  );

  const register = useCallback(async (name: string, email: string, password: string) => {
    try {
      // Phase 2: register no longer returns tokens — the user must confirm
      // via email link first. Just await the call to surface validation
      // errors and let the caller advance to the "check your inbox" step.
      await authService.register(name, email, password, password);
      return { success: true };
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'common.error';
      return { success: false, error: message };
    }
  }, []);

  const resendConfirmation = useCallback(async (email: string) => {
    // 200 regardless of whether the email matches an account (anti-enum) so
    // we don't surface errors from the API.
    try { await authService.resendConfirmation(email); } catch { /* swallow */ }
  }, []);

  const logout = useCallback(() => {
    clearToken();
    setUser(null);
    localStorage.removeItem('althea-user');
    if (typeof window !== 'undefined') {
      window.dispatchEvent(new Event('althea:logout'));
    }
  }, []);

  const confirmEmail = useCallback(async (token: string) => {
    await authService.confirmEmail(token);
    // Refresh user to get updated emailConfirmed status
    const updated = await authService.getMe();
    setUser(updated);
  }, []);

  const updateUser = useCallback(async (updates: { name?: string; email?: string }) => {
    if (!user) return;
    const updated = await usersService.update(user.id, updates);
    setUser(updated);
  }, [user]);

  const anonymizeAccount = useCallback(async (stepUpToken: string) => {
    if (!user) return;
    await usersService.anonymize(user.id, stepUpToken);
    clearToken();
    setUser(null);
    localStorage.removeItem('althea-user');
    if (typeof window !== 'undefined') {
      window.dispatchEvent(new Event('althea:logout'));
    }
  }, [user]);

  const refreshUser = useCallback(async () => {
    try {
      const updated = await authService.getMe();
      setUser(updated);
    } catch {
      clearToken();
      setUser(null);
    }
  }, []);

  return (
    <AuthContext.Provider value={{
      user,
      loading,
      isAuthenticated: !!user && user.emailConfirmed,
      isAdmin: user?.role === 1,
      login, completeTwoFactorChallenge, applyAuth,
      register, resendConfirmation, logout, confirmEmail,
      updateUser, anonymizeAccount, refreshUser,
    }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
