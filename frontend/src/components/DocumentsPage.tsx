import { useEffect, useState, useCallback, useRef } from 'react';
import { useApp } from '../contexts/useApp';
import { useToast } from '../contexts/useToast';
import { documents as docApi } from '../services';
import type { DocumentDetailsDto, ReaderType } from '../types';

export function DocumentsPage() {
  const { currentBot, chatbots } = useApp();
  const { showToast } = useToast();
  const [docs, setDocs] = useState<DocumentDetailsDto[]>([]);
  const [docBotId, setDocBotId] = useState<number | null>(currentBot?.id ?? null);
  const [readerType, setReaderType] = useState<ReaderType>('Pdf');
  const [dragOver, setDragOver] = useState(false);
  const [_file, setFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);

  const generation = useRef(0);
  const loadDocs = useCallback((botId: number) => {
    const version = ++generation.current;
    docApi.list(botId).then(data => { if (generation.current === version) setDocs(data); })
      .catch((error: Error) => { if (generation.current === version) showToast(error.message, 'error'); });
  }, [showToast]);

  useEffect(() => { if (docBotId) loadDocs(docBotId); }, [docBotId, loadDocs]);

  const handleBotChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const id = parseInt(e.target.value);
    setDocBotId(id);
    setDocs([]);
  };

  const handleDrop = async (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    const f = e.dataTransfer.files[0];
    await uploadFile(f);
  };

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0];
    await uploadFile(f);
  };

  const uploadFile = async (f: File | undefined) => {
    if (uploading) return;
    if (!f || !docBotId) return showToast('Select a chatbot first.');
    const allowed = readerType === 'Pdf' ? ['.pdf'] : ['.txt', '.md'];
    const ext = '.' + f.name.split('.').pop()?.toLowerCase();
    if (!allowed.includes(ext)) return showToast(`Please upload a ${readerType === 'Pdf' ? 'PDF' : 'text'} file.`);

    setUploading(true);
    try {
      const form = new FormData();
      form.append('ChatBotId', docBotId.toString());
      form.append('Name', f.name);
      form.append('Size', (f.size / 1048576).toString());
      form.append('UploadDate', new Date().toISOString().split('T')[0]);
      form.append('Status', 'Processing');
      form.append('ReaderType', readerType);
      form.append('File', f);
      await docApi.upload(form);
      showToast('Document uploaded. Processing has started.');
      loadDocs(docBotId);
    } catch (err: any) {
      showToast(err.message, 'error');
    } finally {
      setUploading(false);
      setFile(null);
    }
  };


  const botOptions = chatbots.length > 0 ? chatbots : (currentBot ? [currentBot] : []);

  return (
    <section className="page on" id="docs">
      <div className="head">
        <div>
          <h1>Knowledge base</h1>
          <p className="sub">Upload files. They are split into chunks, embedded and made searchable.</p>
        </div>
        <div className="pick">
          <span className="sub">Chatbot</span>
          <select className="botSel" value={docBotId ?? ''} onChange={handleBotChange}>
            {botOptions.map(b => <option key={b.id} value={b.id}>{(b.name)}</option>)}
            {botOptions.length === 0 && <option>No chatbots yet</option>}
          </select>
        </div>
      </div>
      <div className="grid g21" style={{ alignItems: 'start' }}>
        <div className="card">
          <div className="pad" style={{ paddingBottom: 6 }}>
            <h3>Documents</h3>
            <p className="hint" style={{ marginBottom: 0 }}>Documents are processed in the background. Reopen this page to see the latest status.</p>
          </div>
          <table>
            <thead><tr><th>File</th><th>Uploaded</th><th>Chunks</th><th>Status</th></tr></thead>
            <tbody id="docRows">
              {docs.map(d => (
                <tr key={d.id}>
                  <td>
                    <div className="file">
                      <div className={`ico ${d.readerType === 'Text' ? 't' : ''}`}>
                        {d.readerType === 'Text' ? 'TXT' : 'PDF'}
                      </div>
                      <div>
                        <b>{(d.name)}</b>
                        <div className="sub">{d.size.toFixed(1)} MB</div>
                      </div>
                    </div>
                  </td>
                  <td>{d.uploadDate}</td>
                  <td>{d.status === 'Processed' || d.status === 'Proceed' ? d.chunks : '—'}</td>
                  <td>
                    {d.status === 'Processed' || d.status === 'Proceed' ? (
                      <span className="chip ok">Ready</span>
                    ) : d.status === 'Failed' ? (
                      <span className="chip warn">{(d.error)}</span>
                    ) : (
                      <span className="chip warn"><i className="spin"></i>Processing</span>
                    )}
                  </td>
                </tr>
              ))}
              {docs.length === 0 && (
                <tr><td colSpan={4} className="sub" style={{ padding: '28px', textAlign: 'center' }}>No documents yet. Upload your first file.</td></tr>
              )}
            </tbody>
          </table>
        </div>
        <div className="card pad">
          <h3>Upload document</h3>
          <p className="hint"><span className="route">POST /Document/create</span></p>
          <div className="f"><label>Reader type</label>
            <div className="seg" id="rdSeg">
              <button type="button" className={readerType === 'Pdf' ? 'on' : ''} onClick={() => setReaderType('Pdf')}>PDF</button>
              <button type="button" className={readerType === 'Text' ? 'on' : ''} onClick={() => setReaderType('Text')}>Text</button>
            </div>
          </div>
          <div
            className={`drop ${dragOver ? 'over' : ''}`}
            id="drop"
            onDragOver={e => { e.preventDefault(); setDragOver(true); }}
            onDragLeave={() => setDragOver(false)}
            onDrop={handleDrop}
            onClick={() => document.getElementById('fileIn')?.click()}
          >
            Drop a file here or <u>browse</u>
            <br /><small id="dropHint">{readerType === 'Pdf' ? 'PDF files' : 'Plain text files'}</small>
            <input type="file" id="fileIn" hidden
              accept={readerType === 'Pdf' ? '.pdf' : '.txt,.md'}
              onChange={handleFileSelect}
            />
          </div>
          {uploading && <div className="row" style={{ marginTop: 12 }}><i className="spin"></i> Uploading...</div>}
        </div>
      </div>
    </section>
  );
}
