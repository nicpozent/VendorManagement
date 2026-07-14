import type { Role } from "./types";

// Decouples the API client from the auth implementation. The AuthProvider installs
// a token getter (MSAL) and the current role hint; the client reads them per request.
type TokenGetter = () => Promise<string | null>;

let tokenGetter: TokenGetter = async () => null;
let roleGetter: () => Role = () => "Cfo";

export const authBridge = {
  setTokenGetter(fn: TokenGetter) { tokenGetter = fn; },
  setRoleGetter(fn: () => Role) { roleGetter = fn; },
  getToken: () => tokenGetter(),
  getRole: () => roleGetter(),
};

export const ENTRA_CONFIGURED = Boolean(import.meta.env.VITE_ENTRA_CLIENT_ID);
