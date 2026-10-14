export type LlmModel = 'GptOss20B' | 'GptOss120B' | 'KimiK2' | 'Llama4Scout';

export interface UserDto {
  id: string;
  name: string;
  email: string;
  phoneNumber?: string | null;
  role: string;
  organizationId?: number | null;
  organizationName?: string | null;
  isActive: boolean;
  createdAtUtc: string;
  chatbotCount: number;
}

export interface AuthResponse {
  user: UserDto;
  accessExpiresAtUtc: string;
  csrfToken: string;
}

export interface RegisterRequest {
  organizationName: string;
  name: string;
  email: string;
  password: string;
  phoneNumber?: string | null;
  acceptTerms: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface ForgotPasswordRequest { email: string; }
export interface ResetPasswordRequest { token: string; newPassword: string; }
export interface ChangePasswordRequest { currentPassword: string; newPassword: string; }

export interface ChatbotSummaryDto {
  id: number;
  organizationId: number;
  createdByUserId?: string | null;
  name: string;
  description?: string | null;
  systemPrompt?: string | null;
  apiKeyPrefix?: string | null;
  configuration?: LlmConfigDto | null;
  totalQueries: number;
  documentCount: number;
  totalChunks: number;
}

export interface ChatBotCreateDto {
  organizationId: number;
  name: string;
  description?: string | null;
  systemPrompt?: string | null;
}

export interface ChatbotUpdateRequest {
  name: string;
  description?: string | null;
  systemPrompt?: string | null;
}

export interface LlmConfigDto {
  chatBotId: number;
  modelName: LlmModel;
  temperature: number;
  maxToken: number;
  topP: number;
  systemPrompt?: string | null;
}

export interface ModelDto {
  id: LlmModel;
  name: string;
  providerModel: string;
  available: boolean;
  unavailableReason?: string | null;
}

export interface OrganizationSummaryDto {
  id: number;
  name: string;
  email?: string | null;
  phoneNumber?: string | null;
  chatbotCount: number;
}

export interface OrganizationDetailsDto {
  name: string;
  email?: string | null;
  phoneNumber?: string | null;
}

export type ReaderType = 'Pdf' | 'Text';

export interface DocumentDetailsDto {
  id: number;
  chatBotId: number;
  name: string;
  size: number;
  uploadDate: string;
  readerType: ReaderType;
  status: string;
  chunks: number;
  error?: string | null;
}

export interface DocumentUploadForm {
  chatBotId: number;
  name: string;
  size: string;
  uploadDate: string;
  status: string;
  file: File;
  fileUrl?: string | null;
  readerType: ReaderType;
}

export interface ChatAnswerDto {
  chatBotId: number;
  conversationId: string;
  answer: string;
}

export interface ChatStreamEvent {
  type: string;
  text?: string | null;
  conversationId?: string | null;
}

export interface ChatHistoryResponseDto {
  userPromt?: string | null;
  systemMessage?: string | null;
  id?: string | null;
  conversationId: string;
  createdAtUtc: string;
}

export interface ChatbotAccessDto {
  chatBotId: number;
  name?: string | null;
  description?: string | null;
}

export interface ApiKeyResult {
  chatBotId: number;
  apiKey: string;
  prefix: string;
}

export interface ApiKeyDto { chatBotId: number; }

export interface DashboardDto {
  chatbotCount: number;
  totalQueries: number;
  documentCount: number;
  processingCount: number;
  totalChunks: number;
  chatbots: ChatbotSummaryDto[];
}

export type ToastType = 'info' | 'success' | 'error';
export interface Toast {
  id: number;
  message: string;
  type: ToastType;
}
