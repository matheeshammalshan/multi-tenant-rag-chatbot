import { useState } from 'react';
import { useApp } from '../contexts/useApp';
import { useToast } from '../contexts/useToast';
import type { ChatBotCreateDto } from '../types';
import { chatbots as chatbotApi } from '../services';

export function ChatbotsPage() {
  const { chatbots, currentBotId, setCurrentBotId, models, refetch, currentOrgId } = useApp();
  const { showToast } = useToast();
  const [name, setName] = useState('');
  const [desc, setDesc] = useState('');
  const [sys, setSys] = useState('');
  const [submitting, setSubmitting] = useState(false);


  const handleCreate = async () => {
    if (!name.trim() || submitting) return;
    const orgId = currentOrgId;
    if (!orgId) return showToast('Select an organization first.');
    setSubmitting(true);
    try {
      await chatbotApi.create({ organizationId: orgId, name: name.trim(), description: desc || null, systemPrompt: sys || null } as ChatBotCreateDto);
      setName(''); setDesc(''); setSys('');
      await refetch();
      showToast('Chatbot created. Generate a shared key on the API access page.');
    } catch (err: any) {
      showToast(err.message, 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const handleBotClick = (id: number) => {
    setCurrentBotId(id);
  };

  return (
    <section className="page on" id="chatbots">
      <div className="head">
        <div>
          <h1>Chatbots</h1>
          <p className="sub">Each chatbot has its own knowledge, model settings and API key.</p>
        </div>
      </div>
      <div className="grid g21" style={{ alignItems: 'start' }}>
        <div className="grid g2" id="botGrid">
          {chatbots.map(b => {
            const m = models.find(x => x.id === b.configuration?.modelName);
            return (
              <div key={b.id} className={`card bot ${b.id === currentBotId ? 'sel' : ''}`}
                onClick={() => handleBotClick(b.id)}>
                <div className="t">
                  <b>{(b.name)}</b>
                  <span className="chip ok">Live</span>
                </div>
                <p>{(b.description)}</p>
                <div className="row">
                  <span className="chip">{m?.name || b.configuration?.modelName || 'No model'}</span>
                  <span className="chip mono">#{b.id}</span>
                </div>
                <div className="mini">
                  <span><b>{b.totalQueries}</b> queries</span>
                  <span><b>{b.documentCount}</b> docs</span>
                  <span><b>{b.totalChunks}</b> chunks</span>
                </div>
              </div>
            );
          })}
          {chatbots.length === 0 && (
            <div className="card pad sub" style={{ gridColumn: '1/-1' }}>No chatbots in this organization yet. Create the first one.</div>
          )}
        </div>
        {currentOrgId && (
          <div className="card pad">
            <h3>New chatbot</h3>
            <p className="hint"><span className="route">POST /ChatBot/create</span></p>
            <div className="f"><label>Name</label><input type="text" id="cbName" maxLength={255}
              value={name} onChange={e => setName(e.target.value)} placeholder="e.g. HR Policy Assistant" /></div>
            <div className="f"><label>Description</label><input type="text" id="cbDesc" maxLength={255}
              value={desc} onChange={e => setDesc(e.target.value)} placeholder="What is it for?" /></div>
            <div className="f">
              <label className="r"><span>System prompt</span><span id="spCount" style={{ fontSize: '11.5px' }}>{sys.length}/255</span></label>
              <textarea id="cbSys" maxLength={255} value={sys} onChange={e => setSys(e.target.value)}
                placeholder="You are a helpful assistant. Answer only from the provided documents." />
            </div>
            <button className="btn" id="cbCreate" onClick={handleCreate} disabled={submitting || !name.trim()}>
              {submitting ? 'Creating...' : 'Create chatbot'}
            </button>
          </div>
        )}
      </div>
    </section>
  );
}
