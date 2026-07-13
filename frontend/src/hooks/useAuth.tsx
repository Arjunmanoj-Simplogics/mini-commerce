import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { authApi, type AuthResponse } from '../services/commerceApi';
import { getToken, setToken } from '../services/apiClient';

type AuthUser = Omit<AuthResponse, 'token' | 'expiresAtUtc'>;

interface AuthState {
  token: string | null;
  user: AuthUser | null;
  ready: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, fullName: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setTokenState] = useState<string | null>(() => getToken());
  const [user, setUser] = useState<AuthUser | null>(null);
  const [ready, setReady] = useState(false);

  const applyAuth = useCallback((response: AuthResponse) => {
    setToken(response.token);
    setTokenState(response.token);
    setUser({
      userId: response.userId,
      email: response.email,
      fullName: response.fullName,
      role: response.role,
    });
  }, []);

  const logout = useCallback(() => {
    setToken(null);
    setTokenState(null);
    setUser(null);
  }, []);

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      const existing = getToken();
      if (!existing) {
        if (!cancelled) setReady(true);
        return;
      }

      try {
        const me = await authApi.me();
        if (cancelled) return;
        setTokenState(existing);
        setUser({
          userId: me.id,
          email: me.email,
          fullName: me.fullName,
          role: me.role,
        });
      } catch {
        if (!cancelled) logout();
      } finally {
        if (!cancelled) setReady(true);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [logout]);

  const value = useMemo<AuthState>(
    () => ({
      token,
      user,
      ready,
      isAuthenticated: Boolean(token),
      login: async (email, password) => applyAuth(await authApi.login(email, password)),
      register: async (email, fullName, password) => applyAuth(await authApi.register(email, fullName, password)),
      logout,
    }),
    [token, user, ready, applyAuth, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
