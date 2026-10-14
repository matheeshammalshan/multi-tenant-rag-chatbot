import { AppContext } from './useApp';
import type { ReactNode } from 'react';
import { useState, useEffect, useCallback, useRef } from 'react';
import type {
  ChatbotSummaryDto,
  OrganizationSummaryDto,
  ModelDto,
} from '../types';
import { chatbots as chatbotApi, organizations as orgApi, models as modelsApi } from '../services';
import { useAuth } from './useAuth';

export function AppProvider({ children }: { children: ReactNode }) {
  const [organizations, setOrganizations] = useState<OrganizationSummaryDto[]>([]);
  const [currentOrgId, setCurrentOrgId] = useState<number | null>(null);
  const [chatbots, setChatbots] = useState<ChatbotSummaryDto[]>([]);
  const [currentBotId, setCurrentBotId] = useState<number | null>(null);
  const [models, setModels] = useState<ModelDto[]>([]);
  const [loading, setLoading] = useState(true);

  const { user } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const generation = useRef(0);

  const load = useCallback((): Promise<void> => {
    const version = ++generation.current;
    if (!user) return Promise.resolve();
    return Promise.all([orgApi.list(), chatbotApi.list(), modelsApi.list()])
      .then(([orgs, bots, mds]) => {
        if (generation.current !== version) return;
        setOrganizations(orgs); setChatbots(bots); setModels(mds);
        setCurrentOrgId(previous => orgs.some(org => org.id === previous) ? previous :
          (orgs.find(org => org.id === user.organizationId) || orgs[0])?.id ?? null);
      }).catch(error => {
        if (generation.current === version) setError(error instanceof Error ? error.message : 'Unable to load workspace.');
        throw error;
      }).finally(() => { if (generation.current === version) setLoading(false); });
  }, [user]);

  const cancelPending = useCallback(() => { generation.current++; }, []);
  const refetch = useCallback(() => {
    setLoading(true); setError(null);
    return load();
  }, [load]);
  useEffect(() => {
    void load().catch(() => {});
    return cancelPending;
  }, [load, cancelPending]);

  const isOwner = user?.role === 'owner';
  const visibleOrgs = isOwner ? organizations : organizations.filter(o => o.id === user?.organizationId);
  const visibleBots = chatbots.filter(b => b.organizationId === currentOrgId);
  const currentOrg = organizations.find(o => o.id === currentOrgId);
  const currentBot = visibleBots.find(b => b.id === currentBotId) ?? visibleBots[0];

  return (
    <AppContext.Provider value={{
      user,
      organizations,
      visibleOrgs,
      currentOrgId,
      setCurrentOrgId,
      chatbots: visibleBots,
      currentBotId: currentBot?.id ?? null,
      setCurrentBotId,
      models,
      loading,
      error,
      refetch,
      isOwner,
      currentOrg,
      currentBot,
    }}>
      {children}
    </AppContext.Provider>
  );
}
