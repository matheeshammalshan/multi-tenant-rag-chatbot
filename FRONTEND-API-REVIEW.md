# Proposed frontend: API gap review

Reviewed `../rag-chatbot-frontend (1).html` against the current chatbot C# source. This is a static review; no business requests or database writes were made. Endpoint suggestions below are proposed additions, not existing routes.

## Frontend status

The supplied HTML is a design prototype. Its script explicitly labels the data as mock data (line 380). Authentication, account registration, chatbot creation, document upload, statistics, settings, and chat all manipulate in-memory JavaScript objects. The `fetch()` shown on the API access page is example text rendered into the page, not a working integration. The live status indicator does not open a WebSocket connection. Backend changes alone will not connect this frontend; its mock handlers must also be replaced with real HTTP/WebSocket calls.

## Existing API capabilities

| Existing route | Current behavior |
| --- | --- |
| POST /Organization/create | Creates an organization; returns empty HTTP 200 |
| POST /ChatBot/create | Creates a chatbot and default LLM configuration; returns empty HTTP 200 |
| POST /Document/create | Accepts a multipart upload and processes it within the request; returns no document DTO |
| PUT /LlmConfig/update | Updates saved LLM configuration; returns empty HTTP 200 |
| GET /ChatHistory/{chatBotId} | Returns all question/answer pairs for the chatbot |
| GET /Api/create-apikey | Accepts a JSON body and replaces the chatbot key |
| POST /SemanticSearch/askQuery | Checks the supplied key against the supplied chatbot ID, retrieves context, generates an answer, and writes history |
| /ws/chat | Accepts sockets and broadcasts document notifications to every connected socket |

## Required additions for the visible frontend

### 1. Authentication, users, and ownership — highest priority

The frontend includes register, login, remember-me, forgot-password, logout, user/owner roles, and an owner-only users/organizations page. None of these have backend implementations. The Domain has no user/account entity, organization membership, password data, or chatbot creator relationship. There are no authentication handlers or endpoint authorization policies. `UseAuthorization()` alone does not implement login or ownership enforcement.

Suggested contracts:

- POST /Auth/register — atomically create the account and its organization; ordinary registration must not assign the platform owner role.
- POST /Auth/login, POST /Auth/logout, GET /Auth/me.
- Persistent session/refresh behavior for keep-me-signed-in, appropriate to the selected authentication mechanism.
- POST /Auth/forgot-password, POST /Auth/reset-password, with actual email delivery.
- GET /Users — platform-owner only, with organization/contact/registration data.

The frontend treats ordinary users as owners of their own chatbots (`b.owner == user.id`); the platform owner can inspect all organizations. Model this explicitly. Derive user identity from authentication and check organization/chatbot ownership for each resource request. Do not trust user IDs, role selections, or organization IDs supplied by the browser. Ordinary account registration and API-key-only guest chat are distinct authentication flows.

Current organization/chatbot creation, document upload, configuration changes, history retrieval, and key rotation are not protected by management authentication or ownership checks. Fix this before exposing the console to multiple tenants. Qdrant's chatbot filter protects retrieval scope only after the selected chatbot is authorized; it does not enforce console ownership.

### 2. Organization and chatbot reads

The sidebar, chatbot cards, and selectors cannot reload persisted data with the current API. Add:

- GET /Organizations — scoped to the current user, or all for the platform owner.
- GET /ChatBots?organizationId=... — authorized list with IDs, descriptions, configuration summary, and counts.
- GET /ChatBots/{id} — details and the saved system prompt.

Creation should return HTTP 201 and a DTO with the server-assigned ID. The frontend currently invents IDs locally and automatically creates a chatbot key; the API does neither in its create response. Either generate the key during creation or have the frontend request it separately after receiving the chatbot ID.

Update/delete chatbot and organization endpoints are useful for a complete management API, but are not required by a visible edit/delete control in this HTML.

### 3. Documents and upload tracking

The knowledge-base table needs GET /Documents?chatBotId=... with ID, filename, upload date, numeric size, reader type, status, and chunk count. The SQL model does not currently persist reader type. A status/details endpoint also enables recovery after WebSocket reconnects.

The upload response should return the document ID and processing status. Derive upload size, date, and initial status on the server rather than accepting them as authoritative client metadata. Validate file type, size, reader selection, and chatbot access. Use a generated safe storage filename; the current FileService uses the submitted filename directly in its path.

Current ingestion performs storage, embedding calls, and Qdrant writes while an SQL transaction stays open. Consider a durable processing job with an accepted upload response for larger documents. Persist Processing/Ready/Failed states and publish a failure event. Existing code only publishes Processing/Proceed, sends success before transaction commit, and emits no Failed event after rollback.

Document download/delete/reprocessing are potential later features; this HTML does not currently expose those controls.

### 4. Model settings

Add GET /LlmConfig/{chatBotId} so selectors, sliders, and the active setup panel can load saved configuration. Return the saved configuration from updates. A GET /Models catalog would prevent the frontend's hard-coded model list drifting from backend support.

Concrete existing mismatches:

- The HTML's JSON preview sends model names as strings such as `GptOss20B`. The API currently has no JsonStringEnumConverter for controller JSON; define string enum serialization or send numeric enum values consistently.
- `Infrastructure/LlmModels/GptOss20B.cs` selects `openai/gpt-oss-120b`, so choosing 20B actually invokes 120B.
- `GptOss120B.cs` hard-codes temperature 1, maximum tokens 800, and top-p 1, ignoring the saved sliders.
- The stored chatbot `SystemPromt` is not passed to the model calls. The providers use a fixed system prompt, and place the actual question in a system-role message. Pass the chatbot prompt as system instructions, retrieved context in an appropriate context message, and the question as a user message.
- API-created bots default to KimiK2 with max tokens 45, whereas the prototype creates GPT-OSS 20B bots with 1024 tokens. Agree on one set of defaults.

### 5. Overview and analytics

The four overview counters and per-chatbot breakdown need an authorized GET /Analysis?organizationId=... or GET /Dashboard?organizationId=... endpoint. There is an AnalysisEntity but no analysis read service/controller. Include chatbot count, total queries, document count, processing count, and indexed chunks.

`SemanticSearchService` saves history but does not increment TotalQueries. The current analysis observer only updates document/chunk counters. Define when a query counts and keep concurrent updates consistent. The observer also persists analysis through a separate SQL context before the document transaction commits, so counters and ingestion state can diverge on failures.

### 6. API-key-only guest chat

The dialog accepts only a key, looks up the matching mock bot, and displays its name. The API requires both ChatBotId and key. Add a key-authenticated chatbot metadata endpoint such as GET /ChatbotAccess that resolves the bot from `x-api-key`, returning only public chatbot metadata. Alternatively, make the guest chat endpoint resolve chatbot scope directly from the key.

Key generation is currently GET with a JSON body. Change this to an authenticated POST, for example POST /ChatBots/{id}/api-key, since browser fetch does not support GET request bodies and key rotation changes server state. Enforce ownership and return a structured result. Decide whether the UI can redisplay stored raw keys or should show a prefix and only reveal a new key once. Never return every chatbot's key in ordinary list responses.

### 7. Chat response and history

The HTML's integration example uses `response.json()`, but the chat action currently returns a string, which defaults to text/plain. Choose a JSON response DTO such as `{ answer, chatBotId, conversationId }`, or change the frontend to consume plain text. Invalid chatbot keys currently return error text as a successful string response; use HTTP 401/403/404 and a consistent error body.

The history screen can show the existing question/answer pairs after field mapping, but history is currently readable without user/tenant authorization. The frontend's displayed conversation combines all entries for a bot. Add conversation/session IDs before implementing private independent guest conversations or multi-turn context. Timestamps, pagination, and stable message IDs would support ordering and growing histories; the current history DTO has only UserPromt and SystemMessage.

The mock frontend animates progress and words. Providers read upstream streaming responses but the API accumulates the answer and returns a single string. If real token/progress updates are required, add an SSE or scoped WebSocket streaming contract. This is optional if the frontend displays the completed response and simulates animation.

### 8. Live notifications and browser integration

The WebSocket event contains only Message, Status, and Chunks. Add documentId, chatBotId, event type, and error details so the frontend can update the right row. HTTP JSON normally uses camelCase, but current WebSocket serialization uses default PascalCase; standardize the contract.

Authenticate sockets and restrict subscriptions to authorized organizations/chatbots. Current events broadcast to every connected client. For disconnected clients, provide a status/list endpoint for resynchronization.

There is no CORS policy. A frontend served from a different localhost port or another website needs an explicit allowed-origin policy permitting the appropriate HTTP methods and `Content-Type`, `x-api-key`, and any management authentication headers. Same-origin hosting does not require CORS. Serve the frontend through a development server rather than opening it as a local file.

## Suggested implementation order

1. User/accounts, organization membership, chatbot ownership, authentication, and authorization policies.
2. List/details APIs for users, organizations, chatbots, documents, settings, and analysis; return creation DTOs.
3. Fix enum/response contracts, key generation and key-only guest lookup, and provider behavior.
4. Durable upload status, scoped WebSocket events, analytics consistency, and optional chat streaming.
5. Connect frontend handlers to the backend and verify ordinary user, platform owner, and guest key flows with separate tenant data.

This review does not change backend or frontend behavior.
