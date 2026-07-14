import { useState } from "react";
import { api } from "../../api/client";
import { useApp } from "../../app/AppProvider";
import { useAsync } from "../../hooks/useAsync";
import { Button, Card, ErrorState, Loading } from "../../components/ui";
import { Toggle } from "./parts";

export function SettingsTab() {
  const { isAdmin, toast, refreshReference } = useApp();
  const { data, loading, error, reload } = useAsync(() => api.settings(), []);
  const [capped, setCapped] = useState(false);
  const [initialised, setInitialised] = useState(false);

  if (loading) return <Loading />;
  if (error) return <ErrorState message={error} onRetry={reload} />;

  if (data && !initialised) {
    setCapped(data.blockerCapsVerdict);
    setInitialised(true);
  }

  async function toggleCap(next: boolean) {
    try {
      const saved = await api.saveSettings({ blockerCapsVerdict: next });
      setCapped(saved.blockerCapsVerdict);
      toast("Setting saved");
    } catch (e) {
      toast(e instanceof Error ? e.message : "Save failed");
    }
  }

  async function reset() {
    try {
      const res = await api.resetSampleData();
      await refreshReference();
      toast(res.message);
    } catch (e) {
      toast(e instanceof Error ? e.message : "Reset failed");
    }
  }

  return (
    <Card style={{ padding: 24 }}>
      <div style={{ display: "flex", alignItems: "flex-start", gap: 16 }}>
        <Toggle on={capped} onChange={toggleCap} disabled={!isAdmin} />
        <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
          <div style={{ fontWeight: 700, color: "var(--text)" }}>
            A single unresolved blocker caps the verdict
          </div>
          <div style={{ color: "var(--muted)", fontSize: 14 }}>
            When on, any blocker forces &ldquo;Do not proceed&rdquo; regardless of other scores. Recommended.
          </div>
        </div>
      </div>

      <div style={{ borderTop: "1px solid var(--line)", margin: "20px 0", paddingTop: 20 }}>
        <div style={{ fontWeight: 700, color: "var(--text)", marginBottom: 4 }}>
          Reset sample data
        </div>
        <div style={{ color: "var(--muted)", fontSize: 14, marginBottom: 14 }}>
          Restore the seeded categories, sections, policies and example vendors. Clears your edits to this tool&rsquo;s data.
        </div>
        <Button variant="danger" onClick={reset} disabled={!isAdmin}>
          Reset to sample data
        </Button>
      </div>
    </Card>
  );
}
