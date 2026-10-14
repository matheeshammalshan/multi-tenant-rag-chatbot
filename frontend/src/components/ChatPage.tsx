import { useEffect, useState, useRef } from 'react';
import { useApp } from '../contexts/useApp';
import { useToast } from '../contexts/useToast';
import { llmConfig, semanticSearch, type ApiChatStreamEvent } from '../services';
import type { LlmConfigDto, ModelDto } from '../types';

interface Message {
  role: 'user' | 'bot';
  text: string;
}

export function ChatPage() {
  const { currentBot, models: modelList } = useApp();
  const { showToast } = useToast();
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [thinking, setThinking] = useState(false);
  const [botConfig, setBotConfig] = useState<LlmConfigDto | null>(null);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const msgsRef = useRef<HTMLDivElement>(null);
  const requestRef = useRef<AbortController | null>(null);
  const botId = currentBot?.id;

  useEffect(() => {
    let active = true;
    if (botId) {
      llmConfig.get(botId).then(config => { if (active) setBotConfig(config); })
        .catch((error: Error) => { if (active) showToast(error.message, 'error'); });
    }
    return () => { active = false; requestRef.current?.abort(); };
  }, [botId, showToast]);

  useEffect(() => {
    const el = msgsRef.current;
    if (el) el.scrollTop = el.scrollHeight;
  }, [messages, thinking]);

  const handleSend = async () => {
    const q = input.trim();
    if (!q || !currentBot || !botConfig || thinking || requestRef.current) return;
    const controller = new AbortController();
    requestRef.current = controller;
    setInput('');
    setThinking(true);
    setMessages(prev => [...prev, { role: 'user', text: q }, { role: 'bot', text: '' }]);
    let fullText = '';
    try {
      await semanticSearch.stream(currentBot.id, q, null, (event: ApiChatStreamEvent) => {
        if (controller.signal.aborted) return;
        if (event.type === 'token') {
          fullText += event.text || '';
          setMessages(prev => [...prev.slice(0, -1), { role: 'bot', text: fullText }]);
        } else if (event.type === 'completed' && event.conversationId) {
          setConversationId(event.conversationId);
        }
      }, conversationId, controller.signal);
    } catch (error) {
      if (!controller.signal.aborted) {
        const message = error instanceof Error ? error.message : 'Chat failed.';
        setMessages(prev => [...prev.slice(0, -1), { role: 'bot', text: fullText ? fullText + '\n' + message : message }]);
        showToast(message, 'error');
      }
    } finally {
      if (requestRef.current === controller) {
        requestRef.current = null;
        if (!controller.signal.aborted) setThinking(false);
      }
    }
  };

  const handleKey = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      void handleSend();
    }
  };

  if (!currentBot) {
    return (
      <section className="page on" id="chat">
        <div className="head"><h1>Playground</h1><p className="sub">Try a chatbot exactly as your users would.</p></div>
        <div className="empty"><b>Select a chatbot</b><br />Choose a chatbot from the sidebar or create one.</div>
      </section>
    );
  }

  const mdl: ModelDto | undefined = modelList.find(m => m.id === botConfig?.modelName);

  return (
    <section className="page on" id="chat">
      <div className="head">
        <div>
          <h1>Playground</h1>
          <p className="sub">Try <b>{currentBot.name}</b> exactly as your users would.</p>
        </div>
      </div>
      <div className="chatwrap">
        <div className="card chat">
          <div className="chat-h">
            <div>
              <b id="chTitle">{currentBot.name}</b>
              <div className="sub" id="chSub">{currentBot.description}</div>
            </div>
            <span className="route">Playground chat</span>
          </div>
          <div className="msgs" id="msgs" ref={msgsRef}>
            {messages.length === 0 && (
              <div className="empty">
                <b>Say hello</b><br />
                Ask something your uploaded documents can answer.
              </div>
            )}
            {messages.map((m, i) => (
              <div key={i} className={`m ${m.role === 'user' ? 'u' : 'b'}`}>{m.text || ' '}</div>
            ))}
            {thinking && <div className="m b" style={{ opacity: 0.5 }}>...</div>}
          </div>
          <form className="composer" id="cForm" onSubmit={e => { e.preventDefault(); void handleSend(); }}>
            <input
              id="cIn"
              placeholder="Ask a question about your documents..."
              value={input}
              onChange={e => setInput(e.target.value)}
              onKeyDown={handleKey}
              autoComplete="off"
              disabled={thinking}
            />
            <button className="btn" type="submit" disabled={!input.trim() || thinking}>
              {thinking ? 'Sending...' : 'Send'}
            </button>
          </form>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div className="card pad">
            <h3>Active setup</h3>
            <p className="hint">Applied to every answer.</p>
            <div className="mini" style={{ flexDirection: 'column', gap: 6 }}>
              {mdl && <span>Model <b>{mdl.name}</b></span>}
              {botConfig && (<><span>Temperature <b>{botConfig.temperature.toFixed(2)}</b></span><span>Max tokens <b>{botConfig.maxToken}</b></span></>)}
              <span>Documents <b>{currentBot.documentCount || 0}</b> ready</span>
              {botConfig?.systemPrompt && <span style={{ marginTop: 6 }}>System prompt<br /><i>{botConfig.systemPrompt}</i></span>}
            </div>
          </div>
          <div className="card pad">
            <h3>Access</h3>
            <p className="hint">Shared API keys are managed on the API access page.</p>
            <div className="keybox">
              <span id="chKey">Uses your signed-in session.</span>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
