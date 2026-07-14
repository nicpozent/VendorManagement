import { useState } from "react";
import { api } from "../api/client";
import { useApp } from "../app/AppProvider";
import { useAsync } from "../hooks/useAsync";
import { Button, Card, EmptyState, ErrorState, Loading, VerdictPill } from "../components/ui";
import { Markdown } from "../components/Markdown";
import type { ArchiveDetail } from "../api/types";

export function Archive() {
  const { toast } = useApp();
  const { data, loading, error, reload } = useAsync(() => api.archive(), []);
  const [detail, setDetail] = useState<ArchiveDetail | null>(null);
  const [opening, setOpening] = useState(false);

  async function open(id: string) {
    setOpening(true);
    try { setDetail(await api.archiveDetail(id)); }
    catch (e) { toast(e instanceof Error ? e.message : "Could not open snapshot"); }
    finally { setOpening(false); }
  }

  if (loading) return <Loading />;
  if (error) return <ErrorState message={error} onRetry={reload} />;
  const items = data ?? [];

  return (
    <div>
      <h1 style={{ fontSize: 30, margin: 0 }}>Archive</h1>
      <div style={{ color: "var(--muted)", marginTop: 4, marginBottom: 24 }}>
        Finished review versions — an immutable record of each completed assessment.
      </div>

      {items.length === 0 ? (
        <EmptyState title="Nothing archived yet" hint="Finish & archive a review to create an immutable snapshot." />
      ) : (
        <Card style={{ overflowX: "auto" }}>
          <table style={{ width: "100%", borderCollapse: "collapse", minWidth: 720 }}>
            <thead>
              <tr style={{ background: "var(--panel2)", color: "var(--muted)", fontSize: 11, letterSpacing: 0.8, textAlign: "left" }}>
                <Th>VENDOR</Th><Th>CATEGORY</Th><Th>OWNER</Th><Th>VERDICT</Th><Th>FINISHED</Th><Th>VER.</Th>
              </tr>
            </thead>
            <tbody>
              {items.map((a) => (
                <tr key={a.id} onClick={() => open(a.id)} style={{ cursor: "pointer", borderTop: "1px solid var(--line)" }}>
                  <Td><b>{a.vendorName}</b></Td>
                  <Td muted>{a.categoryName}</Td>
                  <Td muted>{a.ownerName}</Td>
                  <Td><VerdictPill verdict={a.verdict} /></Td>
                  <Td muted>{new Date(a.finishedOn).toLocaleDateString()}</Td>
                  <Td><b>v{a.version}</b></Td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      {detail && (
        <div onClick={() => setDetail(null)} style={overlay}>
          <div onClick={(e) => e.stopPropagation()} style={{ maxWidth: 720, width: "100%", maxHeight: "88vh", overflow: "auto", borderRadius: 16 }}>
            <div className="no-print" style={{ display: "flex", gap: 8, justifyContent: "flex-end", marginBottom: 10 }}>
              <Button variant="soft" onClick={() => { void navigator.clipboard.writeText(detail.memoMarkdown); toast("Memo copied as Markdown"); }}>Copy .md</Button>
              <Button variant="soft" onClick={() => window.print()}>Print</Button>
              <Button variant="ghost" onClick={() => setDetail(null)}>Close</Button>
            </div>
            <article className="memo-paper" style={{ padding: 40, borderRadius: 16, boxShadow: "0 20px 60px rgba(0,0,0,.35)" }}>
              <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 8 }}>
                <span style={{ fontSize: 11, letterSpacing: 2, color: "#0F6CBD", fontWeight: 700 }}>TECHNICAL REVIEW</span>
                <span style={{ fontSize: 12, color: "#69728A" }}>v{detail.header.version} · {new Date(detail.header.finishedOn).toLocaleDateString()}</span>
              </div>
              <Markdown text={detail.memoMarkdown} />
            </article>
          </div>
        </div>
      )}
      {opening && <Loading label="Opening snapshot…" />}
    </div>
  );
}

const overlay: React.CSSProperties = { position: "fixed", inset: 0, background: "rgba(6,10,25,.55)", display: "flex", alignItems: "flex-start", justifyContent: "center", padding: 40, zIndex: 900 };
const Th = ({ children }: { children?: React.ReactNode }) => <th style={{ padding: "12px 16px", fontWeight: 700, textTransform: "uppercase" }}>{children}</th>;
const Td = ({ children, muted }: { children: React.ReactNode; muted?: boolean }) => <td style={{ padding: "12px 16px", color: muted ? "var(--muted)" : "var(--text)", fontSize: 14 }}>{children}</td>;
