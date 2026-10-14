import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import ts from 'typescript';

async function loadApi() {
  globalThis.document = { cookie: 'csrf_token=initial' };
  const source = (await readFile(new URL('../src/api.ts', import.meta.url), 'utf8'))
    .replace('import.meta.env.DEV', 'false').replace('import.meta.env.VITE_API_URL', "''");
  const code = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ES2022 } }).outputText;
  return import(`data:text/javascript;base64,${Buffer.from(code + '\n//' + Math.random()).toString('base64')}`);
}

const json = (body, status = 200) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
function stream(parts) {
  return new Response(new ReadableStream({ start(controller) {
    for (const part of parts) controller.enqueue(new TextEncoder().encode(part));
    controller.close();
  } }), { headers: { 'Content-Type': 'text/event-stream' } });
}

test('expired concurrent requests share one refresh and retry with the rotated CSRF token', async () => {
  const api = await loadApi();
  let refreshes = 0;
  let sends = 0;
  globalThis.fetch = async (path, options) => {
    if (path === '/Auth/refresh') {
      refreshes++;
      await new Promise(resolve => setTimeout(resolve, 10));
      document.cookie = 'csrf_token=rotated';
      return json({});
    }
    sends++;
    assert.equal(options.credentials, 'include');
    if (options.headers.get('X-CSRF-Token') === 'initial') return json({}, 401);
    assert.equal(options.headers.get('X-CSRF-Token'), 'rotated');
    return json({ saved: true });
  };
  const results = await Promise.all([api.apiFetch('/Users', { method: 'PUT', body: '{}' }), api.apiFetch('/ChatBot', { method: 'POST', body: '{}' })]);
  assert.deepEqual(results, [{ saved: true }, { saved: true }]);
  assert.equal(refreshes, 1);
  assert.equal(sends, 4);
});

test('failed mutations reject rather than returning a successful status wrapper', async () => {
  const api = await loadApi();
  globalThis.fetch = async () => json({ detail: 'Forbidden' }, 403);
  await assert.rejects(api.apiFetchWithStatus('/Users/x/status', { method: 'PUT', body: 'true' }), error => error.status === 403 && error.message === 'Forbidden');
});

test('headers accept Headers and multipart uploads keep their browser-generated boundary', async () => {
  const api = await loadApi();
  const form = new FormData(); form.append('File', new Blob(['document']), 'doc.txt');
  globalThis.fetch = async (_path, options) => {
    assert.equal(options.body, form);
    assert.equal(options.headers.has('Content-Type'), false);
    assert.equal(options.headers.get('Custom'), 'value');
    return json({ id: 7 });
  };
  assert.deepEqual(await api.apiFetch('/Document/create', { method: 'POST', body: form, headers: new Headers({ Custom: 'value' }) }), { id: 7 });
});

test('playground uses session authentication and parses fragmented SSE completion', async () => {
  const api = await loadApi();
  const events = [];
  globalThis.fetch = async (path, options) => {
    assert.equal(path, '/ChatBots/7/stream');
    assert.equal(options.headers.has('x-api-key'), false);
    assert.equal(options.headers.get('X-CSRF-Token'), 'initial');
    assert.equal(JSON.parse(options.body).conversationId, 'conversation');
    return stream(['data: {"type":"tok', 'en","text":"hello"}\r\n\r\n', 'data: {"type":"completed","conversationId":"conversation"}']);
  };
  await api.streamChat(7, 'question', null, 'conversation', event => events.push(event));
  assert.deepEqual(events.map(event => event.type), ['token', 'completed']);
});

test('invalid guest keys do not trigger a management-session refresh', async () => {
  const api = await loadApi();
  let calls = 0;
  globalThis.fetch = async (path) => { calls++; assert.equal(path, '/SemanticSearch/stream'); return json({ detail: 'Invalid key' }, 401); };
  await assert.rejects(api.streamChat(7, 'question', 'bad-key'), /Invalid key/);
  assert.equal(calls, 1);
});

test('stream errors and premature disconnects are surfaced', async () => {
  const api = await loadApi();
  globalThis.fetch = async () => stream(['data: {"type":"error","text":"Provider unavailable"}\n\n']);
  await assert.rejects(api.streamChat(7, 'question', 'key'), /Provider unavailable/);
  globalThis.fetch = async () => stream(['data: {"type":"token","text":"partial"}\n\n']);
  await assert.rejects(api.streamChat(7, 'question', 'key'), /ended before/);
});

test('stream callback failures propagate to the caller', async () => {
  const api = await loadApi();
  globalThis.fetch = async () => stream(['data: {"type":"token","text":"text"}\n\n']);
  await assert.rejects(api.streamChat(7, 'question', 'key', null, () => { throw new Error('callback failed'); }), /callback failed/);
});
