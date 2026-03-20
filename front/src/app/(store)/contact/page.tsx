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

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSent(true);
    toast.success(t('contact.success'));
  };

  if (sent) {
    return (
      <div className="container mx-auto px-4 py-16 max-w-md text-center">
        <Card><CardContent className="p-6 space-y-4">
          <div className="w-16 h-16 rounded-full bg-success/10 flex items-center justify-center mx-auto"><Check className="w-8 h-8 text-success" /></div>
          <h1 className="text-2xl text-brand-dark">{locale === 'fr' ? 'Message envoyé !' : 'Message Sent!'}</h1>
          <p className="text-muted-foreground">{t('contact.success')}</p>
          <Button onClick={() => setSent(false)} variant="outline">{locale === 'fr' ? 'Envoyer un autre message' : 'Send another message'}</Button>
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
          <div><Label htmlFor="subject">{t('contact.subject')}</Label><Input id="subject" value={subject} onChange={e => setSubject(e.target.value)} required placeholder={locale === 'fr' ? 'Saisissez votre sujet' : 'Enter your subject'} /></div>
          <div><Label htmlFor="message">{t('contact.message')}</Label><Textarea id="message" value={message} onChange={e => setMessage(e.target.value)} required rows={6} /></div>
          <Button type="submit" className="w-full bg-brand-primary hover:bg-brand-hover text-white"><Send className="w-4 h-4 mr-2" />{t('contact.send')}</Button>
        </form>
      </CardContent></Card>
      <div className="mt-8 text-center">
        <p className="text-muted-foreground mb-3">{locale === 'fr' ? 'Ou discutez avec notre assistant virtuel' : 'Or chat with our virtual assistant'}</p>
        <Link href="/chatbot"><Button variant="outline" size="lg"><Bot className="w-5 h-5 mr-2" />{t('contact.chatbot_cta')}</Button></Link>
      </div>
    </div>
  );
}
