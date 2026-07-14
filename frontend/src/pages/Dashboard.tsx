import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api/client";
import { useApp } from "../app/AppProvider";
import { useAsync } from "../hooks/useAsync";
import { Button, Card, EmptyState, ErrorState, Loading, ReviewStatusPill } from "../components/ui";
import { STATUS } from "../theme/tokens";
import type { ReviewListItem } from "../api/types";

const KPIS: { key: string; label: string; color: string; count: (r: ReviewListItem[]) => number }[] = [
  { key: "total", label: "TOTAL", color: "var(--brandA)", count: (r) => r.length },
  { key: "inprogress", label: "IN PROGRESS", color: STATUS.brand, count: (r) => r.filter((x) => x.status === "InProgress").length },
  { key: "concern", label: "CONCERN", color: STATUS.concern, count: (r) => r.filter((x) => x.status === "Concern").length },
  { key: "approved", label: "APPROVED", color: STATUS.pass, count: (r) => r.filter((x) => x.status === "Approved").length },
  { key: "rejected", label: "REJECTED", color: STATUS.blocker, count: (r) => r.filter((x) => x.status === "Rejected").length },
  { key: "finished", label: "FINISHED", color: "var(--muted)", count: (r) => r.filter((x) => x.status === "Finished").length },
  { key: "blockers", label: "OPEN BLOCKERS", color: STATUS.blocker, count: (r) => r.reduce((a, x) => a + x.blockers, 0) },
];

export function Dashboard() {
  const { role, entities, categories, toast } = useApp();
  const nav = useNavigate();
  const { data, loading, error, reload } = useAsync(() => api.reviews(), []);
  const [status, setStatus] = useState("");
  const [category, setCategory] = useState("");
  const [entity, setEntity] = useState("");
  const [creating, setCreating] = useState(false);

  const leadership = role !== "ITManager";
  const reviews = data ?? [];

  const filtered = useMemo(() => reviews.filter((r) =>
    (!status || r.status === status) &&
    (!category || r.categoryName === category) &&
    (!entity || r.entityId === entity)
  ), [reviews, status, category, entity]);

  const attention = reviews.find((r) => r.blockers > 0);

  async function newReview() {
    setCreating(true);
    try {
      const r = await api.createReview({
        vendorName: "New vendor",
        productName: "",
        categoryId: categories[0]?.id ?? null,
        entityId: entities[0]?.id ?? null,
      });
      nav(`/review/${r.id}`);
    } catch (e) {
      toast(e instanceof Error ? e.message : "Could not create review");
    } finally {
      setCreating(false);
    }
  }

  if (loading) return <Loading />;
  if (error) return <ErrorState message={error} onRetry={reload} />;

  return (
    <div>
      <div style={{ display: "flex", alignItems: "flex-start", marginBottom: 20 }}>
        <div>
          <h1 style={{ fontSize: 30, margin: 0 }}>{leadership ? "All vendor reviews" : "My vendor reviews"}</h1>
          <div style={{ color: "var(--muted)", marginTop: 4 }}>
            {leadership ? "Portfolio view across all IT managers — full visibility for leadership." : "The vendor reviews assigned to you."}
          </div>
        </div>
        <div style={{ flex: 1 }} />
        <Button onClick={newReview} disabled={creating}>+ New review</Button>
      </div>

      {/* KPI cards */}
      <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 16, marginBottom: 18 }}>
        {KPIS.map((k) => (
          <Card key={k.key} style={{ padding: "16px 18px" }}>
            <div style={{ display: "flex", alignItems: "center", gap: 8, color: "var(--muted)", fontSize: 12, fontWeight: 700, letterSpacing: 0.5 }}>
              <span style={{ width: 8, height: 8, borderRadius: 999, background: k.color }} />
              {k.label}
            </div>
            <div className="num" style={{ fontSize: 34, fontWeight: 700, color: k.color, marginTop: 6 }}>{k.count(reviews)}</div>
          </Card>
        ))}
      </div>

      {/* Needs attention banner */}
      {attention && (
        <Card style={{ padding: "14px 18px", marginBottom: 18, background: "color-mix(in srgb, #DB1A52 8%, var(--panel))", borderColor: "color-mix(in srgb, #DB1A52 30%, var(--line))", display: "flex", alignItems: "center", gap: 14 }}>
          <span style={{ fontSize: 11, fontWeight: 800, letterSpacing: 1, color: STATUS.blocker, border: `1px solid ${STATUS.blocker}`, borderRadius: 999, padding: "4px 12px" }}>NEEDS ATTENTION</span>
          <span style={{ color: STATUS.blocker, fontWeight: 600 }}>
            {attention.vendorName} ({attention.blockers}) — unresolved blocker(s) capping the verdict.
          </span>
        </Card>
      )}

      {/* Filters */}
      <div style={{ display: "flex", alignItems: "center", gap: 18, marginBottom: 12, flexWrap: "wrap" }}>
        <Filter label="Status" value={status} onChange={setStatus} options={[["", "All"], ["Draft", "Draft"], ["InProgress", "In progress"], ["Concern", "Concern"], ["Approved", "Approved"], ["Rejected", "Rejected"], ["Finished", "Finished"]]} />
        <Filter label="Category" value={category} onChange={setCategory} options={[["", "All categories"], ...categories.map((c) => [c.name, c.name] as [string, string])]} />
        <Filter label="Entity" value={entity} onChange={setEntity} options={[["", "All entities"], ...entities.map((e) => [e.id, e.name] as [string, string])]} />
        <div style={{ flex: 1 }} />
        <div style={{ color: "var(--muted)", fontSize: 13 }}>{filtered.length} of {reviews.length} shown</div>
      </div>

      {/* Reviews table */}
      {reviews.length === 0 ? (
        <EmptyState title="No reviews yet" hint="Create your first vendor review to get started." />
      ) : (
        <Card style={{ overflow: "hidden" }}>
          <table style={{ width: "100%", borderCollapse: "collapse" }}>
            <thead>
              <tr style={{ background: "var(--panel2)", color: "var(--muted)", fontSize: 11, letterSpacing: 0.8, textAlign: "left" }}>
                <Th>VENDOR</Th><Th>CATEGORY</Th><Th>OWNER</Th><Th>STATUS</Th>
                <Th center>BLK</Th><Th center>CNC</Th><Th>UPDATED</Th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((r) => (
                <tr key={r.id} onClick={() => nav(`/review/${r.id}`)} style={{ cursor: "pointer", borderTop: "1px solid var(--line)" }}>
                  <Td>
                    <div style={{ fontWeight: 700 }}>{r.vendorName}</div>
                    {r.entityName && <div style={{ fontSize: 12, color: "var(--brandA)", fontWeight: 600 }}>{r.entityName}</div>}
                  </Td>
                  <Td muted>{r.categoryName || "—"}</Td>
                  <Td muted>{r.ownerName}</Td>
                  <Td><ReviewStatusPill status={r.status} /></Td>
                  <Td center><b style={{ color: r.blockers ? STATUS.blocker : "var(--faint)" }}>{r.blockers}</b></Td>
                  <Td center><b style={{ color: r.concerns ? STATUS.concern : "var(--faint)" }}>{r.concerns}</b></Td>
                  <Td muted>{new Date(r.updatedUtc).toLocaleDateString()}</Td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  );
}

function Filter({ label, value, onChange, options }: { label: string; value: string; onChange: (v: string) => void; options: [string, string][] }) {
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
      <span style={{ color: "var(--muted)", fontSize: 13 }}>{label}</span>
      <select value={value} onChange={(e) => onChange(e.target.value)} style={{ padding: "8px 12px", borderRadius: 10, border: "1px solid var(--line)", background: "var(--panel)", color: "var(--text)" }}>
        {options.map(([v, l]) => <option key={v} value={v}>{l}</option>)}
      </select>
    </div>
  );
}

const Th = ({ children, center }: { children: React.ReactNode; center?: boolean }) =>
  <th style={{ padding: "12px 16px", fontWeight: 700, textAlign: center ? "center" : "left", textTransform: "uppercase" }}>{children}</th>;
const Td = ({ children, muted, center }: { children: React.ReactNode; muted?: boolean; center?: boolean }) =>
  <td style={{ padding: "12px 16px", color: muted ? "var(--muted)" : "var(--text)", textAlign: center ? "center" : "left", fontSize: 14 }}>{children}</td>;
