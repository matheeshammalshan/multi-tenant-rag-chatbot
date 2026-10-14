import { useAuth } from '../contexts/useAuth';
import { useApp } from '../contexts/useApp';
import { useNavigate } from './useNavigate';
import type { ReactNode } from 'react';

function IconOverview() { return <><rect x="3" y="3" width="7" height="9"/><rect x="14" y="3" width="7" height="5"/><rect x="14" y="12" width="7" height="9"/><rect x="3" y="16" width="7" height="5"/></>; }
function IconChatbots() { return <><rect x="4" y="8" width="16" height="12" rx="3"/><path d="M12 8V4M9 14h.01M15 14h.01"/></>; }
function IconDocs() { return <><path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z"/><path d="M14 3v5h5"/></>; }
function IconLlm() { return <><path d="M4 6h10M18 6h2M4 12h2M10 12h10M4 18h12M20 18h0"/><circle cx="16" cy="6" r="2"/><circle cx="8" cy="12" r="2"/><circle cx="18" cy="18" r="2"/></>; }
function IconChat() { return <><path d="M21 12a8 8 0 0 1-11.6 7.1L3 21l1.9-5.4A8 8 0 1 1 21 12z"/></>; }
function IconHistory() { return <><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></>; }
function IconUsers() { return <><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3.45-3.95"/></>; }
function IconOrgs() { return <><path d="M3 21h18M5 21V7l7-4 7 4v14M9 21V11h6v10"/></>; }
function IconApi() { return <><circle cx="8" cy="15" r="4"/><path d="m11 12 9-9M16 7l3 3"/></>; }

const navItems: { id: string; label: string; icon: ReactNode }[] = [
  { id: 'overview', label: 'Overview', icon: <IconOverview /> },
  { id: 'chatbots', label: 'Chatbots', icon: <IconChatbots /> },
  { id: 'docs', label: 'Knowledge base', icon: <IconDocs /> },
  { id: 'llm', label: 'Model settings', icon: <IconLlm /> },
  { id: 'chat', label: 'Playground', icon: <IconChat /> },
  { id: 'history', label: 'Chat history', icon: <IconHistory /> },
  { id: 'api', label: 'API access', icon: <IconApi /> },
];

const managementItems: { id: string; label: string; icon: ReactNode }[] = [
  { id: 'users', label: 'Users & organizations', icon: <IconUsers /> },
  { id: 'orgs', label: 'Organizations', icon: <IconOrgs /> },
];

export function Sidebar({
  theme,
  onToggleTheme,
  onLogout,
  onKeyChat,
}: {
  theme: 'light' | 'dark';
  onToggleTheme: () => void;
  onLogout: () => void;
  onKeyChat: () => void;
}) {
  const { user } = useAuth();
  const { page, go } = useNavigate();
  const { organizations, currentOrgId, setCurrentOrgId, isOwner, chatbots, setCurrentBotId } = useApp();

  const visibleOrgs = isOwner ? organizations : organizations.filter(o => o.id === user?.organizationId);

  const handleOrgChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const orgId = parseInt(e.target.value);
    setCurrentOrgId(orgId);
    const bot = chatbots.find(b => b.organizationId === orgId);
    setCurrentBotId(bot?.id ?? null);
  };

  return (
    <aside>
      <div className="brand">
        <div className="logo">R</div>
        <div><b>RAG Console</b><span>Multi-tenant chatbots</span></div>
      </div>
      <div className="tenant">
        <label htmlFor="orgSel">Organization</label>
        <select id="orgSel" value={currentOrgId ?? ''} onChange={handleOrgChange} disabled={!isOwner}>
          {visibleOrgs.map(o => (
            <option key={o.id} value={o.id}>{o.name}</option>
          ))}
        </select>
      </div>
      <nav>
        <div className="grp">Workspace</div>
        {navItems.map(item => (
          <button
            key={item.id}
            className={page === item.id ? 'on' : ''}
            onClick={() => go(item.id as any)}
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              {item.icon}
            </svg>
            {item.label}
          </button>
        ))}
        {isOwner && (
          <>
            <div className="grp">Management</div>
            {managementItems.map(item => (
              <button
                key={item.id}
                className={page === item.id ? 'on' : ''}
                onClick={() => go(item.id as any)}
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  {item.icon}
                </svg>
                {item.label}
              </button>
            ))}
          </>
        )}
      </nav>
      <div className="side-foot">
        <button className="ghost" onClick={onToggleTheme} style={{ flex: 1 }}>
          {theme === 'dark' ? 'Light' : 'Dark'} theme
        </button>
        <button className="ghost" style={{ flex: 1 }} onClick={onKeyChat}>Chat with API key</button>
        <button className="ghost" style={{ flex: 1 }} onClick={onLogout}>Sign out</button>
      </div>
    </aside>
  );
}
