import { useEffect, useState } from "react";
import { api } from "../api/client";
import { useApp } from "../app/AppProvider";
import { useAsync } from "../hooks/useAsync";
import { Card, EmptyState, ErrorState, ItemStatusPill, Loading, Pill } from "../components/ui";
import { STATUS } from "../theme/tokens";
import type { ReviewStatus } from "../api/types";

const STATUS_COLOR: Record<string, string> = {
  Draft: "var(--faint)", InProgress: STATUS.brand, Concern: STATUS.concern,
  Approved: STATUS.pass, Rejected: STATUS.blocker, Finished: "var(--muted)",
};
const STATUS_LABEL: Record<string, string> = {
  Draft: "DRAFT", InProgress: "IN PROGRESS", Concern: "CONCERN",
  Approved: "APPROVED", Rejected: "REJECTED", Finished: "FINISHED",
};

export function Compare() {
  const { categories } = useApp();
  const [categoryId, setCategoryId] = useState<string>("");

  useEffect(() => {
    if (!categoryId && categories.length) setCategoryId(categories[0].id);
  }, [categories, categoryId]);

  const { data, loading, error, reload } = useAsync(
    () => (categoryId ? api.compare(categoryId) : Promise.resolve(null)),
    [categoryId],
  );

  return (
    <div>
      <h1 style={{ fontSize: 30, margin: 0 }}>Compare vendors</h1>
      <div style={{ color: "var(--muted)", marginTop: 4, marginBottom: 20 }}>
        Side-by-side, blocker-first. Same category only — dimensions are pulled from each vendor's assessment.
      </div>

      <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 16 }}>
        <span style={{ color: "var(--muted)", fontSize: 13 }}>Category</span>
        <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} style={{ padding: "8px 12px", borderRadius: 10, border: "1px solid var(--line)", background: "var(--panel)", color: "var(--text)", minWidth: 220 }}>
          {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
      </div>

      {loading && <Loading />}
      {error && <ErrorState message={error} onRetry={reload} />}
      {data && data.columns.length === 0 && <EmptyState title="No vendors in this category" hint="Reviews in this category will appear as columns here." />}

      {data && data.columns.length > 0 && (
        <Card style={{ overflowX: "auto" }}>
          <table style={{ borderCollapse: "collapse", width: "100%", minWidth: 640 }}>
            <thead>
              <tr style={{ background: "var(--shell)", color: "#eef2ff" }}>
                <th style={{ ...cell, textAlign: "left", position: "sticky", left: 0, background: "var(--shell)", minWidth: 240 }}>VENDOR</th>
                {data.columns.map((c) => (
                  <th key={c.reviewId} style={{ ...cell, textAlign: "left", minWidth: 150, fontFamily: "'Space Grotesk',sans-serif" }}>{c.vendorName}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              <tr style={{ borderTop: "1px solid var(--line)", background: "var(--panel2)" }}>
                <td style={{ ...cell, textAlign: "left", fontWeight: 700, position: "sticky", left: 0, background: "var(--panel2)" }}>Verdict</td>
                {data.columns.map((c) => (
                  <td key={c.reviewId} style={cell}>
                    <Pill color={STATUS_COLOR[c.verdict] ?? "var(--muted)"}>{STATUS_LABEL[c.verdict] ?? (c.verdict as ReviewStatus)}</Pill>
                  </td>
                ))}
              </tr>
              <tr style={{ borderTop: "1px solid var(--line)" }}>
                <td style={{ ...cell, textAlign: "left", fontWeight: 700, position: "sticky", left: 0, background: "var(--panel)" }}>Readiness</td>
                {data.columns.map((c) => (
                  <td key={c.reviewId} style={{ ...cell, fontWeight: 700, color: "var(--brandA)" }}>{c.readinessPct}%</td>
                ))}
              </tr>
              {data.rows.map((row, i) => (
                <tr key={i} style={{ borderTop: "1px solid var(--line)" }}>
                  <td style={{ ...cell, textAlign: "left", position: "sticky", left: 0, background: "var(--panel)" }}>
                    <div style={{ fontWeight: 600 }}>{row.itemLabel}</div>
                    <div style={{ fontSize: 12, color: "var(--muted)" }}>{row.sectionName}</div>
                  </td>
                  {row.cells.map((cellData, j) => (
                    <td key={j} style={cell}><ItemStatusPill status={cellData.status} /></td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  );
}

const cell: React.CSSProperties = { padding: "14px 16px", textAlign: "center", fontSize: 13, verticalAlign: "middle" };
