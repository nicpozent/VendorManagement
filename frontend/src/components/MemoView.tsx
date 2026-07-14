import { STATUS } from "../theme/tokens";
import type { ReviewDetail, VerdictName } from "../api/types";

const VERDICT_COLOR: Record<VerdictName, string> = {
  DoNotProceed: STATUS.blocker,
  ProceedWithConditions: STATUS.concern,
  Proceed: STATUS.pass,
  InProgress: "#0F6CBD",
};

// Live, blocker-first memo. Stays light in every theme (printable paper artifact).
export function MemoView({ review }: { review: ReviewDetail }) {
  const v = review.verdict;
  const items = review.sections.flatMap((s) => s.items.map((i) => ({ ...i, section: s.sectionName })));
  const blockers = items.filter((i) => i.status === "Blocker");
  const concerns = items.filter((i) => i.status === "Concern");
  const acceptable = items.filter((i) => i.status === "Pass");
  const open = review.openQuestions.filter((q) => !q.resolved);
  const vc = VERDICT_COLOR[v.verdict];

  return (
    <article className="memo-paper" style={{ padding: 40, borderRadius: 16, boxShadow: "0 10px 40px rgba(0,0,0,.12)", minHeight: "100%" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 18 }}>
        <span style={{ fontSize: 11, letterSpacing: 2, color: "#0F6CBD", fontWeight: 800, lineHeight: 1.3 }}>TECHNICAL<br />REVIEW</span>
        <span style={{ fontFamily: "'Space Grotesk',sans-serif", fontWeight: 800, letterSpacing: 1, color: "#0F6CBD", fontSize: 22 }}>BILTEMA</span>
      </div>

      <h1 style={{ fontFamily: "'Space Grotesk',sans-serif", fontSize: 40, lineHeight: 1.05, margin: "0 0 10px", color: "#11163A" }}>{review.vendorName || "Untitled vendor"}</h1>
      <div style={{ color: "#69728A", fontSize: 15 }}>{review.productName || "—"}</div>
      <div style={{ color: "#9aa2b4", fontSize: 13, marginBottom: 20 }}>
        Ref {review.reviewRef} · {new Date(review.date).toLocaleDateString()}{review.entityName ? ` · ${review.entityName}` : ""}
      </div>

      {/* Verdict block */}
      <div style={{ background: vc, color: "#fff", borderRadius: 12, padding: "16px 20px", marginBottom: 22 }}>
        <div style={{ fontSize: 11, letterSpacing: 2, fontWeight: 800, opacity: 0.9 }}>VERDICT</div>
        <div style={{ fontFamily: "'Space Grotesk',sans-serif", fontSize: 24, fontWeight: 700, lineHeight: 1.15, marginTop: 4 }}>
          {v.verdictLabel} — {v.verdictReason}
        </div>
        <div style={{ fontSize: 13, marginTop: 8, opacity: 0.95 }}>
          Readiness {v.readinessPct}% · {v.blockerCount} blocker(s) · {v.concernCount} concern(s) · {v.passCount} pass
        </div>
      </div>

      <MemoGroup title="Blockers" color={STATUS.blocker} items={blockers} showWhy showUnblock />
      <MemoGroup title="Concerns" color={STATUS.concern} items={concerns} showWhy showUnblock />
      <MemoGroup title="Acceptable" color={STATUS.pass} items={acceptable} />

      {open.length > 0 && (
        <section style={{ marginBottom: 18 }}>
          <H color="#11163A">Open questions</H>
          <ul style={{ margin: "6px 0", paddingLeft: 20, color: "#11163A" }}>
            {open.map((q) => <li key={q.id} style={{ marginBottom: 4 }}>{q.text}</li>)}
          </ul>
        </section>
      )}

      <section style={{ marginBottom: 18 }}>
        <H color="#11163A">Recommendation</H>
        <p style={{ margin: "6px 0", lineHeight: 1.55, color: review.recommendation ? "#11163A" : "#9aa2b4" }}>
          {review.recommendation || "No recommendation recorded yet."}
        </p>
      </section>

      <hr style={{ border: "none", borderTop: "1px solid #E7EBF2", margin: "18px 0 12px" }} />
      <div style={{ fontSize: 13, color: "#69728A" }}>
        Nick [Surname], Senior Manager, Global IT Infrastructure, Birgma International SA
      </div>
    </article>
  );
}

function MemoGroup({ title, color, items, showWhy, showUnblock }: {
  title: string; color: string;
  items: { id: string; label: string; section: string; note: string | null; mitigation: string | null }[];
  showWhy?: boolean; showUnblock?: boolean;
}) {
  if (items.length === 0) return null;
  return (
    <section style={{ marginBottom: 18 }}>
      <H color={color}>{title}</H>
      <div style={{ display: "flex", flexDirection: "column", gap: 10, marginTop: 6 }}>
        {items.map((i) => (
          <div key={i.id}>
            <div style={{ fontWeight: 700, color: "#11163A" }}>
              {i.label} <span style={{ fontWeight: 400, color: "#9aa2b4", fontSize: 13 }}>· {i.section}</span>
            </div>
            {showWhy && i.note && <div style={{ fontSize: 14, color: "#69728A" }}>Why: {i.note}</div>}
            {showUnblock && i.mitigation && <div style={{ fontSize: 14, color: "#69728A" }}>Unblock: {i.mitigation}</div>}
          </div>
        ))}
      </div>
    </section>
  );
}

const H = ({ children, color }: { children: React.ReactNode; color: string }) =>
  <h3 style={{ fontFamily: "'Space Grotesk',sans-serif", fontSize: 15, letterSpacing: 1, textTransform: "uppercase", margin: 0, color }}>{children}</h3>;
