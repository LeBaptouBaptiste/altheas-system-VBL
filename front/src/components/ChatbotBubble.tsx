'use client';

import { useState, useRef, useEffect } from 'react';
import { Send, Bot, User, X, LifeBuoy, MessageCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { useI18n } from '@/context/i18n-context';
import { products } from '@/mock';
import { formatPrice, calculateTTC, toIntlLocale } from '@/lib/money';
import { VAT_RATES } from '@/lib/constants';
import { toast } from 'sonner';

interface Message { role: 'user' | 'bot'; content: string; }

type BotLocale = 'fr' | 'en' | 'ms' | 'ar';

const BOT_STRINGS: Record<string, Record<BotLocale, string>> = {
  stock_in:    { fr: 'en stock',    en: 'in stock',     ms: 'dalam stok',      ar: 'متوفر في المخزون' },
  stock_low:   { fr: 'stock faible',en: 'low stock',    ms: 'stok rendah',     ar: 'مخزون منخفض' },
  stock_out:   { fr: 'rupture',     en: 'out of stock', ms: 'kehabisan stok',  ar: 'نفاد المخزون' },
  product_tpl: {
    fr: (name: string, price: string, stock: string, desc: string) => `Le ${name} est proposé à ${price} TTC. Statut : ${stock}. ${desc}`,
    en: (name: string, price: string, stock: string, desc: string) => `The ${name} is available at ${price} incl. VAT. Status: ${stock}. ${desc}`,
    ms: (name: string, price: string, stock: string, desc: string) => `${name} ditawarkan pada ${price} termasuk CBP. Status: ${stock}. ${desc}`,
    ar: (name: string, price: string, stock: string, desc: string) => `${name} متاح بسعر ${price} شامل ضريبة القيمة المضافة. الحالة: ${stock}. ${desc}`,
  } as unknown as Record<BotLocale, string>,
  price: {
    fr: 'Nos prix sont affichés TTC sur chaque fiche produit. Pour un devis personnalisé, contactez notre équipe commerciale.',
    en: 'Our prices are displayed including VAT on each product page. For a custom quote, contact our sales team.',
    ms: 'Harga kami dipaparkan termasuk cukai pada setiap halaman produk. Untuk sebut harga, hubungi pasukan jualan kami.',
    ar: 'تُعرض أسعارنا شاملة الضريبة على كل صفحة منتج. للحصول على عرض سعر مخصص، تواصل مع فريق المبيعات.',
  },
  shipping: {
    fr: 'Nous proposons 3 modes de livraison : Standard (5-7 jours, 15€), Express (2-3 jours, 35€), 24h (75€).',
    en: 'We offer 3 shipping methods: Standard (5-7 days, €15), Express (2-3 days, €35), Overnight (€75).',
    ms: 'Kami menawarkan 3 kaedah penghantaran: Standard (5-7 hari, €15), Ekspres (2-3 hari, €35), 24 jam (€75).',
    ar: 'نقدم 3 طرق شحن: عادي (5-7 أيام، 15€)، سريع (2-3 أيام، 35€)، 24 ساعة (75€).',
  },
  returns: {
    fr: 'Pour tout retour ou SAV, contactez notre service client. Je peux créer un ticket si vous le souhaitez.',
    en: 'For any returns or after-sales service, contact our customer service. I can create a ticket if you wish.',
    ms: 'Untuk sebarang pemulangan atau perkhidmatan selepas jualan, hubungi khidmat pelanggan kami. Saya boleh buat tiket jika anda mahu.',
    ar: 'لأي إرجاع أو خدمة ما بعد البيع، تواصل مع خدمة العملاء. يمكنني إنشاء تذكرة دعم إن أردت.',
  },
  hours: {
    fr: 'Notre service client est disponible du lundi au vendredi de 8h à 18h. Je suis disponible 24h/24 !',
    en: "Our customer service is available Monday to Friday from 8am to 6pm. I'm available 24/7!",
    ms: 'Khidmat pelanggan kami tersedia Isnin hingga Jumaat 8 pagi hingga 6 petang. Saya sedia 24/7!',
    ar: 'خدمة العملاء متاحة من الإثنين إلى الجمعة من 8 صباحاً حتى 6 مساءً. أنا متاح على مدار الساعة!',
  },
  greeting: {
    fr: "Bonjour ! Comment puis-je vous aider aujourd'hui ?",
    en: 'Hello! How can I help you today?',
    ms: 'Helo! Bagaimana saya boleh membantu anda hari ini?',
    ar: 'مرحباً! كيف يمكنني مساعدتك اليوم؟',
  },
  fallback: {
    fr: "Je ne suis pas sûr de pouvoir répondre à cette question. Souhaitez-vous contacter notre support technique ? Je peux créer un ticket pour vous.",
    en: "I'm not sure I can answer this question. Would you like to contact our technical support? I can create a ticket for you.",
    ms: 'Saya tidak pasti boleh menjawab soalan ini. Adakah anda ingin menghubungi sokongan teknikal kami? Saya boleh buat tiket untuk anda.',
    ar: 'لست متأكداً من إمكانية الإجابة على هذا السؤال. هل تريد التواصل مع الدعم الفني؟ يمكنني إنشاء تذكرة لك.',
  },
};

function s(key: string, locale: string): string {
  return (BOT_STRINGS[key]?.[locale as BotLocale] ?? BOT_STRINGS[key]?.en) as string;
}

function generateBotResponse(input: string, locale: string): string {
  const q = input.toLowerCase();
  const fmt = (n: number) => formatPrice(n, toIntlLocale(locale));

  for (const p of products) {
    const name = (p.name[locale] || p.name.fr).toLowerCase();
    if (q.includes(name.split(' ')[0].toLowerCase()) || q.includes(p.slug.split('-')[0])) {
      const price = calculateTTC(p.priceHT, VAT_RATES[p.vatRate]);
      const stockKey = p.stockStatus === 'in_stock' ? 'stock_in' : p.stockStatus === 'low_stock' ? 'stock_low' : 'stock_out';
      const stock = s(stockKey, locale);
      const tpl = BOT_STRINGS.product_tpl[locale as BotLocale] as unknown as (n: string, p: string, st: string, d: string) => string
        ?? BOT_STRINGS.product_tpl.en as unknown as (n: string, p: string, st: string, d: string) => string;
      return tpl(p.name[locale] || p.name.en, fmt(price), stock, p.description[locale] || p.description.en);
    }
  }

  if (q.includes('prix') || q.includes('price') || q.includes('tarif') || q.includes('harga') || q.includes('سعر')) return s('price', locale);
  if (q.includes('livraison') || q.includes('delivery') || q.includes('shipping') || q.includes('penghantaran') || q.includes('شحن')) return s('shipping', locale);
  if (q.includes('retour') || q.includes('return') || q.includes('sav') || q.includes('pemulangan') || q.includes('إرجاع')) return s('returns', locale);
  if (q.includes('horaire') || q.includes('hour') || q.includes('contact') || q.includes('waktu') || q.includes('ساعة')) return s('hours', locale);
  if (q.includes('bonjour') || q.includes('hello') || q.includes('hi') || q.includes('salut') || q.includes('helo') || q.includes('مرحبا') || q.includes('مرحباً')) return s('greeting', locale);

  return s('fallback', locale);
}

export function ChatbotBubble() {
  const { t, locale } = useI18n();
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

  return (
    <>
      {/* Popup */}
      {open && (
        <div className="fixed bottom-24 right-6 z-50 w-[340px] sm:w-[380px] shadow-2xl rounded-2xl overflow-hidden border border-gray-200 bg-white flex flex-col" style={{ maxHeight: '520px' }}>
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
        className="fixed bottom-6 right-6 z-50 w-14 h-14 rounded-full bg-brand-primary hover:bg-brand-hover text-white shadow-lg flex items-center justify-center transition-all duration-200 hover:scale-105"
      >
        {open ? <X className="w-6 h-6" /> : <MessageCircle className="w-6 h-6" />}
      </button>
    </>
  );
}
