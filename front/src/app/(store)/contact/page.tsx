'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Send, Check, Bot } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent } from '@/components/ui/card';
import { useI18n } from '@/context/i18n-context';
import { toast } from 'sonner';

export default function ContactPage() {
  const { t, locale } = useI18n();
  const [sent, setSent] = useState(false);
  const [email, setEmail] = useState('');
  const [subject, setSubject] = useState('');
  const [message, setMessage] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const { messagesService } = await import('@/lib/api-services');
      await messagesService.create({ email, subject, message });
      setSent(true);
      toast.success(t('contact.success'));
    } catch {
      toast.error(t('common.error'));
    }
  };

  if (sent) {
    return (
      <div className="container mx-auto px-4 py-16 max-w-md text-center">
        <Card><CardContent className="p-6 space-y-4">
          <div className="w-16 h-16 rounded-full bg-success/10 flex items-center justify-center mx-auto"><Check className="w-8 h-8 text-success" /></div>
          <h1 className="text-2xl text-brand-dark">{t('contact.sent_title')}</h1>
          <p className="text-muted-foreground">{t('contact.success')}</p>
          <Button onClick={() => setSent(false)} variant="outline">{t('contact.send_another')}</Button>
        </CardContent></Card>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-2xl">
      <h1 className="text-3xl text-brand-dark mb-6">{t('contact.title')}</h1>
      <Card><CardContent className="p-6">
        <form onSubmit={handleSubmit} className="space-y-4">
          <div><Label htmlFor="email">{t('contact.email')}</Label><Input id="email" type="email" value={email} onChange={e => setEmail(e.target.value)} required /></div>
          <div><Label htmlFor="subject">{t('contact.subject')}</Label><Input id="subject" value={subject} onChange={e => setSubject(e.target.value)} required placeholder={t('contact.subject_placeholder')} /></div>
          <div><Label htmlFor="message">{t('contact.message')}</Label><Textarea id="message" value={message} onChange={e => setMessage(e.target.value)} required rows={6} /></div>
          <Button type="submit" className="w-full bg-brand-primary hover:bg-brand-hover text-white"><Send className="w-4 h-4 me-2" />{t('contact.send')}</Button>
        </form>
      </CardContent></Card>
    </div>
  );
}
