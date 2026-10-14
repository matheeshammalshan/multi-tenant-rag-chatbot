import { createContext, useContext } from 'react';


export type Page = 'overview' | 'chatbots' | 'docs' | 'llm' | 'chat' | 'history' | 'users' | 'orgs' | 'api';

interface RouterContextType {
  page: Page;
  go: (p: Page) => void;
}

export const RouterContext = createContext<RouterContextType | undefined>(undefined);

export function useNavigate() {
  const ctx = useContext(RouterContext);
  if (!ctx) throw new Error('useNavigate must be used within RouterProvider');
  return ctx;
}

