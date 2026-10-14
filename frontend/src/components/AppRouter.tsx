import { RouterContext, type Page } from './useNavigate';
import { useState, useEffect } from 'react';
import type { ReactNode } from 'react';

export function RouterProvider({ children }: { children: ReactNode }) {
  const [page, setPage] = useState<Page>('overview');

  useEffect(() => {
    const onHash = () => {
      const hash = window.location.hash.replace('#', '') as Page;
      if (hash && ['overview','chatbots','docs','llm','chat','history','users','orgs','api'].includes(hash)) {
        setPage(hash);
      }
    };
    onHash();
    window.addEventListener('hashchange', onHash);
    return () => window.removeEventListener('hashchange', onHash);
  }, []);

  const go = (p: Page) => {
    setPage(p);
    window.location.hash = '#' + p;
  };

  return <RouterContext.Provider value={{ page, go }}>{children}</RouterContext.Provider>;
}
