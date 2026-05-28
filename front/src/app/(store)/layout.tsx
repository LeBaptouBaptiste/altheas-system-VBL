import { Header } from '@/components/Header';
import { Footer } from '@/components/Footer';
import { ChatbotBubble } from '@/components/ChatbotBubble';

export default function StoreLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen flex flex-col">
      <Header />
      <main className="flex-1">{children}</main>
      <Footer />
      <ChatbotBubble />
    </div>
  );
}
