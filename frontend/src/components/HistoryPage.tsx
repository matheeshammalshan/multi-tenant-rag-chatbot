import { useEffect, useState } from 'react';
import { useApp } from '../contexts/useApp';
import { useToast } from '../contexts/useToast';
import { chatHistory } from '../services';
import type { ChatHistoryResponseDto } from '../types';

export function HistoryPage() {
  const { currentBot } = useApp();
  const { showToast } = useToast();
  const [hist, setHist] = useState<ChatHistoryResponseDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!currentBot) return;
    let active = true;
    chatHistory
      .list(currentBot.id)
      .then(data => { if (active) setHist(data); })
      .catch(err => { if (active) showToast(err.message, 'error'); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [currentBot, showToast]);


  if (!currentBot) {
    return (
      <section className="page on" id="history">
        <div className="head"><h1>Chat history</h1><p className="sub">Select a chatbot to view its conversations.</p></div>
        <div className="empty"><b>No chatbot selected</b><br />Choose a chatbot from the sidebar.</div>
      </section>
    );
  }

  return (
    <section className="page on" id="history">
      <div className="head">
        <div>
          <h1>Chat history</h1>
          <p className="sub">Every question and answer for <b>{currentBot.name}</b>.</p>
        </div>
      </div>
      <div className="hist" id="hisList" style={{ maxWidth: 820 }}>
        {loading && <div className="card pad sub">Loading...</div>}
        {!loading && hist.length === 0 && (
          <div className="card pad sub">No conversations yet for this chatbot.</div>
        )}
        {!loading && hist.slice().reverse().map((h, i) => (
          <div key={i} className="hi">
            <small>User</small><p>{(h.userPromt)}</p>
            <small>Chatbot</small><p>{(h.systemMessage)}</p>
          </div>
        ))}
      </div>
    </section>
  );
}
