const BASE_URL = import.meta.env.DEV ? '' : (import.meta.env.VITE_API_URL || '');

function getCsrfToken(): string {
  const cookie = document.cookie.split('; ').find(value => value.startsWith('csrf_token='));
  return cookie ? decodeURIComponent(cookie.slice('csrf_token='.length)) : '';
}

export class ApiError extends Error {
  readonly status: number;
  constructor(message: string, status: number) { super(message); this.status = status; }
}

let refreshPromise: Promise<boolean> | null = null;
async function refreshSession(): Promise<boolean> {
  if (!getCsrfToken()) return false;
  refreshPromise ??= fetch(`${BASE_URL}/Auth/refresh`, {
    method: 'POST', credentials: 'include', headers: { 'X-CSRF-Token': getCsrfToken() },
  }).then(response => response.ok).finally(() => { refreshPromise = null; });
  return refreshPromise;
}

async function request(path: string, options: RequestInit = {}): Promise<Response> {
  const send = () => {
    const headers = new Headers(options.headers);
    if (options.body && !(options.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');
    if (!['GET', 'HEAD', 'OPTIONS'].includes((options.method || 'GET').toUpperCase()) && getCsrfToken()) headers.set('X-CSRF-Token', getCsrfToken());
    return fetch(`${BASE_URL}${path}`, { ...options, credentials: 'include', headers });
  };
  let response = await send();
  const canRefresh = !new Headers(options.headers).has('x-api-key') && (!path.startsWith('/Auth/') || path === '/Auth/me');
  if (response.status === 401 && canRefresh && await refreshSession()) response = await send();
  if (!response.ok) {
    const body = await response.text();
    let message = body || `Request failed with status ${response.status}`;
    try { const error = JSON.parse(body); message = error.detail || error.title || error.message || message; }
    catch { /* use the text response */ }
    throw new ApiError(message, response.status);
  }
  return response;
}

export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await request(path, options);
  return response.headers.get('content-type')?.includes('json') ? await response.json() as T : null as T;
}

export async function apiFetchWithStatus(path: string, options: RequestInit = {}): Promise<{ status: number; data: unknown }> {
  const response = await request(path, options);
  const data = response.headers.get('content-type')?.includes('json') ? await response.json() : null;
  return { status: response.status, data };
}

export async function streamChat(
  chatBotId: number, query: string, apiKey: string | null,
  conversationId?: string | null,
  onEvent: (event: ChatStreamEvent) => void = () => {},
  signal?: AbortSignal,
): Promise<void> {
  const path = apiKey ? '/SemanticSearch/stream' : `/ChatBots/${chatBotId}/stream`;
  const response = await request(path, {
    method: 'POST', signal, headers: apiKey ? { 'x-api-key': apiKey } : undefined,
    body: JSON.stringify({ chatBotId, query, conversationId: conversationId || undefined }),
  });
  const reader = response.body?.getReader();
  if (!reader) throw new Error('No response body');
  const decoder = new TextDecoder();
  let buffer = '';
  let completed = false;
  const parseLine = (line: string) => {
    if (!line.startsWith('data:')) return;
    const event = JSON.parse(line.slice(5).trim()) as ChatStreamEvent;
    if (event.type === 'error') throw new Error(event.text || 'Chat generation failed.');
    if (event.type === 'completed') completed = true;
    onEvent(event);
  };
  try {
    while (true) {
      const { done, value } = await reader.read();
      buffer += done ? decoder.decode() : decoder.decode(value, { stream: true });
      const lines = buffer.split(/\r?\n/);
      buffer = lines.pop() || '';
      for (const line of lines) parseLine(line);
      if (done) { if (buffer) parseLine(buffer); break; }
    }
    if (!completed) throw new Error('The chat connection ended before the answer completed.');
  } finally { await reader.cancel().catch(() => {}); reader.releaseLock(); }
}

export interface ChatStreamEvent {
  type: string;
  text?: string | null;
  conversationId?: string | null;
}
