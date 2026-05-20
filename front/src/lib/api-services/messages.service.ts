import { api } from '@/lib/api';
import type {
  ContactMessageDto,
  ChatConversationDto,
  ChatMessageDto,
  SupportTicketDto,
  PaginatedResponse,
} from '@/lib/api-types';

export const messagesService = {
  // Contact messages
  getAll: (page = 1, pageSize = 20) =>
    api.get<PaginatedResponse<ContactMessageDto>>(`/messages?page=${page}&pageSize=${pageSize}`),

  getById: (id: string) =>
    api.get<ContactMessageDto>(`/messages/${id}`),

  create: (data: { email: string; subject: string; message: string }) =>
    api.post<ContactMessageDto>('/messages', data),

  updateStatus: (id: string, status: number) =>
    api.put<void>(`/messages/${id}/status`, status),

  delete: (id: string) =>
    api.delete(`/messages/${id}`),

  // Chat
  getConversations: (page = 1, pageSize = 20) =>
    api.get<PaginatedResponse<ChatConversationDto>>(`/chat?page=${page}&pageSize=${pageSize}`),

  getConversation: (id: string) =>
    api.get<ChatConversationDto>(`/chat/${id}`),

  createConversation: (userId?: string, email?: string) => {
    let endpoint = '/chat';
    const params = new URLSearchParams();
    if (userId) params.set('userId', userId);
    if (email) params.set('email', email);
    if (params.toString()) endpoint += `?${params}`;
    return api.post<ChatConversationDto>(endpoint);
  },

  /**
   * `locale` is the UI language currently selected on the front (fr / en /
   * ms / ar). The backend uses it to instruct the LLM to reply in the
   * same language. Omit / null = backend falls back to French.
   */
  sendMessage: (conversationId: string, content: string, locale?: string) =>
    api.post<ChatMessageDto>(`/chat/${conversationId}/messages`, { content, locale }),

  // Tickets
  getTickets: (page = 1, pageSize = 20) =>
    api.get<PaginatedResponse<SupportTicketDto>>(`/tickets?page=${page}&pageSize=${pageSize}`),

  getTicket: (id: string) =>
    api.get<SupportTicketDto>(`/tickets/${id}`),

  updateTicket: (id: string, status: number) =>
    api.put<SupportTicketDto>(`/tickets/${id}`, { status }),
};
