import { useState, useEffect, useRef } from 'react';
import { apiKey as apiKeyApi } from '../services';
import type { ApiChatStreamEvent } from '../services';
import { semanticSearch } from '../services';
import type { ChatbotAccessDto } from '../types';

interface KeyChatMessage {
  role: 'user' | 'bot';
  text: string;
}

export function KeyChatDialog({
  open,
  onClose,
  standalone = false,
}: {
  open: boolean;
  onClose: () => void;
  standalone?: boolean;
}) {
  const [pastedKey, setPastedKey] = useState('');
  const [connected, setConnected] = useState(false);
  const [chatbotInfo, setChatbotInfo] = useState<ChatbotAccessDto | null>(null);
  const [messages, setMessages] = useState<KeyChatMessage[]>([]);
  const [question, setQuestion] = useState('');
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [sending, setSending] = useState(false);
  const requestRef = useRef<AbortController | null>(null);
  const [status, setStatus] = useState('Paste a key to get started.');
  const dialogRef = useRef<HTMLDialogElement | null>(null);

  useEffect(() => {
    if (!open) return;
    const dialog = dialogRef.current;
    if (dialog && typeof dialog.showModal === 'function') {
      if (!dialog.open) dialog.showModal();
    }
    return () => { requestRef.current?.abort(); };
  }, [open]);

  const handleConnect = async () => {
    if (!pastedKey.trim()) return setStatus('Paste an API key first.');
    try {
      const info = await apiKeyApi.resolve(pastedKey.trim());
      setChatbotInfo(info);
      setConnected(true);
      setStatus('Connected to ' + (info.name || 'chatbot'));
      setMessages([]);
      setConversationId(null);
    } catch (err: any) {
      setStatus(err.message);
      setConnected(false);
    }
  };

  const handleDisconnect = () => {
    requestRef.current?.abort();
    requestRef.current = null;
    setSending(false);
    setConversationId(null);
    setConnected(false);
    setPastedKey('');
    setChatbotInfo(null);
    setMessages([]);
    setStatus('Paste a key to get started.');
  };

  const handleSend = async () => {
    if (!question.trim() || !connected || !pastedKey || !chatbotInfo || requestRef.current) return;
    const controller = new AbortController();
    requestRef.current = controller;
    setSending(true);

    const q = question.trim();
    setMessages(prev => [...prev, { role: 'user', text: q }]);
    setQuestion('');

    let fullText = '';
    setMessages(prev => [...prev, { role: 'bot', text: '' }]);

    try {
      await semanticSearch.stream(
        chatbotInfo.chatBotId,
        q,
        pastedKey.trim(),
        (event: ApiChatStreamEvent) => {
          if (controller.signal.aborted) return;
          if (event.type === 'token' || event.type === 'chunk') {
            fullText += event.text || '';
            setMessages(prev => {
              const last = prev[prev.length - 1];
              if (last?.role === 'bot') {
                return [...prev.slice(0, -1), { role: 'bot', text: fullText }];
              }
              return [...prev, { role: 'bot', text: fullText }];
            });
          } else if (event.type === 'completed') {
            if (event.conversationId) setConversationId(event.conversationId);
          } else if (event.type === 'error') {
            setMessages(prev => [...prev.slice(0, -1), { role: 'bot', text: event.text || 'Error' }]);
          }
        },
        conversationId,
        controller.signal,
      );
    } catch (err: any) {
      if (!controller.signal.aborted) setMessages(prev => [...prev.slice(0, -1), { role: 'bot', text: err.message }]);
    } finally {
      if (requestRef.current === controller) { requestRef.current = null; setSending(false); }
    }
  };

  if (!open) return null;

  if (standalone) {
    return (
      <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100 }}>
        <div style={{ background: 'var(--panel)', border: '1px solid var(--line)', borderRadius: 16, padding: 24, width: 'min(620px, calc(100% - 32px))', maxHeight: '90vh', overflow: 'auto', color: 'var(--ink)' }}>
          <div className="row" style={{ justifyContent: 'space-between' }}>
            <h3 style={{ margin: 0 }}>Chat with an API key</h3>
            <button type="button" className="ghost" onClick={onClose}>Close</button>
          </div>
          <p className="sub">Paste the key shared by the chatbot creator. No sign-in is needed to chat with a key.</p>
          {!connected ? (
            <>
              <form style={{ marginTop: 16 }} onSubmit={e => { e.preventDefault(); handleConnect(); }}>
                <div className="f">
                  <label htmlFor="pastedKey">Chatbot API key</label>
                  <input
                    type="password"
                    id="pastedKey"
                    placeholder="Paste your chatbot API key"
                    value={pastedKey}
                    onChange={e => setPastedKey(e.target.value)}
                    autoComplete="off"
                    required
                  />
                </div>
                <button className="btn" type="submit">Connect chatbot</button>
              </form>
              <p id="keyChatStatus" className="sub" role="status" style={{ marginTop: 12 }}>{status}</p>
            </>
          ) : (
            <>
              <div className="msgs" id="keyChatMessages" role="log" aria-live="polite"
                style={{ height: 280, border: '1px solid var(--line)', borderRadius: 12, margin: '16px 0', overflow: 'auto', padding: 16 }}>
                {messages.length === 0 && <div className="empty">Your conversation will appear here.</div>}
                {messages.map((m, i) => (
                  <div key={i} className={`m ${m.role === 'user' ? 'u' : 'b'}`}>{m.text || ' '}</div>
                ))}
              </div>
              <form className="composer" id="keyQuestionForm" onSubmit={e => { e.preventDefault(); handleSend(); }}>
                <input
                  id="keyQuestion"
                  placeholder="Ask this chatbot a question"
                  value={question}
                  onChange={e => setQuestion(e.target.value)}
                  autoComplete="off"
                  disabled={!connected || sending}
                  required
                />
                <button className="btn" id="keySend" type="submit" disabled={!connected || sending}>Send</button>
              </form>
              <button type="button" className="ghost" style={{ marginTop: 12 }} onClick={handleDisconnect}>Disconnect key</button>
            </>
          )}
        </div>
      </div>
    );
  }

  return (
    <dialog ref={dialogRef} onCancel={onClose} aria-labelledby="keyChatTitle"
      style={{ width: 'min(620px,calc(100% - 32px))', maxHeight: '90vh', overflow: 'auto', background: 'var(--panel)', color: 'var(--ink)', border: '1px solid var(--line)', borderRadius: 16, padding: 24 }}>
      <div className="row" style={{ justifyContent: 'space-between' }}>
        <h3 id="keyChatTitle" style={{ margin: 0 }}>Chat with an API key</h3>
        <button type="button" className="ghost" onClick={() => { dialogRef.current?.close(); onClose(); }}>Close</button>
      </div>
      <p className="sub">Paste the key shared by the chatbot creator. No sign-in is needed to chat with a key.</p>
      {!connected ? (
        <>
          <form style={{ marginTop: 16 }} onSubmit={e => { e.preventDefault(); handleConnect(); }}>
            <div className="f">
              <label htmlFor="pastedKey">Chatbot API key</label>
              <input
                type="password"
                id="pastedKey"
                placeholder="Paste your chatbot API key"
                value={pastedKey}
                onChange={e => setPastedKey(e.target.value)}
                autoComplete="off"
                required
              />
            </div>
            <button className="btn" type="submit">Connect chatbot</button>
          </form>
          <p id="keyChatStatus" className="sub" role="status" style={{ marginTop: 12 }}>{status}</p>
        </>
      ) : (
        <>
          <div className="msgs" id="keyChatMessages" role="log" aria-live="polite"
            style={{ height: 280, border: '1px solid var(--line)', borderRadius: 12, margin: '16px 0', overflow: 'auto', padding: 16 }}>
            {messages.length === 0 && <div className="empty">Your conversation will appear here.</div>}
            {messages.map((m, i) => (
              <div key={i} className={`m ${m.role === 'user' ? 'u' : 'b'}`}>{m.text || ' '}</div>
            ))}
          </div>
          <form className="composer" id="keyQuestionForm" onSubmit={e => { e.preventDefault(); handleSend(); }}>
            <input
              id="keyQuestion"
              placeholder="Ask this chatbot a question"
              value={question}
              onChange={e => setQuestion(e.target.value)}
              autoComplete="off"
              disabled={!connected || sending}
              required
            />
            <button className="btn" id="keySend" type="submit" disabled={!connected || sending}>Send</button>
          </form>
        </>
      )}
    </dialog>
  );
}
