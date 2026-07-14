import { PublicClientApplication, type Configuration } from "@azure/msal-browser";

const clientId = import.meta.env.VITE_ENTRA_CLIENT_ID as string | undefined;
const tenantId = (import.meta.env.VITE_ENTRA_TENANT_ID as string | undefined) ?? "common";

export const ENTRA_CONFIGURED = Boolean(clientId);

/** Scope(s) requested for the API access token (Expose an API → scope). */
export const apiScopes: string[] = (import.meta.env.VITE_API_SCOPE
  ? [import.meta.env.VITE_API_SCOPE as string]
  : []);

export const loginScopes = ["openid", "profile", "email", ...apiScopes];

const msalConfig: Configuration = {
  auth: {
    clientId: clientId ?? "unconfigured",
    authority: `https://login.microsoftonline.com/${tenantId}`,
    redirectUri: window.location.origin,
  },
  cache: { cacheLocation: "localStorage", storeAuthStateInCookie: false },
};

export const pca: PublicClientApplication | null = ENTRA_CONFIGURED
  ? new PublicClientApplication(msalConfig)
  : null;
