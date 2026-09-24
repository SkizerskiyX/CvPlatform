import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { accountApi } from '../api/endpoints';
import { readRolesFromToken, tokenStorageKey } from '../api/client';
import type { Profile } from '../api/types';

type AuthState = {
  profile: Profile | null;
  roles: string[];
  isAuthenticated: boolean;
  isAdmin: boolean;
  isStaff: boolean;
  loading: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (email: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
  reloadProfile: () => Promise<void>;
  applyToken: (token: string) => Promise<void>;
};

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [roles, setRoles] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);

  const reloadProfile = useCallback(async () => {
    const token = localStorage.getItem(tokenStorageKey);
    if (!token) {
      setProfile(null);
      setRoles([]);
      return;
    }

    setRoles(readRolesFromToken(token));
    setProfile(await accountApi.me());
  }, []);

  const applyToken = useCallback(
    async (token: string) => {
      localStorage.setItem(tokenStorageKey, token);
      setRoles(readRolesFromToken(token));
      setProfile(await accountApi.me());
    },
    [],
  );

  useEffect(() => {
    reloadProfile()
      .catch(() => setProfile(null))
      .finally(() => setLoading(false));
  }, [reloadProfile]);

  const signIn = useCallback(
    async (email: string, password: string) => {
      const result = await accountApi.login(email, password);
      await applyToken(result.token);
    },
    [applyToken],
  );

  const signUp = useCallback(
    async (email: string, password: string) => {
      const result = await accountApi.register(email, password);
      await applyToken(result.token);
    },
    [applyToken],
  );

  const signOut = useCallback(async () => {
    await accountApi.logout().catch(() => undefined);
    localStorage.removeItem(tokenStorageKey);
    setProfile(null);
    setRoles([]);
  }, []);

  const value = useMemo<AuthState>(
    () => ({
      profile,
      roles,
      isAuthenticated: profile !== null,
      isAdmin: roles.includes('Admin'),
      isStaff: roles.includes('Admin') || roles.includes('Recruiter'),
      loading,
      signIn,
      signUp,
      signOut,
      reloadProfile,
      applyToken,
    }),
    [profile, roles, loading, signIn, signUp, signOut, reloadProfile, applyToken],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
