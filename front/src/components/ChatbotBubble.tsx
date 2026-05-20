'use client';

import { useState, useRef, useEffect } from 'react';
import Link from 'next/link';
import { Send, Bot, User, X, LifeBuoy, MessageCircle, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { useI18n } from '@/context/i18n-context';
import { useAuth } from '@/context/auth-context';
import { messagesService } from '@/lib/api-services';
import { toast } from 'sonner';

interface Message { role: 'user' | 'bot'; content: string; }

export function ChatbotBubble() {
  const { t, dir, locale } = useI18n();
  const { isAuthenticated, user } = useAuth();
  const isRtl = dir === 'rtl';
  const [open, setOpen] = useState(false);
  const [started, setStarted] = useState(false);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  // Disable input + show spinner while Ollama is generating a reply.
  // CPU-only models take 5-20 seconds — a clear loading state matters.
  const [sending, setSending] = useState(false);
  const [ticketCreated, setTicketCreated] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (open && scrollRef.current) {
      scrollRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [messages, open]);

  const startChat = async () => {
    if (!isAuthenticated || !user) return;
    // Create a conversation on the backend so subsequent sendMessage calls
    // have a target. Failure here means the chat can't start at all —
    // surface an explicit error rather than landing on a broken UI.
    try {
      const conv = await messagesService.createConversation(user.id);
      setConversationId(conv.id);
      setStarted(true);
      setMessages([{ role: 'bot', content: t('chatbot.welcome') }]);
    } catch (err) {
      console.error('Failed to create chat conversation', err);
      toast.error(locale === 'fr'
        ? 'Impossible de démarrer la conversation'
        : 'Failed to start conversation');
    }
  };

  const handleSend = async () => {
    if (!input.trim() || !conversationId || sending) return;
    const userMsg = input.trim();
    setInput('');
    // Optimistic append so the customer sees their message immediately.
    setMessages((prev) => [...prev, { role: 'user', content: userMsg }]);
    setSending(true);
    try {
      // Backend persists the user message, calls Ollama, persists the bot
      // reply, and returns the bot's reply (role=Bot=1). Pass the current
      // UI locale so the bot answers in the customer's language even if
      // the conversation history is mixed (user switched mid-chat).
      const reply = await messagesService.sendMessage(conversationId, userMsg, locale);
      setMessages((prev) => [...prev, { role: 'bot', content: reply.content }]);
    } catch (err) {
      console.error('Chat send failed', err);
      // Polite inline error so the customer knows the bubble isn't broken,
      // just temporarily unresponsive. The user message stays — the
      // transcript on the server reflects the same.
      setMessages((prev) => [
        ...prev,
        {
          role: 'bot',
          content: locale === 'fr'
            ? "Désolé, je n'ai pas pu répondre. Réessayez dans un instant ou créez un ticket."
            : "Sorry, I couldn't reply. Try again in a moment or open a ticket.",
        },
      ]);
    } finally {
      setSending(false);
    }
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
            /* Start screen — gated on auth. The Ollama-backed endpoint
               requires JWT, so anonymous visitors get a login CTA instead
               of a broken "Send" button. */
            <div className="flex flex-col items-center justify-center flex-1 p-8 gap-4 text-center" style={{ minHeight: '200px' }}>
              <div className="w-14 h-14 rounded-full bg-brand-light flex items-center justify-center">
                <Bot className="w-7 h-7 text-brand-primary" />
              </div>
              {isAuthenticated ? (
                <>
                  <p className="text-sm text-muted-foreground">{t('chatbot.welcome')}</p>
                  <Button className="bg-brand-primary hover:bg-brand-hover text-white" onClick={startChat}>
                    {t('chatbot.start') || 'Démarrer la conversation'}
                  </Button>
                </>
              ) : (
                <>
                  <p className="text-sm text-muted-foreground">
                    {locale === 'fr'
                      ? 'Connectez-vous pour discuter avec notre assistant.'
                      : 'Log in to chat with our assistant.'}
                  </p>
                  <Button asChild className="bg-brand-primary hover:bg-brand-hover text-white">
                    <Link href="/login">
                      {locale === 'fr' ? 'Se connecter' : 'Log in'}
                    </Link>
                  </Button>
                </>
              )}
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
                {/* Typing indicator while Ollama is generating — CPU-only
                    models can take 10-20 s so explicit feedback matters. */}
                {sending && (
                  <div className="flex gap-2">
                    <div className="w-7 h-7 rounded-full flex items-center justify-center shrink-0 bg-brand-light">
                      <Bot className="w-3.5 h-3.5 text-brand-primary" />
                    </div>
                    <div className="rounded-xl px-3 py-2 text-sm bg-gray-100 text-muted-foreground inline-flex items-center gap-2">
                      <Loader2 className="w-3.5 h-3.5 animate-spin" />
                      <span>{locale === 'fr' ? 'Réflexion…' : 'Thinking…'}</span>
                    </div>
                  </div>
                )}
                <div ref={scrollRef} />
              </div>

              {/* Escalation */}
              {!ticketCreated && messages.length > 2 && (
                <div className="px-4 pb-2">
                  <Button variant="outline" size="sm" className="text-warning border-warning hover:bg-warning/10 text-xs" onClick={handleEscalate}>
                    <LifeBuoy className="w-3.5 h-3.5 me-1.5" />{t('chatbot.escalate')}
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
                    disabled={sending}
                  />
                  <Button type="submit" size="sm" className="bg-brand-primary hover:bg-brand-hover text-white h-9 px-3" disabled={!input.trim() || sending}>
                    {sending ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Send className="w-3.5 h-3.5" />}
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
