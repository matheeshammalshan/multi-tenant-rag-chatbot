import type {
  UserDto,
  AuthResponse,
  RegisterRequest,
  LoginRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  ChangePasswordRequest,
  ChatbotSummaryDto,
  ChatBotCreateDto,
  ChatbotUpdateRequest,
  LlmConfigDto,
  ModelDto,
  OrganizationSummaryDto,
  OrganizationDetailsDto,
  DocumentDetailsDto,
  ChatAnswerDto,
  ChatHistoryResponseDto,
  ChatbotAccessDto,
  ApiKeyResult,
  ApiKeyDto,
  DashboardDto,
} from './types';
import { apiFetch, apiFetchWithStatus, streamChat, type ChatStreamEvent as ApiChatStreamEvent } from './api';

export const auth = {
  login: (data: LoginRequest): Promise<AuthResponse> =>
    apiFetch('/Auth/login', { method: 'POST', body: JSON.stringify(data) }),
  register: (data: RegisterRequest): Promise<AuthResponse> =>
    apiFetch('/Auth/register', { method: 'POST', body: JSON.stringify(data) }),
  logout: (): Promise<{ status: number }> =>
    apiFetchWithStatus('/Auth/logout', { method: 'POST' }).then(r => ({ status: r.status })),
  me: (): Promise<UserDto> => apiFetch('/Auth/me'),
  forgotPassword: (data: ForgotPasswordRequest): Promise<{ status: number }> =>
    apiFetchWithStatus('/Auth/forgot-password', { method: 'POST', body: JSON.stringify(data) }).then(r => ({ status: r.status })),
  resetPassword: (data: ResetPasswordRequest): Promise<{ status: number }> =>
    apiFetchWithStatus('/Auth/reset-password', { method: 'POST', body: JSON.stringify(data) }).then(r => ({ status: r.status })),
  changePassword: (data: ChangePasswordRequest): Promise<{ status: number }> =>
    apiFetchWithStatus('/Auth/change-password', { method: 'POST', body: JSON.stringify(data) }).then(r => ({ status: r.status })),
};

export const chatbots = {
  list: (): Promise<ChatbotSummaryDto[]> => apiFetch('/ChatBot'),
  listForOrg: (organizationId: number): Promise<ChatbotSummaryDto[]> =>
    apiFetch(`/ChatBot?organizationId=${organizationId}`),
  get: (id: number): Promise<ChatbotSummaryDto> => apiFetch(`/ChatBot/${id}`),
  create: (data: ChatBotCreateDto): Promise<ChatbotSummaryDto> =>
    apiFetch('/ChatBot/create', { method: 'POST', body: JSON.stringify(data) }),
  update: (id: number, data: ChatbotUpdateRequest): Promise<ChatbotSummaryDto> =>
    apiFetch(`/ChatBot/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  assignOwner: (id: number, userId: string): Promise<{ status: number }> =>
    apiFetchWithStatus(`/ChatBot/${id}/owner`, { method: 'PUT', body: JSON.stringify(userId) }).then(r => ({ status: r.status })),
};

export const models = {
  list: (): Promise<ModelDto[]> => apiFetch('/Models'),
};

export const llmConfig = {
  get: (chatBotId: number): Promise<LlmConfigDto> => apiFetch(`/LlmConfig/${chatBotId}`),
  update: (data: LlmConfigDto): Promise<LlmConfigDto> =>
    apiFetch('/LlmConfig/update', { method: 'PUT', body: JSON.stringify(data) }),
};

export const documents = {
  list: (chatBotId: number): Promise<DocumentDetailsDto[]> => apiFetch(`/Document?chatBotId=${chatBotId}`),
  get: (id: number): Promise<DocumentDetailsDto> => apiFetch(`/Document/${id}`),
  upload: (form: FormData): Promise<unknown> => {
    return apiFetch('/Document/create', { method: 'POST', body: form });
  },
};

export const semanticSearch = {
  ask: (chatBotId: number, query: string, apiKey: string, conversationId?: string | null): Promise<ChatAnswerDto> => {
    const body: { chatBotId: number; query: string; conversationId?: string } = { chatBotId, query };
    if (conversationId) body.conversationId = conversationId;
    return apiFetch('/SemanticSearch/askQuery', {
      method: 'POST',
      headers: { 'x-api-key': apiKey },
      body: JSON.stringify(body),
    });
  },
  stream: (chatBotId: number, query: string, apiKey: string | null, onEvent: (event: ApiChatStreamEvent) => void, conversationId?: string | null, signal?: AbortSignal): Promise<void> =>
    streamChat(chatBotId, query, apiKey, conversationId, onEvent, signal),
};

export const chatHistory = {
  list: (chatBotId: number, conversationId?: string | null): Promise<ChatHistoryResponseDto[]> => {
    const params = new URLSearchParams();
    if (conversationId) params.set('conversationId', conversationId);
    return apiFetch(`/ChatHistory/${chatBotId}?${params.toString()}`);
  },
};

export const organizations = {
  list: (): Promise<OrganizationSummaryDto[]> => apiFetch('/Organization'),
  get: (id: number): Promise<OrganizationSummaryDto> => apiFetch(`/Organization/${id}`),
  create: (data: OrganizationDetailsDto): Promise<OrganizationSummaryDto> =>
    apiFetch('/Organization/create', { method: 'POST', body: JSON.stringify(data) }),
};

export const users = {
  list: (): Promise<UserDto[]> => apiFetch('/Users'),
  assignOrg: (id: string, orgId: number): Promise<{ status: number }> =>
    apiFetchWithStatus(`/Users/${id}/organization`, { method: 'PUT', body: JSON.stringify(orgId) }).then(r => ({ status: r.status })),
  setStatus: (id: string, active: boolean): Promise<{ status: number }> =>
    apiFetchWithStatus(`/Users/${id}/status`, { method: 'PUT', body: JSON.stringify(active) }).then(r => ({ status: r.status })),
};

export const dashboard = {
  get: (organizationId?: number): Promise<DashboardDto> => {
    const params = new URLSearchParams();
    if (organizationId) params.set('organizationId', organizationId.toString());
    return apiFetch(`/Dashboard?${params.toString()}`);
  },
};

export const apiKey = {
  resolve: (key: string): Promise<ChatbotAccessDto> =>
    apiFetch('/ChatbotAccess', { headers: { 'x-api-key': key } }),
  generate: (data: ApiKeyDto): Promise<ApiKeyResult> =>
    apiFetch('/Api/create-apikey', { method: 'POST', body: JSON.stringify(data) }),
  rotate: (id: number): Promise<ApiKeyResult> =>
    apiFetch(`/ChatBots/${id}/api-key`, { method: 'POST' }),
};

export type { ApiChatStreamEvent };
