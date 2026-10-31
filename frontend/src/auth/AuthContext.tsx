import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';
import { authApi } from '../api/auth.api';
import { TOKEN_KEY, USER_KEY } from '../api/client';
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types/api';

interface AuthContextValue {
  user: Pick<AuthResponse, 'userId' | 'fullName' | 'email'> | null;
  isAuthenticated: boolean;
  login: (payload: LoginRequest) => Promise<void>;
  register: (payload: RegisterRequest) => Promise<void>;
  logout: () => void;
}
const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readUser(): AuthContextValue['user'] {
  try {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) as AuthContextValue['user'] : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthContextValue['user']>(readUser);

  const saveSession = useCallback((response: AuthResponse) => {
    localStorage.setItem(TOKEN_KEY, response.token);
    const nextUser = { userId: response.userId, fullName: response.fullName, email: response.email };
    localStorage.setItem(USER_KEY, JSON.stringify(nextUser));
    setUser(nextUser);
  }, []);

  const login = useCallback(async (payload: LoginRequest) => {
    saveSession(await authApi.login(payload));
  }, [saveSession]);

  const register = useCallback(async (payload: RegisterRequest) => {
    // The API returns the AuthResponse DTO on successful registration.
    saveSession(await authApi.register(payload));
  }, [saveSession]);

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    setUser(null);
  }, []);

  const value = useMemo(() => ({ user, isAuthenticated: Boolean(localStorage.getItem(TOKEN_KEY)), login, register, logout }), [user, login, register, logout]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error('useAuth must be used inside AuthProvider');
  return value;
}
