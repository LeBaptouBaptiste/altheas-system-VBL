'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Mail, ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { useI18n } from '@/context/i18n-context';

export default function ForgotPasswordPage() {
  const { t } = useI18n();
  const [sent, setSent] = useState(false);
  const [email, setEmail] = useState('');

  if (sent) {
    return (
      <div className="container mx-auto px-4 py-16 max-w-md">
        <Card><CardContent className="p-6 text-center space-y-4">
          <Mail className="w-12 h-12 mx-auto text-brand-primary" />
          <h1 className="text-2xl text-brand-dark">{t('auth.reset_password')}</h1>
          <p className="text-muted-foreground">{t('auth.reset_sent')}</p>
          <Link href="/login"><Button variant="outline"><ArrowLeft className="w-4 h-4 mr-2" />{t('auth.login')}</Button></Link>
        </CardContent></Card>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-16 max-w-md">
      <Card><CardContent className="p-6">
        <h1 className="text-2xl text-brand-dark mb-6 text-center">{t('auth.reset_password')}</h1>
        <form onSubmit={(e) => { e.preventDefault(); setSent(true); }} className="space-y-4">
          <div><Label htmlFor="email">{t('auth.email')}</Label><Input id="email" type="email" value={email} onChange={e => setEmail(e.target.value)} required /></div>
          <Button type="submit" className="w-full bg-brand-primary hover:bg-brand-hover text-white">{t('auth.reset_password')}</Button>
        </form>
        <Link href="/login" className="block text-sm text-center mt-4 text-brand-primary hover:underline"><ArrowLeft className="w-3 h-3 inline mr-1" />{t('auth.login')}</Link>
      </CardContent></Card>
    </div>
  );
}
