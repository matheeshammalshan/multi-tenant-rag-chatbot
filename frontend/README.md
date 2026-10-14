# RAG Console frontend

Run the backend using `../RUNNING.md`, then start the frontend:

```powershell
npm.cmd install
npm.cmd run dev
```

Open http://localhost:5173. The development server proxies API routes to http://localhost:5270. Set `VITE_API_URL` in `.env.local` to override that backend target.

```powershell
npm.cmd run build
npm.cmd run lint
npm.cmd test
```

Production builds use the current origin by default. Configure your hosting reverse proxy to forward the API routes to the backend, or set `VITE_API_URL` before building. Browser management requests require credentials, an allowed frontend origin, and cookies usable by the frontend for the CSRF header. Same-origin hosting supports this directly.

The playground uses the signed-in session and never generates or rotates shared API keys. Generate shared keys on the API access page; the full key is shown only after generation. API-key guest chat uses `/SemanticSearch/stream` and retains its conversation ID.

Document processing runs in the background. Reopen the knowledge-base page to read the current status; there are no live notifications. Password recovery is not exposed in the UI.

The API tests use mocked HTTP responses to check refresh coordination, multipart headers, guest versus playground authentication, streaming completion, and error handling.
