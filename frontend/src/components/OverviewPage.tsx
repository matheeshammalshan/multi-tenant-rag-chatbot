import { useEffect, useState } from 'react';
import { useApp } from '../contexts/useApp';
import { useToast } from '../contexts/useToast';
import { dashboard } from '../services';
import type { DashboardDto } from '../types';

export function OverviewPage() {
  const { user, currentOrg, currentOrgId, chatbots, models } = useApp();
  const { showToast } = useToast();
  const [dashboardData, setDashboardData] = useState<DashboardDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (currentOrgId == null) return;
    let active = true;
    dashboard
      .get(currentOrgId)
      .then(data => { if (active) setDashboardData(data); })
      .catch(err => { if (active) showToast(err.message, 'error'); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [currentOrgId, showToast]);


  if (loading || !dashboardData) {
    return (
      <section className="page on">
        <div className="head"><h1>Overview</h1><p className="sub" style={{ color: 'var(--muted)' }}>{currentOrgId == null ? 'Select an organization to view its overview.' : loading ? 'Loading...' : 'Overview could not be loaded. Reopen this page to retry.'}</p></div>
      </section>
    );
  }

  const orgName = currentOrg?.name || 'No organization';
  const bs = chatbots;
  const mx = Math.max(1, ...bs.map(b => b.totalQueries));

  return (
    <section className="page on">
      <div className="head">
        <div>
          <h1>Overview</h1>
          <p className="sub">{orgName} / {user?.role || 'user'}</p>
        </div>
        <span className="route">AnalysisEntity per chatbot</span>
      </div>
      <div className="grid g4" id="stats">
        <div className="card stat"><small>Chatbots</small><div className="n">{dashboardData.chatbotCount}</div><div className="d">active</div></div>
        <div className="card stat"><small>Total queries</small><div className="n">{dashboardData.totalQueries}</div><div className="d">all time</div></div>
        <div className="card stat"><small>Documents</small><div className="n">{dashboardData.documentCount}</div><div className="d">{dashboardData.processingCount} processing</div></div>
        <div className="card stat"><small>Searchable chunks</small><div className="n">{dashboardData.totalChunks}</div><div className="d">indexed</div></div>
      </div>
      <div className="grid g21" style={{ marginTop: 16 }}>
        <div className="card pad">
          <h3>Chatbots in this organization</h3>
          <p className="hint">Queries, documents and chunks tracked per bot.</p>
          <div id="ovBots">
            {bs.map(b => {
              const mdl = models.find(m => m.id === b.configuration?.modelName);
              return (
                <div key={b.id} style={{ padding: '10px 0', borderBottom: '1px solid var(--line)' }}>
                  <div className="row" style={{ justifyContent: 'space-between' }}>
                    <b>{(b.name)}</b>
                    <span className="mini">
                      <span><b>{b.totalQueries}</b> queries</span>
                      <span><b>{b.documentCount}</b> docs</span>
                      <span><b>{b.totalChunks}</b> chunks</span>
                    </span>
                  </div>
                  <div className="bar" style={{ marginTop: 8 }}>
                    <i style={{ width: `${b.totalQueries / mx * 100}%` }}></i>
                  </div>
                  <div className="row" style={{ marginTop: 6 }}>
                    <span className="chip">{mdl?.name || b.configuration?.modelName || 'No model'}</span>
                    {b.apiKeyPrefix && <span className="chip mono">{b.apiKeyPrefix}...</span>}
                  </div>
                </div>
              );
            })}
            {bs.length === 0 && <p className="sub">No chatbots yet.</p>}
          </div>
        </div>
        <div className="card pad">
          <h3>How an answer is made</h3>
          <p className="hint">What happens on every question.</p>
          <div className="pipe">
            <div><i>1</i><p style={{ margin: 0 }}><b>Embed the question</b><span>Ollama turns the query into a vector</span></p></div>
            <div><i>2</i><p style={{ margin: 0 }}><b>Search this chatbot's chunks</b><span>Qdrant, filtered by chatbot id</span></p></div>
            <div><i>3</i><p style={{ margin: 0 }}><b>Ask the selected LLM</b><span>System prompt + retrieved text</span></p></div>
            <div><i>4</i><p style={{ margin: 0 }}><b>Save to history</b><span>Stored in MongoDB</span></p></div>
          </div>
        </div>
      </div>
    </section>
  );
}
