'use client';

import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import { setToken, clearToken } from '@/lib/api';
import { authService, usersService } from '@/lib/api-services';
import type { UserDto } from '@/lib/api-types';

interface AuthContextType {
  user: UserDto | null;
  loading: boolean;
  isAuthenticated: boolean;
  isAdmin: boolean;
  login: (email: string, password: string) => Promise<{ success: boolean; error?: string }>;
  register: (name: string, email: string, password: string) => Promise<{ success: boolean; error?: string }>;
  logout: () => void;
  confirmEmail: (token: string) => Promise<void>;
  twoFactorVerified: boolean;
  verifyTwoFactor: (code: string) => Promise<boolean>;
  updateUser: (updates: { name?: string; email?: string }) => Promise<void>;
  anonymizeAccount: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [twoFactorVerified, setTwoFactorVerified] = useState(false);

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

  const login = useCallback(async (email: string, password: string) => {
    try {
      const response = await authService.login(email, password);
      setToken(response.accessToken);
      setUser(response.user);
      setTwoFactorVerified(false);
      // Clean up legacy key
      localStorage.removeItem('althea-user');
      return { success: true };
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'common.error';
      return { success: false, error: message };
    }
  }, []);

  const register = useCallback(async (name: string, email: string, password: string) => {
    try {
      const response = await authService.register(name, email, password, password);
      setToken(response.accessToken);
      setUser(response.user);
      localStorage.removeItem('althea-user');
      return { success: true };
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'common.error';
      return { success: false, error: message };
    }
  }, []);

  const logout = useCallback(() => {
    clearToken();
    setUser(null);
    setTwoFactorVerified(false);
    localStorage.removeItem('althea-user');
  }, []);

  const confirmEmail = useCallback(async (token: string) => {
    await authService.confirmEmail(token);
    // Refresh user to get updated emailConfirmed status
    const updated = await authService.getMe();
    setUser(updated);
  }, []);

  const verifyTwoFactor = useCallback(async (code: string) => {
    try {
      await authService.verify2fa(code);
      setTwoFactorVerified(true);
      return true;
    } catch {
      return false;
    }
  }, []);

  const updateUser = useCallback(async (updates: { name?: string; email?: string }) => {
    if (!user) return;
    const updated = await usersService.update(user.id, updates);
    setUser(updated);
  }, [user]);

  const anonymizeAccount = useCallback(async () => {
    if (!user) return;
    await usersService.anonymize(user.id);
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
      isAdmin: user?.role === 'Admin',
      login, register, logout, confirmEmail,
      twoFactorVerified, verifyTwoFactor,
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
