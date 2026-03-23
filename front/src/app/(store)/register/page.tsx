'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Mail } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { useI18n } from '@/context/i18n-context';
import { useAuth } from '@/context/auth-context';
import { toast } from 'sonner';

export default function RegisterPage() {
  const { t } = useI18n();
  const { register, confirmEmail } = useAuth();
  const router = useRouter();
  const [step, setStep] = useState<'form' | 'confirm'>('form');
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPw, setConfirmPw] = useState('');
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    if (password !== confirmPw) { setError('Passwords do not match'); return; }
    if (password.length < 8) { setError(t('auth.password_rules')); return; }
    const result = await register(name, email, password);
    if (result.success) {
      setStep('confirm');
    } else {
      setError(result.error || t('common.error'));
    }
  };

  const handleConfirm = async () => {
    await confirmEmail(email);
    toast.success(t('auth.login') + ' ✓');
    router.push('/');
  };

  if (step === 'confirm') {
    return (
      <div className="container mx-auto px-4 py-16 max-w-md">
        <Card><CardContent className="p-6 text-center space-y-4">
          <Mail className="w-12 h-12 mx-auto text-brand-primary" />
          <h1 className="text-2xl text-brand-dark">{t('auth.confirm_email_title')}</h1>
          <p className="text-muted-foreground">{t('auth.confirm_email_text')}</p>
          <Button onClick={handleConfirm} className="bg-brand-primary hover:bg-brand-hover text-white">{t('auth.i_confirmed')}</Button>
        </CardContent></Card>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-16 max-w-md">
      <Card><CardContent className="p-6">
        <h1 className="text-2xl text-brand-dark mb-6 text-center">{t('auth.register')}</h1>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div><Label htmlFor="name">{t('auth.full_name')}</Label><Input id="name" value={name} onChange={e => setName(e.target.value)} required /></div>
          <div><Label htmlFor="email">{t('auth.email')}</Label><Input id="email" type="email" value={email} onChange={e => setEmail(e.target.value)} required /></div>
          <div><Label htmlFor="password">{t('auth.password')}</Label><Input id="password" type="password" value={password} onChange={e => setPassword(e.target.value)} required /><p className="text-xs text-muted-foreground mt-1">{t('auth.password_rules')}</p></div>
          <div><Label htmlFor="confirmPw">{t('auth.confirm_password')}</Label><Input id="confirmPw" type="password" value={confirmPw} onChange={e => setConfirmPw(e.target.value)} required /></div>
          {error && <p className="text-sm text-error" role="alert">{error}</p>}
          <Button type="submit" className="w-full bg-brand-primary hover:bg-brand-hover text-white">{t('auth.register')}</Button>
        </form>
        <p className="text-sm text-center mt-4 text-muted-foreground">{t('auth.has_account')} <Link href="/login" className="text-brand-primary hover:underline">{t('auth.login')}</Link></p>
      </CardContent></Card>
    </div>
  );
}
