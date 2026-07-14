import {
  createContext, useCallback, useContext, useEffect, useMemo, useRef, useState,
  type ReactNode,
} from "react";
import { authBridge } from "../api/authBridge";
import { api } from "../api/client";
import { ENTRA_CONFIGURED, apiScopes, pca } from "../auth/msal";
import type { Category, Entity, Me, Role } from "../api/types";
import { applyTheme, type ThemeName } from "../theme/tokens";

interface Toast { id: number; message: string; }

interface AppState {
  me: Me | null;
  role: Role;
  setRole: (r: Role) => void;
  isAdmin: boolean;
  theme: ThemeName;
  setTheme: (t: ThemeName) => void;
  entities: Entity[];
  categories: Category[];
  refreshReference: () => Promise<void>;
  toast: (message: string) => void;
  toasts: Toast[];
  ready: boolean;
}

const Ctx = createContext<AppState | null>(null);

const ROLE_KEY = "vr.role";
const THEME_KEY = "vr.theme";

// Token getter used by the API client when Entra is configured.
async function acquireToken(): Promise<string | null> {
  if (!ENTRA_CONFIGURED || !pca) return null;
  const account = pca.getAllAccounts()[0];
  if (!account) return null;
  try {
    const res = await pca.acquireTokenSilent({ scopes: apiScopes, account });
    return res.accessToken;
  } catch {
    await pca.acquireTokenRedirect({ scopes: apiScopes, account });
    return null;
  }
}

export function AppProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<ThemeName>(
    () => (localStorage.getItem(THEME_KEY) as ThemeName) || "daylight",
  );
  const [role, setRoleState] = useState<Role>(
    () => (localStorage.getItem(ROLE_KEY) as Role) || "Cfo",
  );
  const [me, setMe] = useState<Me | null>(null);
  const [entities, setEntities] = useState<Entity[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [ready, setReady] = useState(false);
  const [toasts, setToasts] = useState<Toast[]>([]);
  const toastId = useRef(0);

  // Wire the API client to auth + role.
  useEffect(() => { authBridge.setTokenGetter(acquireToken); }, []);
  const roleRef = useRef(role);
  roleRef.current = role;
  useEffect(() => { authBridge.setRoleGetter(() => roleRef.current); }, []);

  useEffect(() => { applyTheme(theme); }, [theme]);

  const setTheme = useCallback((t: ThemeName) => {
    setThemeState(t);
    localStorage.setItem(THEME_KEY, t);
  }, []);

  const toast = useCallback((message: string) => {
    const id = ++toastId.current;
    setToasts((t) => [...t, { id, message }]);
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 3800);
  }, []);

  const refreshReference = useCallback(async () => {
    const [ents, cats] = await Promise.all([api.entities(), api.categories()]);
    setEntities(ents);
    setCategories(cats);
  }, []);

  const loadSession = useCallback(async () => {
    try {
      const m = await api.me();
      setMe(m);
      // In dev the role hint drives the API; keep the displayed role in sync.
      if (!ENTRA_CONFIGURED) { /* role stays as chosen */ }
      else setRoleState(m.role);
      await refreshReference();
    } catch {
      /* surfaces as empty state in pages */
    } finally {
      setReady(true);
    }
  }, [refreshReference]);

  useEffect(() => { void loadSession(); }, [loadSession]);

  const setRole = useCallback((r: Role) => {
    setRoleState(r);
    localStorage.setItem(ROLE_KEY, r);
    // Re-load session/reference under the new role hint (dev scoping).
    if (!ENTRA_CONFIGURED) { roleRef.current = r; void api.me().then(setMe); }
  }, []);

  const value = useMemo<AppState>(() => ({
    me, role, setRole, isAdmin: ENTRA_CONFIGURED ? (me?.isAdmin ?? false) : (role !== "ITManager"),
    theme, setTheme, entities, categories, refreshReference, toast, toasts, ready,
  }), [me, role, setRole, theme, setTheme, entities, categories, refreshReference, toast, toasts, ready]);

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function useApp(): AppState {
  const v = useContext(Ctx);
  if (!v) throw new Error("useApp must be used within AppProvider");
  return v;
}
