'use client';

import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import type { User } from '@/mock/types';
import { users } from '@/mock/users';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  login: (email: string, password: string) => { success: boolean; error?: string };
  register: (name: string, email: string, password: string) => { success: boolean; error?: string };
  logout: () => void;
  confirmEmail: () => void;
  twoFactorVerified: boolean;
  verifyTwoFactor: (code: string) => boolean;
  updateUser: (updates: Partial<User>) => void;
  anonymizeAccount: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => {
    if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('althea-user');
      if (saved) {
        try { return JSON.parse(saved); } catch { /* ignore */ }
      }
    }
    return null;
  });
  const [twoFactorVerified, setTwoFactorVerified] = useState(false);

  const persistUser = (u: User | null) => {
    setUser(u);
    if (typeof window !== 'undefined') {
      if (u) localStorage.setItem('althea-user', JSON.stringify(u));
      else localStorage.removeItem('althea-user');
    }
  };

  const login = useCallback((email: string, password: string) => {
    const found = users.find(u => u.email === email && u.password === password);
    if (!found) return { success: false, error: 'auth.email_not_confirmed' };
    if (!found.emailConfirmed) return { success: false, error: 'auth.email_not_confirmed' };
    if (found.anonymized) return { success: false, error: 'common.error' };
    persistUser(found);
    setTwoFactorVerified(false);
    return { success: true };
  }, []);

  const register = useCallback((name: string, email: string, _password: string) => {
    const exists = users.find(u => u.email === email);
    if (exists) return { success: false, error: 'Email already exists' };
    const newUser: User = {
      id: `user-${Date.now()}`,
      name, email,
      status: 'pending',
      anonymized: false,
      emailConfirmed: false,
      role: 'customer',
      addresses: [],
      paymentMethods: [],
      lastLogin: new Date().toISOString(),
      createdAt: new Date().toISOString().split('T')[0],
    };
    persistUser(newUser);
    return { success: true };
  }, []);

  const logout = useCallback(() => {
    persistUser(null);
    setTwoFactorVerified(false);
  }, []);

  const confirmEmail = useCallback(() => {
    if (user) {
      const updated = { ...user, emailConfirmed: true, status: 'active' as const };
      persistUser(updated);
    }
  }, [user]);

  const verifyTwoFactor = useCallback((code: string) => {
    if (code === '123456') {
      setTwoFactorVerified(true);
      return true;
    }
    return false;
  }, []);

  const updateUser = useCallback((updates: Partial<User>) => {
    if (user) {
      const updated = { ...user, ...updates };
      persistUser(updated);
    }
  }, [user]);

  const anonymizeAccount = useCallback(() => {
    if (user) {
      const anonymized: User = {
        ...user,
        name: 'Anonymisé',
        email: `anon-${user.id}@deleted.local`,
        anonymized: true,
        status: 'inactive',
        addresses: [],
        paymentMethods: [],
      };
      persistUser(anonymized);
    }
  }, [user]);

  return (
    <AuthContext.Provider value={{
      user,
      isAuthenticated: !!user && user.emailConfirmed,
      isAdmin: user?.role === 'admin',
      login, register, logout, confirmEmail,
      twoFactorVerified, verifyTwoFactor,
      updateUser, anonymizeAccount,
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
