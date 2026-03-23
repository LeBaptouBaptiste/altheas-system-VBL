'use client';

import { useState, useRef, useEffect } from 'react';
import { Send, Bot, User, LifeBuoy } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { useI18n } from '@/context/i18n-context';
import { productsService } from '@/lib/api-services';
import type { ProductDto } from '@/lib/api-types';
import { toLocalized } from '@/lib/api-types';
import { formatPrice, calculateTTC } from '@/lib/money';
import { StockStatus, VatRate } from '@/lib/enums';
import { toast } from 'sonner';

const VAT_RATE_VALUES: Record<number, number> = {
  [VatRate.Standard]: 0.20, [VatRate.Intermediate]: 0.10, [VatRate.Reduced]: 0.055, [VatRate.Zero]: 0,
};

interface Message { role: 'user' | 'bot'; content: string; }

export default function ChatbotPage() {
  const { t, localized, locale } = useI18n();
  const [messages, setMessages] = useState<Message[]>([
    { role: 'bot', content: t('chatbot.welcome') },
  ]);
  const [input, setInput] = useState('');
  const [ticketCreated, setTicketCreated] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    scrollRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const generateResponse = async (userInput: string): Promise<string> => {
    const q = userInput.toLowerCase();
    const fmt = (n: number) => formatPrice(n, locale === 'fr' ? 'fr-FR' : 'en-US');

    // Try product search via API
    try {
      const res = await productsService.search(userInput, 1, 3);
      if (res.data.length > 0) {
        const p = res.data[0];
        const name = localized(toLocalized(p.nameFr, p.nameEn));
        const vatRate = VAT_RATE_VALUES[p.vatRate] ?? 0.20;
        const price = calculateTTC(p.priceHT, vatRate);
        const stock = p.stockStatus === StockStatus.InStock ? (locale === 'fr' ? 'en stock' : 'in stock') : p.stockStatus === StockStatus.LowStock ? (locale === 'fr' ? 'stock faible' : 'low stock') : (locale === 'fr' ? 'rupture' : 'out of stock');
        const desc = localized(toLocalized(p.descriptionFr, p.descriptionEn));
        return locale === 'fr'
          ? `Le ${name} est proposé à ${fmt(price)} TTC. Statut : ${stock}. ${desc}`
          : `The ${name} is available at ${fmt(price)} incl. VAT. Status: ${stock}. ${desc}`;
      }
    } catch { /* fallback to static responses */ }

    // Common questions
    if (q.includes('prix') || q.includes('price') || q.includes('tarif')) {
      return locale === 'fr'
        ? 'Nos prix sont affichés TTC sur chaque fiche produit. Pour un devis personnalisé, contactez notre équipe commerciale.'
        : 'Our prices are displayed including VAT on each product page. For a custom quote, contact our sales team.';
    }
    if (q.includes('livraison') || q.includes('delivery') || q.includes('shipping')) {
      return locale === 'fr'
        ? 'Nous proposons 3 modes de livraison : Standard (5-7 jours, 15€), Express (2-3 jours, 35€), 24h (75€).'
        : 'We offer 3 shipping methods: Standard (5-7 days, €15), Express (2-3 days, €35), Overnight (€75).';
    }
    if (q.includes('retour') || q.includes('return') || q.includes('sav')) {
      return locale === 'fr'
        ? 'Pour tout retour ou SAV, contactez notre service client. Je peux créer un ticket si vous le souhaitez.'
        : 'For any returns or after-sales service, contact our customer service. I can create a ticket if you wish.';
    }
    if (q.includes('bonjour') || q.includes('hello') || q.includes('hi') || q.includes('salut')) {
      return locale === 'fr' ? 'Bonjour ! Comment puis-je vous aider aujourd\'hui ?' : 'Hello! How can I help you today?';
    }

    return locale === 'fr'
      ? 'Je ne suis pas sûr de pouvoir répondre à cette question. Souhaitez-vous contacter notre support technique ?'
      : 'I\'m not sure I can answer this question. Would you like to contact our technical support?';
  };

  const handleSend = async () => {
    if (!input.trim()) return;
    const userMsg = input.trim();
    setInput('');
    setMessages(prev => [...prev, { role: 'user', content: userMsg }]);

    const response = await generateResponse(userMsg);
    setMessages(prev => [...prev, { role: 'bot', content: response }]);
  };

  const handleEscalate = async () => {
    try {
      const { messagesService } = await import('@/lib/api-services');
      // Create a conversation first, then escalate by creating a ticket
      const lastUserMsg = messages.filter(m => m.role === 'user').pop()?.content || 'Support request';
      await messagesService.create({
        email: 'chatbot@altheasystems.com',
        subject: locale === 'fr' ? 'Escalade chatbot' : 'Chatbot escalation',
        message: lastUserMsg,
      });
      setTicketCreated(true);
      setMessages(prev => [...prev, { role: 'bot', content: t('chatbot.ticket_created') }]);
      toast.success(t('chatbot.ticket_created'));
    } catch {
      toast.error(locale === 'fr' ? 'Erreur lors de la création du ticket' : 'Failed to create ticket');
    }
  };

  return (
    <div className="container mx-auto px-4 py-8 max-w-2xl">
      <div className="flex items-center gap-3 mb-6">
        <div className="w-10 h-10 rounded-full bg-brand-primary flex items-center justify-center">
          <Bot className="w-5 h-5 text-white" />
        </div>
        <div>
          <h1 className="text-xl text-brand-dark">{t('chatbot.title')}</h1>
          <Badge variant="outline" className="text-success border-success text-xs">Online</Badge>
        </div>
      </div>

      <Card className="h-[500px] flex flex-col">
        <div className="flex-1 overflow-y-auto p-4 space-y-4">
          {messages.map((msg, i) => (
            <div key={i} className={`flex gap-3 ${msg.role === 'user' ? 'flex-row-reverse' : ''}`}>
              <div className={`w-8 h-8 rounded-full flex items-center justify-center shrink-0 ${msg.role === 'bot' ? 'bg-brand-light' : 'bg-gray-200'}`}>
                {msg.role === 'bot' ? <Bot className="w-4 h-4 text-brand-primary" /> : <User className="w-4 h-4 text-gray-600" />}
              </div>
              <div className={`max-w-[80%] rounded-lg px-4 py-2 text-sm ${msg.role === 'bot' ? 'bg-gray-100 text-foreground' : 'bg-brand-primary text-white'}`}>
                {msg.content}
              </div>
            </div>
          ))}
          <div ref={scrollRef} />
        </div>

        {!ticketCreated && messages.length > 2 && (
          <div className="px-4 pb-2">
            <Button variant="outline" size="sm" className="text-warning border-warning hover:bg-warning/10" onClick={handleEscalate}>
              <LifeBuoy className="w-4 h-4 mr-2" />{t('chatbot.escalate')}
            </Button>
          </div>
        )}

        <div className="p-4 border-t">
          <form onSubmit={(e) => { e.preventDefault(); handleSend(); }} className="flex gap-2">
            <Input value={input} onChange={(e) => setInput(e.target.value)} placeholder={t('chatbot.placeholder')} className="flex-1" aria-label={t('chatbot.placeholder')} />
            <Button type="submit" className="bg-brand-primary hover:bg-brand-hover text-white" disabled={!input.trim()}>
              <Send className="w-4 h-4" />
            </Button>
          </form>
        </div>
      </Card>
    </div>
  );
}
