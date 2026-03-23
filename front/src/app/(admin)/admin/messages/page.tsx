'use client';

import { useState, useEffect } from 'react';
import { Mail, MessageSquare, Ticket, Copy, CheckCircle, Eye, Send, Loader2 } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Separator } from '@/components/ui/separator';
import { useI18n } from '@/context/i18n-context';
import { messagesService } from '@/lib/api-services';
import type { ContactMessageDto, ChatConversationDto, SupportTicketDto } from '@/lib/api-types';
import { MessageStatus, TicketStatus } from '@/lib/enums';
import { enumLabel } from '@/lib/enums';
import { toast } from 'sonner';

const MSG_STATUS_COLORS: Record<number, string> = {
  [MessageStatus.Unread]: 'bg-error/10 text-error border-error/20',
  [MessageStatus.Read]: 'bg-blue-50 text-blue-600 border-blue-200',
  [MessageStatus.Replied]: 'bg-success/10 text-success border-success/20',
  [MessageStatus.Archived]: 'bg-gray-100 text-muted-foreground',
};

const TICKET_STATUS_COLORS: Record<number, string> = {
  [TicketStatus.Open]: 'bg-error/10 text-error border-error/20',
  [TicketStatus.InProgress]: 'bg-warning/10 text-warning border-warning/20',
  [TicketStatus.Resolved]: 'bg-success/10 text-success border-success/20',
  [TicketStatus.Closed]: 'bg-gray-100 text-muted-foreground',
};

export default function AdminMessagesPage() {
  const { t, locale } = useI18n();
  const [messages, setMessages] = useState<ContactMessageDto[]>([]);
  const [chats, setChats] = useState<ChatConversationDto[]>([]);
  const [tickets, setTickets] = useState<SupportTicketDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [viewMessage, setViewMessage] = useState<ContactMessageDto | null>(null);
  const [viewChat, setViewChat] = useState<ChatConversationDto | null>(null);
  const [replyText, setReplyText] = useState('');

  useEffect(() => {
    const load = async () => {
      try {
        const [msgsRes, chatsRes, ticketsRes] = await Promise.all([
          messagesService.getAll(1, 100),
          messagesService.getConversations(1, 100),
          messagesService.getTickets(1, 100),
        ]);
        setMessages(msgsRes.data);
        setChats(chatsRes.data);
        setTickets(ticketsRes.data);
      } catch (err) {
        console.error('Failed to load messages', err);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const handleMessageStatus = async (id: string, status: number) => {
    try {
      await messagesService.updateStatus(id, status);
      setMessages(prev => prev.map(m => m.id === id ? { ...m, status: status as ContactMessageDto['status'] } : m));
      toast.success(locale === 'fr' ? 'Statut mis à jour' : 'Status updated');
    } catch (err) {
      console.error('Failed to update message status', err);
      toast.error(locale === 'fr' ? 'Erreur' : 'Error');
    }
  };

  const handleTicketStatus = async (id: string, status: number) => {
    try {
      const updated = await messagesService.updateTicket(id, status);
      setTickets(prev => prev.map(t => t.id === id ? updated : t));
      toast.success(locale === 'fr' ? 'Ticket mis à jour' : 'Ticket updated');
    } catch (err) {
      console.error('Failed to update ticket', err);
      toast.error(locale === 'fr' ? 'Erreur' : 'Error');
    }
  };

  const copyEmail = (email: string) => {
    navigator.clipboard.writeText(email);
    toast.success(locale === 'fr' ? 'Email copié' : 'Email copied');
  };

  const handleSendReply = async () => {
    if (!viewChat || !replyText.trim()) return;
    try {
      await messagesService.sendMessage(viewChat.id, replyText);
      toast.success(locale === 'fr' ? 'Réponse envoyée' : 'Reply sent');
      setReplyText('');
    } catch (err) {
      console.error('Failed to send reply', err);
      toast.error(locale === 'fr' ? 'Erreur lors de l\'envoi' : 'Failed to send reply');
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-8 h-8 animate-spin text-brand-primary" />
      </div>
    );
  }

  const unreadCount = messages.filter(m => m.status === MessageStatus.Unread).length;
  const escalatedCount = chats.filter(c => c.escalated).length;
  const openTickets = tickets.filter(t => t.status !== TicketStatus.Closed && t.status !== TicketStatus.Resolved).length;

  return (
    <div className="space-y-4">
      {/* Summary */}
      <div className="grid grid-cols-3 gap-4">
        <Card><CardContent className="p-4 flex items-center gap-3">
          <Mail className="w-5 h-5 text-error" />
          <div><p className="text-xl font-bold">{unreadCount}</p><p className="text-xs text-muted-foreground">{locale === 'fr' ? 'Messages non lus' : 'Unread messages'}</p></div>
        </CardContent></Card>
        <Card><CardContent className="p-4 flex items-center gap-3">
          <MessageSquare className="w-5 h-5 text-warning" />
          <div><p className="text-xl font-bold">{escalatedCount}</p><p className="text-xs text-muted-foreground">{locale === 'fr' ? 'Conversations escaladées' : 'Escalated chats'}</p></div>
        </CardContent></Card>
        <Card><CardContent className="p-4 flex items-center gap-3">
          <Ticket className="w-5 h-5 text-brand-primary" />
          <div><p className="text-xl font-bold">{openTickets}</p><p className="text-xs text-muted-foreground">{locale === 'fr' ? 'Tickets ouverts' : 'Open tickets'}</p></div>
        </CardContent></Card>
      </div>

      <Tabs defaultValue="messages">
        <TabsList>
          <TabsTrigger value="messages">
            <Mail className="w-4 h-4 mr-1" />{locale === 'fr' ? 'Messages' : 'Messages'} ({messages.length})
          </TabsTrigger>
          <TabsTrigger value="chats">
            <MessageSquare className="w-4 h-4 mr-1" />{locale === 'fr' ? 'Conversations' : 'Chats'} ({chats.length})
          </TabsTrigger>
          <TabsTrigger value="tickets">
            <Ticket className="w-4 h-4 mr-1" />Tickets ({tickets.length})
          </TabsTrigger>
        </TabsList>

        {/* Contact Messages */}
        <TabsContent value="messages" className="mt-4">
          <Card>
            <CardContent className="p-0">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-gray-50">
                    <th className="p-3 text-left">Email</th>
                    <th className="p-3 text-left">{locale === 'fr' ? 'Sujet' : 'Subject'}</th>
                    <th className="p-3 text-left">Date</th>
                    <th className="p-3 text-center">Status</th>
                    <th className="p-3 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {[...messages].sort((a, b) => b.createdAt.localeCompare(a.createdAt)).map(msg => (
                    <tr key={msg.id} className={`border-b hover:bg-gray-50/50 ${msg.status === MessageStatus.Unread ? 'bg-blue-50/30' : ''}`}>
                      <td className="p-3">
                        <button className="text-brand-primary hover:underline text-xs" onClick={() => copyEmail(msg.email)}>
                          {msg.email} <Copy className="w-3 h-3 inline ml-1" />
                        </button>
                      </td>
                      <td className="p-3">
                        <button onClick={() => { setViewMessage(msg); if (msg.status === MessageStatus.Unread) handleMessageStatus(msg.id, MessageStatus.Read); }} className="text-left hover:text-brand-primary">
                          <span className={msg.status === MessageStatus.Unread ? 'font-semibold' : ''}>{msg.subject}</span>
                        </button>
                      </td>
                      <td className="p-3 text-muted-foreground text-xs">{new Date(msg.createdAt).toLocaleDateString(locale === 'fr' ? 'fr-FR' : 'en-US')}</td>
                      <td className="p-3 text-center">
                        <Badge variant="outline" className={MSG_STATUS_COLORS[msg.status] || ''}>{enumLabel('MessageStatus', msg.status, locale)}</Badge>
                      </td>
                      <td className="p-3 text-right">
                        <div className="flex items-center justify-end gap-1">
                          <Button size="icon" variant="ghost" className="h-7 w-7" onClick={() => { setViewMessage(msg); if (msg.status === MessageStatus.Unread) handleMessageStatus(msg.id, MessageStatus.Read); }}>
                            <Eye className="w-3.5 h-3.5" />
                          </Button>
                          {msg.status !== MessageStatus.Replied && msg.status !== MessageStatus.Archived && (
                            <Button size="icon" variant="ghost" className="h-7 w-7 text-success" onClick={() => handleMessageStatus(msg.id, MessageStatus.Replied)}>
                              <CheckCircle className="w-3.5 h-3.5" />
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Chat Conversations */}
        <TabsContent value="chats" className="mt-4">
          <div className="space-y-3">
            {[...chats].sort((a, b) => b.createdAt.localeCompare(a.createdAt)).map(chat => (
              <Card key={chat.id} className={chat.escalated ? 'border-warning' : ''}>
                <CardContent className="p-4">
                  <div className="flex items-center justify-between mb-2">
                    <div className="flex items-center gap-2">
                      <span className="text-xs text-muted-foreground">{chat.email || chat.userId || 'Anonymous'}</span>
                      {chat.escalated && <Badge className="bg-warning/10 text-warning text-[10px]">{locale === 'fr' ? 'Escaladé' : 'Escalated'}</Badge>}
                      {chat.ticketId && <Badge variant="outline" className="text-[10px]">Ticket: {chat.ticketId}</Badge>}
                    </div>
                    <span className="text-xs text-muted-foreground">{new Date(chat.createdAt).toLocaleDateString(locale === 'fr' ? 'fr-FR' : 'en-US')}</span>
                  </div>
                  <div className="text-xs text-muted-foreground mb-2">{chat.messages.length} messages</div>
                  <Button size="sm" variant="outline" onClick={() => setViewChat(chat)}>
                    <Eye className="w-3.5 h-3.5 mr-1" />{locale === 'fr' ? 'Voir la conversation' : 'View conversation'}
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        {/* Support Tickets */}
        <TabsContent value="tickets" className="mt-4">
          <Card>
            <CardContent className="p-0">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-gray-50">
                    <th className="p-3 text-left">ID</th>
                    <th className="p-3 text-left">{locale === 'fr' ? 'Sujet' : 'Subject'}</th>
                    <th className="p-3 text-left">Email</th>
                    <th className="p-3 text-center">Status</th>
                    <th className="p-3 text-left">{locale === 'fr' ? 'Mis à jour' : 'Updated'}</th>
                    <th className="p-3 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {[...tickets].sort((a, b) => b.updatedAt.localeCompare(a.updatedAt)).map(ticket => (
                    <tr key={ticket.id} className="border-b hover:bg-gray-50/50">
                      <td className="p-3 font-medium text-brand-primary">{ticket.id}</td>
                      <td className="p-3">{ticket.subject}</td>
                      <td className="p-3 text-muted-foreground text-xs">{ticket.email}</td>
                      <td className="p-3 text-center">
                        <Badge variant="outline" className={TICKET_STATUS_COLORS[ticket.status] || ''}>
                          {enumLabel('TicketStatus', ticket.status, locale)}
                        </Badge>
                      </td>
                      <td className="p-3 text-muted-foreground text-xs">{new Date(ticket.updatedAt).toLocaleDateString(locale === 'fr' ? 'fr-FR' : 'en-US')}</td>
                      <td className="p-3 text-right">
                        <div className="flex items-center justify-end gap-1">
                          {ticket.status === TicketStatus.Open && (
                            <Button size="sm" variant="outline" className="h-7 text-xs" onClick={() => handleTicketStatus(ticket.id, TicketStatus.InProgress)}>
                              {locale === 'fr' ? 'Prendre en charge' : 'Take over'}
                            </Button>
                          )}
                          {ticket.status === TicketStatus.InProgress && (
                            <Button size="sm" variant="outline" className="h-7 text-xs text-success" onClick={() => handleTicketStatus(ticket.id, TicketStatus.Closed)}>
                              {locale === 'fr' ? 'Fermer' : 'Close'}
                            </Button>
                          )}
                          {ticket.status === TicketStatus.Closed && (
                            <Button size="sm" variant="outline" className="h-7 text-xs" onClick={() => handleTicketStatus(ticket.id, TicketStatus.Open)}>
                              {locale === 'fr' ? 'Réouvrir' : 'Reopen'}
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* View Message Dialog */}
      <Dialog open={!!viewMessage} onOpenChange={() => setViewMessage(null)}>
        <DialogContent className="max-w-md">
          {viewMessage && (
            <>
              <DialogHeader>
                <DialogTitle>{viewMessage.subject}</DialogTitle>
              </DialogHeader>
              <div className="space-y-3 text-sm">
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>{viewMessage.email}</span>
                  <span>{new Date(viewMessage.createdAt).toLocaleDateString(locale === 'fr' ? 'fr-FR' : 'en-US')}</span>
                </div>
                <Separator />
                <p className="whitespace-pre-wrap text-muted-foreground">{viewMessage.message}</p>
                <Separator />
                <div className="flex gap-2">
                  <Button size="sm" variant="outline" onClick={() => copyEmail(viewMessage.email)}>
                    <Copy className="w-3.5 h-3.5 mr-1" />{t('admin.copy_email')}
                  </Button>
                  {viewMessage.status !== MessageStatus.Replied && viewMessage.status !== MessageStatus.Archived && (
                    <Button size="sm" className="bg-success hover:bg-success/90 text-white" onClick={() => { handleMessageStatus(viewMessage.id, MessageStatus.Replied); setViewMessage(null); }}>
                      <CheckCircle className="w-3.5 h-3.5 mr-1" />{t('admin.mark_treated')}
                    </Button>
                  )}
                </div>
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>

      {/* View Chat Dialog */}
      <Dialog open={!!viewChat} onOpenChange={() => { setViewChat(null); setReplyText(''); }}>
        <DialogContent className="max-w-md max-h-[80vh] flex flex-col">
          {viewChat && (
            <>
              <DialogHeader>
                <DialogTitle>{locale === 'fr' ? 'Conversation' : 'Conversation'} - {viewChat.email || 'Anonymous'}</DialogTitle>
              </DialogHeader>
              <div className="flex-1 overflow-y-auto space-y-2 py-2">
                {viewChat.messages.map(msg => (
                  <div key={msg.id} className={`flex ${msg.role === 0 ? 'justify-end' : 'justify-start'}`}>
                    <div className={`max-w-[80%] rounded-lg px-3 py-2 text-xs ${msg.role === 0 ? 'bg-brand-primary text-white' : 'bg-gray-100'}`}>
                      {msg.content}
                    </div>
                  </div>
                ))}
              </div>
              <Separator />
              <div className="flex gap-2 pt-2">
                <input
                  type="text"
                  className="flex-1 border rounded-md px-3 py-2 text-sm"
                  placeholder={locale === 'fr' ? 'Répondre...' : 'Reply...'}
                  value={replyText}
                  onChange={e => setReplyText(e.target.value)}
                />
                <Button size="sm" className="bg-brand-primary hover:bg-brand-hover text-white" disabled={!replyText.trim()} onClick={handleSendReply}>
                  <Send className="w-3.5 h-3.5" />
                </Button>
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
