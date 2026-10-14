import { createContext, useContext } from 'react';
import type { Toast } from '../types';

export const toastContext = createContext<{ showToast: (message: string, type?: Toast['type']) => void } | null>(null);

export function useToast() {
  const ctx = useContext(toastContext);
  if (!ctx) throw new Error('useToast must be used within ToastProvider');
  return ctx;
}

