import { createContext, useContext } from 'react';
import type { UserDto, RegisterRequest, LoginRequest } from '../types';

interface AuthContextType {
  user: UserDto | null;
  loading: boolean;
  initialized: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}

