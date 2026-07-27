import { useState } from "react";
import { api } from "../../api/client";
import { useApp } from "../../app/AppProvider";
import { useAsync } from "../../hooks/useAsync";
import { Button, Card, ErrorState, Loading, inputStyle } from "../../components/ui";
import { Toggle } from "./parts";

export function SettingsTab() {
  const { isAdmin, toast, refreshReference } = useApp();
  const { data, loading, error, reload } = useAsync(() => api.settings(), []);
  const [capped, setCapped] = useState(false);
  const [interval, setIntervalMonths] = useState(12);
  const [initialised, setInitialised] = useState(false);

  if (loading) return <Loading />;
  if (error) return <ErrorState message={error} onRetry={reload} />;

  if (data && !initialised) {
    setCapped(data.blockerCapsVerdict);
    setIntervalMonths(data.reviewIntervalMonths);
    setInitialised(true);
  }

  async function toggleCap(next: boolean) {
    try {
      const saved = await api.saveSettings({ blockerCapsVerdict: next, reviewIntervalMonths: interval });
      setCapped(saved.blockerCapsVerdict);
      toast("Setting saved");
    } catch (e) {
      toast(e instanceof Error ? e.message : "Save failed");
    }
  }

  async function saveInterval(next: number) {
    const months = Number.isFinite(next) && next >= 1 ? Math.round(next) : 12;
    try {
      const saved = await api.saveSettings({ blockerCapsVerdict: capped, reviewIntervalMonths: months });
      setIntervalMonths(saved.reviewIntervalMonths);
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
          Re-review interval
        </div>
        <div style={{ color: "var(--muted)", fontSize: 14, marginBottom: 14 }}>
          Months after an approved vendor&rsquo;s last review before a re-review is due. Vendors past
          this window are flagged &ldquo;Due soon&rdquo; (within 60 days) or &ldquo;Overdue&rdquo; on the dashboard and Vendors page.
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <input
            type="number" min={1} max={60} value={interval} disabled={!isAdmin}
            onChange={(e) => setIntervalMonths(Number(e.target.value))}
            onBlur={(e) => void saveInterval(Number(e.target.value))}
            style={{ ...inputStyle, width: 90 }}
          />
          <span style={{ color: "var(--muted)", fontSize: 14 }}>months</span>
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
