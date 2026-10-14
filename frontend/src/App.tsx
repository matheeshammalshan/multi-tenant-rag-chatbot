import { useEffect, useState, type ReactElement } from 'react';
import { useAuth } from './contexts/useAuth';
import { ToastProvider } from './contexts/ToastContext';
import { AuthProvider } from './contexts/AuthContext';
import { AuthScreen } from './components/AuthScreen';
import { Sidebar } from './components/Sidebar';
import { OverviewPage } from './components/OverviewPage';
import { ChatbotsPage } from './components/ChatbotsPage';
import { DocumentsPage } from './components/DocumentsPage';
import { ModelSettingsPage } from './components/ModelSettingsPage';
import { ChatPage } from './components/ChatPage';
import { HistoryPage } from './components/HistoryPage';
import { UsersPage } from './components/UsersPage';
import { OrganizationsPage } from './components/OrganizationsPage';
import { ApiAccessPage } from './components/ApiAccessPage';
import { KeyChatDialog } from './components/KeyChatDialog';
import { RouterProvider } from './components/AppRouter';
import { useNavigate, type Page } from './components/useNavigate';
import { AppProvider } from './contexts/AppContext';
import { useApp } from './contexts/useApp';
import { useToast } from './contexts/useToast';
import './index.css';

function MainApp() {
  const { user, initialized, logout } = useAuth();
  const { page } = useNavigate();
  const { error, refetch, currentBot, currentOrgId } = useApp();
  const { showToast } = useToast();
  const [theme, setTheme] = useState<'light' | 'dark'>(() => localStorage.getItem('theme') === 'dark' ? 'dark' : 'light');
  const [keyChatOpen, setKeyChatOpen] = useState(false);

  useEffect(() => { document.documentElement.dataset.theme = theme; }, [theme]);

  const toggleTheme = () => {
    const next = theme === 'dark' ? 'light' : 'dark';
    setTheme(next);
    document.documentElement.dataset.theme = next;
    localStorage.setItem('theme', next);
  };

  const handleLogout = async () => {
    try { await logout(); }
    catch (error) { showToast(error instanceof Error ? error.message : 'Unable to sign out.', 'error'); }
  };

  const handleKeyChat = () => setKeyChatOpen(true);

  if (!initialized) {
    return (
      <div style={{ height: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <div style={{ textAlign: 'center' }}>
          <div className="spin" style={{ width: 32, height: 32, margin: '0 auto 12px' }}></div>
          <p style={{ color: 'var(--muted)' }}>Checking session...</p>
        </div>
      </div>
    );
  }

  if (!user) {
    document.body.classList.add('guest');
    return (
      <>
        <AuthScreen onKeyChat={handleKeyChat} />
        <KeyChatDialog key={keyChatOpen ? "open" : "closed"} open={keyChatOpen} onClose={() => setKeyChatOpen(false)} standalone />
      </>
    );
  }

  document.body.classList.remove('guest');

  const pages: Record<Page, ReactElement | null> = {
    overview: <OverviewPage key={currentOrgId} />,
    chatbots: <ChatbotsPage />,
    docs: <DocumentsPage key={currentBot?.id} />,
    llm: <ModelSettingsPage key={currentBot?.id} />,
    chat: <ChatPage key={currentBot?.id} />,
    history: <HistoryPage key={currentBot?.id} />,
    users: <UsersPage />,
    orgs: <OrganizationsPage />,
    api: <ApiAccessPage />,
  };

  return (
    <>
      <div className="app">
        <Sidebar
          theme={theme}
          onToggleTheme={toggleTheme}
          onLogout={handleLogout}
          onKeyChat={handleKeyChat}
        />
        <main>
          {error ? <div className="card pad" role="alert"><p>{error}</p><button className="btn" onClick={() => { void refetch().catch(() => {}); }}>Retry loading workspace</button></div> : (pages[page] ?? pages.overview)}
        </main>
      </div>
      <KeyChatDialog key={keyChatOpen ? "open" : "closed"} open={keyChatOpen} onClose={() => setKeyChatOpen(false)} />
    </>
  );
}

function Workspace() {
  const { user } = useAuth();
  return <AppProvider key={user?.id ?? "guest"}><MainApp /></AppProvider>;
}

function App() {
  return (
    <ToastProvider>
      <AuthProvider>
        <RouterProvider>
          <Workspace />
        </RouterProvider>
      </AuthProvider>
    </ToastProvider>
  );
}

export default App;
