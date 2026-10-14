import { toastContext } from './useToast';
import type { ReactNode } from 'react';
import { useState, useCallback } from 'react';
import type { Toast } from '../types';

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const showToast = useCallback((message: string, type: Toast['type'] = 'info') => {
    const toast: Toast = { id: Date.now() + Math.random(), message, type };
    setToasts(prev => [...prev, toast]);
    setTimeout(() => setToasts(prev => prev.filter(t => t.id !== toast.id)), 3600);
  }, []);

  return (
    <toastContext.Provider value={{ showToast }}>
      {children}
      {toasts.length > 0 && (
        <div className="toast">
          {toasts.map(t => (
            <div key={t.id}>{t.message}</div>
          ))}
        </div>
      )}
    </toastContext.Provider>
  );
}
