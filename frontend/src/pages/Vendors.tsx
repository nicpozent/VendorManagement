import { useState } from "react";
import { api } from "../api/client";
import { useApp } from "../app/AppProvider";
import { useAsync } from "../hooks/useAsync";
import { Button, Card, EmptyState, ErrorState, Loading, NdaPill } from "../components/ui";
import { STATUS } from "../theme/tokens";
import type { Vendor } from "../api/types";

export function Vendors() {
  const { toast } = useApp();
  const { data, loading, error, reload } = useAsync(() => api.vendors(), []);
  const [sending, setSending] = useState<string | null>(null);

  if (loading) return <Loading />;
  if (error) return <ErrorState message={error} onRetry={reload} />;

  const vendors = data ?? [];
  const approved = vendors.filter((v) => v.status === "Approved");
  const rejected = vendors.filter((v) => v.status === "Rejected");

  async function sendNda(v: Vendor) {
    setSending(v.id);
    try {
      const res = await api.sendNda(v.id);
      toast(`${res.message} · cc ${res.ccTo}`);
      reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Send NDA failed");
    } finally {
      setSending(null);
    }
  }

  const fmt = (d: string | null) => (d ? new Date(d).toLocaleDateString() : "—");

  return (
    <div>
      <h1 style={{ fontSize: 30, margin: 0 }}>Vendors</h1>
      <div style={{ color: "var(--muted)", marginTop: 4, marginBottom: 24 }}>
        Approved and rejected vendors with contact details and NDA status. Visible to IT Managers, CFO, CTO and CIO.
      </div>

      <SectionTitle color={STATUS.pass}>Approved vendors</SectionTitle>
      {approved.length === 0 ? (
        <EmptyState title="No approved vendors yet" />
      ) : (
        <Card style={{ overflowX: "auto", marginBottom: 32 }}>
          <table style={{ width: "100%", borderCollapse: "collapse", minWidth: 720 }}>
            <thead>
              <tr style={head}>
                <Th>VENDOR</Th><Th>CATEGORY</Th><Th>CONTACT</Th><Th>LAST REVIEW</Th><Th>NDA</Th><Th></Th>
              </tr>
            </thead>
            <tbody>
              {approved.map((v) => (
                <tr key={v.id} style={{ borderTop: "1px solid var(--line)" }}>
                  <Td>
                    <div style={{ fontWeight: 700 }}>{v.name}</div>
                    {v.ownerName && <div style={{ fontSize: 12, color: "var(--muted)" }}>{v.ownerName}</div>}
                  </Td>
                  <Td muted>{v.category}</Td>
                  <Td>
                    <div>{v.contactName}</div>
                    <div style={{ fontSize: 12, color: "var(--muted)" }}>{v.contactEmail}</div>
                  </Td>
                  <Td muted>{fmt(v.lastReview)}</Td>
                  <Td><NdaPill nda={v.nda} /></Td>
                  <Td>
                    <Button variant="soft" disabled={sending === v.id} onClick={() => sendNda(v)}>
                      {sending === v.id ? "Sending…" : "Send NDA"}
                    </Button>
                  </Td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      <SectionTitle color={STATUS.blocker}>Rejected vendors</SectionTitle>
      {rejected.length === 0 ? (
        <EmptyState title="No rejected vendors" />
      ) : (
        <Card style={{ overflowX: "auto" }}>
          <table style={{ width: "100%", borderCollapse: "collapse", minWidth: 720 }}>
            <thead>
              <tr style={head}>
                <Th>VENDOR</Th><Th>CATEGORY</Th><Th>REJECTED</Th><Th>REASON</Th>
              </tr>
            </thead>
            <tbody>
              {rejected.map((v) => (
                <tr key={v.id} style={{ borderTop: "1px solid var(--line)" }}>
                  <Td><div style={{ fontWeight: 700 }}>{v.name}</div></Td>
                  <Td muted>{v.category}</Td>
                  <Td muted>{fmt(v.rejectedOn)}</Td>
                  <Td muted>{v.rejectedReason}</Td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  );
}

const head: React.CSSProperties = { background: "var(--panel2)", color: "var(--muted)", fontSize: 11, letterSpacing: 0.8, textAlign: "left" };
const SectionTitle = ({ children, color }: { children: React.ReactNode; color: string }) => (
  <div style={{ display: "flex", alignItems: "center", gap: 10, margin: "0 0 12px" }}>
    <span style={{ width: 10, height: 10, borderRadius: 999, background: color }} />
    <h2 style={{ fontSize: 20, margin: 0 }}>{children}</h2>
  </div>
);
const Th = ({ children }: { children?: React.ReactNode }) => <th style={{ padding: "12px 16px", fontWeight: 700, textTransform: "uppercase" }}>{children}</th>;
const Td = ({ children, muted }: { children: React.ReactNode; muted?: boolean }) => <td style={{ padding: "12px 16px", color: muted ? "var(--muted)" : "var(--text)", fontSize: 14 }}>{children}</td>;
