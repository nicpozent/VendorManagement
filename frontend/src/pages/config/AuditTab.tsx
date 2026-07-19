import { api } from "../../api/client";
import { useApp } from "../../app/AppProvider";
import { useAsync } from "../../hooks/useAsync";
import { Card, EmptyState, ErrorState, Loading, Pill } from "../../components/ui";
import { TabHint } from "./parts";
import { STATUS } from "../../theme/tokens";

// Colour the action by what it does: destructive red, sign-off/finish green,
// mail/import amber, everything else neutral.
function actionColor(action: string): string {
  if (action.includes("reset") || action.includes("delete")) return STATUS.blocker;
  if (action.includes("signoff") || action.includes("finish")) return STATUS.pass;
  if (action.includes("nda") || action.includes("remind") || action.includes("import")) return STATUS.concern;
  return "var(--muted)";
}

export function AuditTab() {
  const { isAdmin } = useApp();
  const { data, loading, error, reload } = useAsync(() => api.audit(200), []);

  if (!isAdmin) return <EmptyState title="Administrators only" hint="Only CIO/CTO and CFO can view the audit trail." />;
  if (loading) return <Loading />;
  if (error) return <ErrorState message={error} onRetry={reload} />;
  const events = data ?? [];

  return (
    <div>
      <TabHint>Append-only record of sensitive actions — sign-off, finish, NDA &amp; reminder mail, directory import, role changes and settings.</TabHint>
      {events.length === 0 ? (
        <EmptyState title="No activity yet" hint="Sensitive actions will appear here as they happen." />
      ) : (
        <Card style={{ overflowX: "auto" }}>
          <table style={{ width: "100%", borderCollapse: "collapse", minWidth: 820 }}>
            <thead>
              <tr style={{ background: "var(--panel2)", color: "var(--muted)", fontSize: 11, letterSpacing: 0.8, textAlign: "left" }}>
                <Th>WHEN</Th><Th>ACTOR</Th><Th>ACTION</Th><Th>TARGET</Th><Th>DETAIL</Th>
              </tr>
            </thead>
            <tbody>
              {events.map((e) => (
                <tr key={e.id} style={{ borderTop: "1px solid var(--line)" }}>
                  <Td muted>{new Date(e.utc).toLocaleString()}</Td>
                  <Td>
                    <div style={{ fontWeight: 600 }}>{e.actorName}</div>
                    <div style={{ fontSize: 12, color: "var(--muted)" }}>{e.actorRole}</div>
                  </Td>
                  <Td><Pill color={actionColor(e.action)}>{e.action}</Pill></Td>
                  <Td>{e.targetName ?? e.targetType}</Td>
                  <Td muted>{e.summary}</Td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  );
}

const Th = ({ children }: { children?: React.ReactNode }) => <th style={{ padding: "12px 16px", fontWeight: 700, textTransform: "uppercase" }}>{children}</th>;
const Td = ({ children, muted }: { children: React.ReactNode; muted?: boolean }) => <td style={{ padding: "12px 16px", color: muted ? "var(--muted)" : "var(--text)", fontSize: 14, verticalAlign: "top" }}>{children}</td>;
