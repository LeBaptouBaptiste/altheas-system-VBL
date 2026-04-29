'use client';

import { useState, useRef, useEffect } from 'react';
import { Send, Bot, User, X, LifeBuoy, MessageCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { useI18n } from '@/context/i18n-context';
import { botString } from '@/lib/chatbot-strings';
import { toast } from 'sonner';

interface Message { role: 'user' | 'bot'; content: string; }

// NOTE: product-name matching was removed when the front merged into the
// API-backed branch — the mocks (and their `Record<locale, string>` shape)
// are gone. Re-introducing it would require an API search call here, which
// is out of scope. The bot still answers price/shipping/returns/hours/etc.
function generateBotResponse(input: string, locale: string): string {
  const q = input.toLowerCase();

  // Common intents
  if (q.includes('prix') || q.includes('price') || q.includes('tarif') || q.includes('harga') || q.includes('سعر')) return botString('price', locale);
  if (q.includes('livraison') || q.includes('delivery') || q.includes('shipping') || q.includes('penghantaran') || q.includes('شحن')) return botString('shipping', locale);
  if (q.includes('retour') || q.includes('return') || q.includes('sav') || q.includes('pemulangan') || q.includes('إرجاع')) return botString('returns', locale);
  if (q.includes('horaire') || q.includes('hour') || q.includes('contact') || q.includes('waktu') || q.includes('ساعة')) return botString('hours', locale);
  if (q.includes('bonjour') || q.includes('hello') || q.includes('hi') || q.includes('salut') || q.includes('helo') || q.includes('مرحبا') || q.includes('مرحباً')) return botString('greeting', locale);

  return botString('fallback', locale);
}

export function ChatbotBubble() {
  const { t, locale, dir } = useI18n();
  const isRtl = dir === 'rtl';
  const [open, setOpen] = useState(false);
  const [started, setStarted] = useState(false);
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [ticketCreated, setTicketCreated] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (open && scrollRef.current) {
      scrollRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [messages, open]);

  const startChat = () => {
    setStarted(true);
    setMessages([{ role: 'bot', content: t('chatbot.welcome') }]);
  };

  const handleSend = () => {
    if (!input.trim()) return;
    const userMsg = input.trim();
    setInput('');
    setMessages(prev => [...prev, { role: 'user', content: userMsg }]);
    setTimeout(() => {
      const response = generateBotResponse(userMsg, locale);
      setMessages(prev => [...prev, { role: 'bot', content: response }]);
    }, 600);
  };

  const handleEscalate = () => {
    setTicketCreated(true);
    setMessages(prev => [...prev, { role: 'bot', content: t('chatbot.ticket_created') }]);
    toast.success(t('chatbot.ticket_created'));
  };

  const handleClose = () => {
    setOpen(false);
  };

  // RTL — flip the floating bubble + popup to the opposite side so the
  // reading flow keeps the chat at the "end" of the viewport.
  const sideClass = isRtl ? 'left-6' : 'right-6';

  return (
    <>
      {/* Popup */}
      {open && (
        <div
          className={`fixed bottom-24 ${sideClass} z-50 w-[340px] sm:w-[380px] shadow-2xl rounded-2xl overflow-hidden border border-gray-200 bg-white flex flex-col`}
          style={{ maxHeight: '520px' }}
        >
          {/* Header */}
          <div className="bg-brand-primary px-4 py-3 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 rounded-full bg-white/20 flex items-center justify-center">
                <Bot className="w-4 h-4 text-white" />
              </div>
              <div>
                <p className="text-white text-sm font-medium leading-none">{t('chatbot.title')}</p>
                <Badge variant="outline" className="text-white border-white/40 text-[10px] mt-0.5 px-1.5 py-0">Online</Badge>
              </div>
            </div>
            <button onClick={handleClose} className="text-white/70 hover:text-white transition-colors" aria-label="Fermer">
              <X className="w-5 h-5" />
            </button>
          </div>

          {!started ? (
            /* Start screen */
            <div className="flex flex-col items-center justify-center flex-1 p-8 gap-4 text-center" style={{ minHeight: '200px' }}>
              <div className="w-14 h-14 rounded-full bg-brand-light flex items-center justify-center">
                <Bot className="w-7 h-7 text-brand-primary" />
              </div>
              <p className="text-sm text-muted-foreground">{t('chatbot.welcome')}</p>
              <Button className="bg-brand-primary hover:bg-brand-hover text-white" onClick={startChat}>
                {t('chatbot.start') || 'Démarrer la conversation'}
              </Button>
            </div>
          ) : (
            <>
              {/* Messages */}
              <div className="flex-1 overflow-y-auto p-4 space-y-3" style={{ minHeight: '260px', maxHeight: '320px' }}>
                {messages.map((msg, i) => (
                  <div key={i} className={`flex gap-2 ${msg.role === 'user' ? 'flex-row-reverse' : ''}`}>
                    <div className={`w-7 h-7 rounded-full flex items-center justify-center shrink-0 ${msg.role === 'bot' ? 'bg-brand-light' : 'bg-gray-200'}`}>
                      {msg.role === 'bot' ? <Bot className="w-3.5 h-3.5 text-brand-primary" /> : <User className="w-3.5 h-3.5 text-gray-600" />}
                    </div>
                    <div className={`max-w-[80%] rounded-xl px-3 py-2 text-sm ${msg.role === 'bot' ? 'bg-gray-100 text-foreground' : 'bg-brand-primary text-white'}`}>
                      {msg.content}
                    </div>
                  </div>
                ))}
                <div ref={scrollRef} />
              </div>

              {/* Escalation */}
              {!ticketCreated && messages.length > 2 && (
                <div className="px-4 pb-2">
                  <Button variant="outline" size="sm" className="text-warning border-warning hover:bg-warning/10 text-xs" onClick={handleEscalate}>
                    <LifeBuoy className="w-3.5 h-3.5 mr-1.5" />{t('chatbot.escalate')}
                  </Button>
                </div>
              )}

              {/* Input */}
              <div className="p-3 border-t">
                <form onSubmit={(e) => { e.preventDefault(); handleSend(); }} className="flex gap-2">
                  <Input
                    value={input}
                    onChange={(e) => setInput(e.target.value)}
                    placeholder={t('chatbot.placeholder')}
                    className="flex-1 text-sm h-9"
                    aria-label={t('chatbot.placeholder')}
                  />
                  <Button type="submit" size="sm" className="bg-brand-primary hover:bg-brand-hover text-white h-9 px-3" disabled={!input.trim()}>
                    <Send className="w-3.5 h-3.5" />
                  </Button>
                </form>
              </div>
            </>
          )}
        </div>
      )}

      {/* Floating button */}
      <button
        onClick={() => setOpen(o => !o)}
        aria-label="Chatbot"
        className={`fixed bottom-6 ${sideClass} z-50 w-14 h-14 rounded-full bg-brand-primary hover:bg-brand-hover text-white shadow-lg flex items-center justify-center transition-all duration-200 hover:scale-105`}
      >
        {open ? <X className="w-6 h-6" /> : <MessageCircle className="w-6 h-6" />}
      </button>
    </>
  );
}
