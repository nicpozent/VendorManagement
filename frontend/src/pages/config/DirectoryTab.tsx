import { useEffect, useState } from "react";
import { api } from "../../api/client";
import { useApp } from "../../app/AppProvider";
import { useAsync } from "../../hooks/useAsync";
import { Button, Card, EmptyState, ErrorState, Loading } from "../../components/ui";
import { Toggle, TabHint, selectStyle } from "./parts";
import type { AppUser, GraphMember, ImportKind, ImportSource } from "../../api/types";

const KINDS: { value: ImportKind; label: string }[] = [
  { value: "group", label: "Groups" },
  { value: "servicePrincipal", label: "Enterprise apps" },
  { value: "application", label: "App registrations" },
];

const ROLES: { value: string; label: string }[] = [
  { value: "ITManager", label: "IT Manager" },
  { value: "CioCto", label: "CIO / CTO" },
  { value: "Cfo", label: "CFO" },
];

const head: React.CSSProperties = { background: "var(--panel2)", color: "var(--muted)", fontSize: 11, letterSpacing: 0.8, textAlign: "left" };
const th: React.CSSProperties = { padding: "12px 16px", fontWeight: 700, textTransform: "uppercase" };
const td: React.CSSProperties = { padding: "12px 16px", color: "var(--text)", fontSize: 14, verticalAlign: "middle" };

export function DirectoryTab() {
  const { isAdmin, toast } = useApp();

  const status = useAsync(() => api.graphStatus(), []);
  const usersState = useAsync(() => api.users(), []);

  const [kind, setKind] = useState<ImportKind>("group");
  const [sources, setSources] = useState<ImportSource[]>([]);
  const [sourceId, setSourceId] = useState<string>("");
  const [preview, setPreview] = useState<GraphMember[]>([]);
  const [defaultRole, setDefaultRole] = useState<string>("ITManager");
  const [importing, setImporting] = useState(false);
  const [reminding, setReminding] = useState(false);

  useEffect(() => {
    if (!isAdmin) return;
    let cancelled = false;
    setSourceId("");
    setPreview([]);
    setSources([]);
    api.importSources(kind)
      .then((s) => { if (!cancelled) setSources(s); })
      .catch((e) => { if (!cancelled) toast(e instanceof Error ? e.message : "Failed to load sources"); });
    return () => { cancelled = true; };
  }, [kind, isAdmin, toast]);

  if (!isAdmin) {
    return (
      <EmptyState
        title="Administrators only"
        hint="Only CIO/CTO and CFO can import and manage users."
      />
    );
  }

  async function selectSource(id: string) {
    setSourceId(id);
    setPreview([]);
    if (!id) return;
    try {
      const members = await api.importPreview(kind, id);
      setPreview(members);
    } catch (e) {
      toast(e instanceof Error ? e.message : "Preview failed");
    }
  }

  const selected = sources.find((s) => s.id === sourceId);

  async function doImport() {
    if (!selected) return;
    setImporting(true);
    try {
      const r = await api.importUsers({
        sourceId: selected.id,
        sourceKind: kind,
        sourceName: selected.displayName,
        defaultRole,
      });
      toast(`Imported ${r.imported}, updated ${r.updated}${r.warning ? ` — ${r.warning}` : ""}`);
      usersState.reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Import failed");
    } finally {
      setImporting(false);
    }
  }

  async function runReminders() {
    setReminding(true);
    try {
      const r = await api.runReminders();
      toast(`Reminders attempted: ${r.attempted}`);
    } catch (e) {
      toast(e instanceof Error ? e.message : "Reminder sweep failed");
    } finally {
      setReminding(false);
    }
  }

  async function changeRole(u: AppUser, role: string) {
    try {
      await api.updateUser(u.id, role, u.enabled);
      toast(`Role updated for ${u.displayName}`);
      usersState.reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Update failed");
    }
  }

  async function changeEnabled(u: AppUser, next: boolean) {
    try {
      await api.updateUser(u.id, u.role, next);
      toast(`${u.displayName} ${next ? "enabled" : "disabled"}`);
      usersState.reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Update failed");
    }
  }

  async function removeUser(u: AppUser) {
    try {
      await api.deleteUser(u.id);
      toast(`Removed ${u.displayName}`);
      usersState.reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Remove failed");
    }
  }

  const users = usersState.data ?? [];

  return (
    <div>
      <TabHint>
        Provision users from Microsoft Entra — import from a group, enterprise app, or app
        registration via Microsoft Graph.
      </TabHint>

      {status.loading ? (
        <Loading />
      ) : status.error ? (
        <ErrorState message={status.error} onRetry={status.reload} />
      ) : status.data && !status.data.configured ? (
        <Card
          style={{
            padding: "12px 16px",
            marginBottom: 24,
            background: "color-mix(in srgb, #F2A516 10%, var(--panel))",
            borderColor: "color-mix(in srgb, #F2A516 30%, var(--line))",
            color: "var(--text)",
            fontSize: 14,
          }}
        >
          Microsoft Graph is not configured — import and reminders use mock directory data. Set
          AzureAd + Graph settings to go live.
        </Card>
      ) : null}

      {/* Import panel */}
      <Card style={{ padding: 20, marginBottom: 24 }}>
        <h2 style={{ fontSize: 18, margin: "0 0 16px" }}>Import from Entra</h2>
        <div style={{ display: "flex", flexWrap: "wrap", gap: 12, alignItems: "flex-end" }}>
          <label style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            <span style={labelText}>SOURCE TYPE</span>
            <select
              style={selectStyle()}
              value={kind}
              onChange={(e) => setKind(e.target.value as ImportKind)}
            >
              {KINDS.map((k) => (
                <option key={k.value} value={k.value}>{k.label}</option>
              ))}
            </select>
          </label>

          <label style={{ display: "flex", flexDirection: "column", gap: 6, flex: 1, minWidth: 260 }}>
            <span style={labelText}>SOURCE</span>
            <select
              style={{ ...selectStyle(), width: "100%" }}
              value={sourceId}
              onChange={(e) => selectSource(e.target.value)}
            >
              <option value="">— Select source —</option>
              {sources.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.displayName}{s.description ? ` — ${s.description}` : ""}
                </option>
              ))}
            </select>
          </label>

          <label style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            <span style={labelText}>DEFAULT ROLE</span>
            <select
              style={selectStyle()}
              value={defaultRole}
              onChange={(e) => setDefaultRole(e.target.value)}
            >
              {ROLES.map((r) => (
                <option key={r.value} value={r.value}>{r.label}</option>
              ))}
            </select>
          </label>
        </div>

        {preview.length > 0 && (
          <div style={{ marginTop: 18 }}>
            <div style={{ fontSize: 13, fontWeight: 700, color: "var(--muted)", marginBottom: 8 }}>
              {preview.length} users found
            </div>
            <div style={{ border: "1px solid var(--line)", borderRadius: 12, overflowX: "auto" }}>
              <table style={{ width: "100%", borderCollapse: "collapse", minWidth: 480 }}>
                <thead>
                  <tr style={head}>
                    <th style={th}>NAME</th>
                    <th style={th}>EMAIL</th>
                    <th style={th}>TITLE</th>
                  </tr>
                </thead>
                <tbody>
                  {preview.map((m) => (
                    <tr key={m.objectId} style={{ borderTop: "1px solid var(--line)" }}>
                      <td style={td}>{m.displayName}</td>
                      <td style={{ ...td, color: "var(--muted)" }}>{m.email ?? "—"}</td>
                      <td style={{ ...td, color: "var(--muted)" }}>{m.jobTitle ?? "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        <div style={{ display: "flex", gap: 12, marginTop: 18 }}>
          <Button onClick={doImport} disabled={!sourceId || importing}>
            {importing ? "Importing…" : "Import users"}
          </Button>
          <Button variant="soft" onClick={runReminders} disabled={reminding}>
            {reminding ? "Sending…" : "Run reminder sweep"}
          </Button>
        </div>
      </Card>

      {/* Imported users */}
      {usersState.loading ? (
        <Loading />
      ) : usersState.error ? (
        <ErrorState message={usersState.error} onRetry={usersState.reload} />
      ) : users.length === 0 ? (
        <EmptyState title="No users imported yet" hint="Import from Entra above." />
      ) : (
        <Card style={{ overflowX: "auto" }}>
          <table style={{ width: "100%", borderCollapse: "collapse", minWidth: 820 }}>
            <thead>
              <tr style={head}>
                <th style={th}>USER</th>
                <th style={th}>TITLE</th>
                <th style={th}>SOURCE</th>
                <th style={th}>ROLE</th>
                <th style={th}>ENABLED</th>
                <th style={th}></th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id} style={{ borderTop: "1px solid var(--line)" }}>
                  <td style={td}>
                    <div style={{ fontWeight: 700 }}>{u.displayName}</div>
                    {u.email && <div style={{ fontSize: 12, color: "var(--muted)" }}>{u.email}</div>}
                  </td>
                  <td style={{ ...td, color: "var(--muted)" }}>{u.jobTitle ?? "—"}</td>
                  <td style={{ ...td, color: "var(--muted)" }}>{u.sourceName ?? "—"}</td>
                  <td style={td}>
                    <select
                      style={selectStyle()}
                      value={u.role}
                      onChange={(e) => changeRole(u, e.target.value)}
                    >
                      {ROLES.map((r) => (
                        <option key={r.value} value={r.value}>{r.label}</option>
                      ))}
                    </select>
                  </td>
                  <td style={td}>
                    <Toggle on={u.enabled} onChange={(next) => changeEnabled(u, next)} />
                  </td>
                  <td style={td}>
                    <Button variant="danger" onClick={() => removeUser(u)}>Remove</Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  );
}

const labelText: React.CSSProperties = {
  fontSize: 11,
  fontWeight: 700,
  letterSpacing: 0.6,
  color: "var(--muted)",
  textTransform: "uppercase",
};
