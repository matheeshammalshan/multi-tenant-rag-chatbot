import { createContext, useContext } from 'react';
import type { ChatbotSummaryDto, OrganizationSummaryDto, ModelDto, UserDto } from '../types';

interface AppContextType {
  user: UserDto | null;
  organizations: OrganizationSummaryDto[];
  visibleOrgs: OrganizationSummaryDto[];
  currentOrgId: number | null;
  setCurrentOrgId: (id: number | null) => void;
  chatbots: ChatbotSummaryDto[];
  currentBotId: number | null;
  setCurrentBotId: (id: number | null) => void;
  models: ModelDto[];
  loading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
  isOwner: boolean;
  currentOrg: OrganizationSummaryDto | undefined;
  currentBot: ChatbotSummaryDto | undefined;
}

export const AppContext = createContext<AppContextType | undefined>(undefined);

export function useApp() {
  const ctx = useContext(AppContext);
  if (!ctx) throw new Error('useApp must be used within AppProvider');
  return ctx;
}

