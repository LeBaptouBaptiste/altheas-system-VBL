'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { useI18n } from '@/context/i18n-context';
import { useAuth } from '@/context/auth-context';
import { toast } from 'sonner';

export default function LoginPage() {
  const { t } = useI18n();
  const { login } = useAuth();
  const router = useRouter();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    const result = await login(email, password);
    if (result.success) {
      toast.success(t('auth.login') + ' ✓');
      router.push('/');
    } else {
      setError(result.error ? t(result.error) : t('common.error'));
    }
  };

  return (
    <div className="container mx-auto px-4 py-16 max-w-md">
      <Card>
        <CardContent className="p-6">
          <h1 className="text-2xl text-brand-dark mb-6 text-center">{t('auth.login')}</h1>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div><Label htmlFor="email">{t('auth.email')}</Label><Input id="email" type="email" value={email} onChange={e => setEmail(e.target.value)} required /></div>
            <div><Label htmlFor="password">{t('auth.password')}</Label><Input id="password" type="password" value={password} onChange={e => setPassword(e.target.value)} required /></div>
            {error && <p className="text-sm text-error" role="alert">{error}</p>}
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2"><Checkbox id="remember" /><Label htmlFor="remember" className="text-sm font-normal">{t('auth.remember_me')}</Label></div>
              <Link href="/forgot-password" className="text-sm text-brand-primary hover:underline">{t('auth.forgot_password')}</Link>
            </div>
            <Button type="submit" className="w-full bg-brand-primary hover:bg-brand-hover text-white">{t('auth.login')}</Button>
          </form>
          <p className="text-sm text-center mt-4 text-muted-foreground">{t('auth.no_account')} <Link href="/register" className="text-brand-primary hover:underline">{t('auth.register')}</Link></p>
        </CardContent>
      </Card>
    </div>
  );
}
