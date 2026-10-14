import { useEffect, useState } from 'react';
import { useApp } from '../contexts/useApp';
import { useToast } from '../contexts/useToast';
import { llmConfig } from '../services';
import type { LlmConfigDto, LlmModel } from '../types';

export function ModelSettingsPage() {
  const { currentBot, models } = useApp();
  const { showToast } = useToast();
  const [config, setConfig] = useState<LlmConfigDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [model, setModel] = useState<LlmModel>('GptOss20B');
  const [temp, setTemp] = useState(0.5);
  const [topP, setTopP] = useState(0.9);
  const [maxTokens, setMaxTokens] = useState(1024);

  const botId = currentBot?.id;
  useEffect(() => {
    let active = true;
    if (!botId) return;
    llmConfig.get(botId).then(data => {
      if (!active) return;
      setConfig(data); setModel(data.modelName); setTemp(data.temperature);
      setTopP(data.topP); setMaxTokens(data.maxToken); setLoading(false);
    }).catch((error: Error) => { if (active) { showToast(error.message, 'error'); setLoading(false); } });
    return () => { active = false; };
  }, [botId, showToast]);

  const handleSave = async () => {
    if (!config || config.chatBotId !== botId || saving) return;
    setSaving(true);
    try {
      const saved = await llmConfig.update({ ...config, modelName: model, temperature: temp, topP, maxToken: maxTokens });
      setConfig(saved);
      setModel(saved.modelName);
      setTemp(saved.temperature);
      setTopP(saved.topP);
      setMaxTokens(saved.maxToken);
      showToast('Model settings saved.');
    } catch (err: any) {
      showToast(err.message, 'error');
    } finally {
      setSaving(false);
    }
  };

  if (loading || !currentBot || config?.chatBotId !== botId) {
    return (
      <section className="page on" id="llm">
        <div className="head"><h1>Model settings</h1><p className="sub">Select a chatbot to configure its model.</p></div>
        {!currentBot && <p className="sub">Create a chatbot first.</p>}
      </section>
    );
  }

  if (!config) return null;

  const preview = {
    chatBotId: config.chatBotId,
    modelName: model,
    temperature: temp,
    maxToken: maxTokens,
    topP,
  };

  return (
    <section className="page on" id="llm">
      <div className="head">
        <div>
          <h1>Model settings</h1>
          <p className="sub">Choose how this chatbot thinks and how long it may answer.</p>
        </div>
        <div className="pick">
          <span className="sub">Chatbot</span>
          <span className="route">#{config.chatBotId}</span>
        </div>
      </div>
      <div className="grid g2" style={{ alignItems: 'start' }}>
        <div className="card pad">
          <h3>Model</h3>
          <p className="hint"><span className="route">PUT /LlmConfig/update</span></p>
          <div className="models" id="models">
            {models.map(m => {
              const selected = m.id === model;
              return (
                <div
                  key={m.id}
                  className={`model ${selected ? 'on' : ''}`}
                  data-id={m.id}
                  onClick={() => { if (m.available) setModel(m.id); }}
                >
                  <b>{m.name}</b>
                  <span>{m.unavailableReason || m.providerModel}</span>
                  {!m.available && <span style={{ color: 'var(--warn)', fontSize: '11px' }}>Retired</span>}
                </div>
              );
            })}
          </div>
          <div className="f" style={{ marginTop: 14 }}>
            <div className="sl"><span>Temperature</span><output id="tOut">{temp.toFixed(2)}</output></div>
            <input type="range" id="temp" min="0" max="2" step="0.05" value={temp}
              onChange={e => setTemp(parseFloat(e.target.value))} />
            <small className="sub">Lower is focused and repeatable, higher is more creative.</small>
          </div>
          <div className="f">
            <div className="sl"><span>Top P</span><output id="pOut">{topP.toFixed(2)}</output></div>
            <input type="range" id="topp" min="0.01" max="1" step="0.05" value={topP}
              onChange={e => setTopP(parseFloat(e.target.value))} />
            <small className="sub">Limits word choices to the most likely options.</small>
          </div>
          <div className="f">
            <div className="sl"><span>Max tokens</span><output id="mOut">{maxTokens}</output></div>
            <input type="range" id="maxt" min="128" max="8192" step="128" value={maxTokens}
              onChange={e => setMaxTokens(parseInt(e.target.value))} />
            <small className="sub">Upper limit on answer length.</small>
          </div>
          <button className="btn" id="llmSave" onClick={handleSave} disabled={saving}>
            {saving ? 'Saving...' : 'Save settings'}
          </button>
        </div>
        <div className="card pad">
          <h3>Config sent to the API</h3>
          <p className="hint">A live preview of the request body.</p>
          <pre id="llmJson" style={{ backgroundColor: '#16201d', color: '#d6e6df' }}>{JSON.stringify(preview, null, 2)}</pre>
        </div>
      </div>
    </section>
  );
}
