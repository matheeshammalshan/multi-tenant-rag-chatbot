import { useState } from 'react';
import { useApp } from '../contexts/useApp';
import { useToast } from '../contexts/useToast';
import { apiKey as apiKeyApi } from '../services';

export function ApiAccessPage() {
  const { currentBot } = useApp();
  const { showToast } = useToast();
  const [keyResult, setKeyResult] = useState<{ botId: number; apiKey: string; prefix: string } | null>(null);
  const [loading, setLoading] = useState(false);


  const handleGenerate = async () => {
    if (!currentBot) return;
    setLoading(true);
    try {
      const res = await apiKeyApi.generate({ chatBotId: currentBot.id });
      setKeyResult({ botId: currentBot.id, apiKey: res.apiKey, prefix: res.prefix });
      showToast('New API key generated.');
    } catch (err: any) {
      showToast(err.message, 'error');
    } finally {
      setLoading(false);
    }
  };

  const handleCopy = () => {
    const key = (keyResult?.botId === currentBot?.id ? keyResult?.apiKey : undefined);
    if (!key) { showToast('Generate a key first.'); return; }
    navigator.clipboard.writeText(key).then(() => showToast('Key copied.')).catch(() => showToast('Copy the key manually.'));
  };

  const displayKey = (keyResult?.botId === currentBot?.id ? keyResult?.apiKey : undefined) || (currentBot?.apiKeyPrefix ? currentBot.apiKeyPrefix + '...' : 'Create a chatbot to generate an API key.');

  const botId = currentBot?.id ?? 0;
  const curlCode = `curl -X POST http://localhost:5270/SemanticSearch/askQuery \\
  -H "x-api-key: YOUR_CHATBOT_API_KEY" \\
  -H "Content-Type: application/json" \\
  -d '{ "chatBotId": ${botId}, "query": "Hello" }'`;

  const jsCode = `const response = await fetch('http://localhost:5270/SemanticSearch/askQuery', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'x-api-key': 'YOUR_CHATBOT_API_KEY'
  },
  body: JSON.stringify({ chatBotId: ${botId}, query: 'Hello' })
});
const answer = await response.json();
console.log(answer);`;

  return (
    <section className="page on" id="api">
      <div className="head">
        <div>
          <h1>API access</h1>
          <p className="sub">Generate a key for your chatbot. Paste it to chat here or integrate the chatbot into your own website.</p>
        </div>
      </div>
      <div className="grid g2" style={{ alignItems: 'start' }}>
        <div className="card pad">
          <h3>API key</h3>
          <p className="hint"><span className="route">POST /Api/create-apikey</span></p>
          <div className="keybox">
            <span id="apiKey">{displayKey}</span>
            <button className="btn sm sec" id="copyKey" onClick={handleCopy} disabled={keyResult?.botId !== currentBot?.id || !keyResult?.apiKey}>Copy</button>
          </div>
          <div className="row" style={{ marginTop: 12 }}>
            <button className="btn sm" id="genKey" onClick={handleGenerate} disabled={loading || !currentBot}>
              {loading ? 'Generating...' : 'Generate API key'}
            </button>
            <small className="sub">Creating a new key replaces the old one.</small>
          </div>
        </div>
        <div className="card pad">
          <h3>Website integration</h3>
          <p className="hint">Send your chatbot key in the <span className="route">x-api-key</span> header. Replace the example host with your deployed API.</p>
          <pre style={{ backgroundColor: '#16201d', color: '#d6e6df' }}>{curlCode}</pre>
          <h3 style={{ marginTop: 18 }}>JavaScript example</h3>
          <pre style={{ backgroundColor: '#16201d', color: '#d6e6df' }}>{jsCode}</pre>
          <p className="sub" style={{ fontSize: 12, marginTop: 12 }}>This page can resolve and chat with your API key via /ChatbotAccess. Live embedded chat requires your deployed chatbot API.</p>
        </div>
      </div>
    </section>
  );
}
