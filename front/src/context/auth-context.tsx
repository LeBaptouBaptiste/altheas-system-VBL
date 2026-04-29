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
  | { kind: 'twoFactorRequired'; challengeToken: string }
  | { kind: 'mustSetupTwoFactor'; setupToken: string }
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
  register: (name: string, email: string, password: string) => Promise<{ success: boolean; error?: string }>;
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
          return { kind: 'twoFactorRequired', challengeToken: response.challengeToken };

        case 'TwoFactorSetupRequired':
          if (!response.setupToken) {
            return { kind: 'error', error: 'Malformed login response.' };
          }
          return { kind: 'mustSetupTwoFactor', setupToken: response.setupToken };
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
      const response = await authService.register(name, email, password, password);
      applyAuth(response);
      return { success: true };
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'common.error';
      return { success: false, error: message };
    }
  }, [applyAuth]);

  const logout = useCallback(() => {
    clearToken();
    setUser(null);
    localStorage.removeItem('althea-user');
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
      register, logout, confirmEmail,
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
