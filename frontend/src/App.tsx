import { Navigate, Route, Routes } from "react-router-dom";
import { useApp } from "./app/AppProvider";
import { Shell } from "./components/Shell";
import { Toasts } from "./components/Toasts";
import { ENTRA_CONFIGURED, loginScopes, pca } from "./auth/msal";
import { Dashboard } from "./pages/Dashboard";
import { Compare } from "./pages/Compare";
import { Vendors } from "./pages/Vendors";
import { Archive } from "./pages/Archive";
import { Configuration } from "./pages/Configuration";
import { ReviewEditor } from "./pages/ReviewEditor";
import { Button } from "./components/ui";

function LoginGate({ children }: { children: React.ReactNode }) {
  // Only gates when real Entra is configured; dev mode renders straight through.
  if (!ENTRA_CONFIGURED || !pca) return <>{children}</>;
  const client = pca;
  const account = client.getAllAccounts()[0];
  if (account) return <>{children}</>;
  return (
    <div style={{ height: "100%", display: "flex", alignItems: "center", justifyContent: "center", background: "var(--canvas)" }}>
      <div style={{ textAlign: "center" }}>
        <div style={{ fontFamily: "'Space Grotesk',sans-serif", fontSize: 24, fontWeight: 700, marginBottom: 6 }}>Vendor Review · Assess</div>
        <div style={{ color: "var(--muted)", marginBottom: 20 }}>Sign in with your Birgma account to continue.</div>
        <Button onClick={() => client.loginRedirect({ scopes: loginScopes })}>Sign in with Microsoft</Button>
      </div>
    </div>
  );
}

export default function App() {
  const { ready } = useApp();
  return (
    <LoginGate>
      <Shell>
        {ready ? (
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/compare" element={<Compare />} />
            <Route path="/vendors" element={<Vendors />} />
            <Route path="/archive" element={<Archive />} />
            <Route path="/configuration" element={<Configuration />} />
            <Route path="/review/:id" element={<ReviewEditor />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        ) : (
          <div style={{ padding: 40, color: "var(--muted)" }}>Loading workspace…</div>
        )}
      </Shell>
      <Toasts />
    </LoginGate>
  );
}
