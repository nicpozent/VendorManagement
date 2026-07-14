import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api } from "../api/client";
import { useApp } from "../app/AppProvider";
import { Button, Card, ErrorState, Field, Loading, NdaPill, inputStyle } from "../components/ui";
import { MemoView } from "../components/MemoView";
import { STATUS } from "../theme/tokens";
import type {
  ItemStatus, Nda, ReviewDetail, ReviewSectionScore, ReviewStatus, Section,
} from "../api/types";

const SCORES: { value: ItemStatus; label: string; color: string }[] = [
  { value: "Pass", label: "Pass", color: STATUS.pass },
  { value: "Concern", label: "Concern", color: STATUS.concern },
  { value: "Blocker", label: "Blocker", color: STATUS.blocker },
  { value: "NA", label: "N/A", color: "var(--faint)" },
];

const STATUS_OPTS: ReviewStatus[] = ["Draft", "InProgress", "Concern", "Approved", "Rejected", "Finished"];
const STATUS_LABEL: Record<ReviewStatus, string> = {
  Draft: "Draft", InProgress: "In progress", Concern: "Concern",
  Approved: "Approved", Rejected: "Rejected", Finished: "Finished",
};

export function ReviewEditor() {
  const { id = "" } = useParams();
  const nav = useNavigate();
  const { categories, entities, isAdmin, toast } = useApp();
  const includedIdsFor = useCallback(
    (catId: string | null) => (catId ? categories.find((c) => c.id === catId)?.includedSectionIds ?? null : null),
    [categories],
  );

  const [review, setReview] = useState<ReviewDetail | null>(null);
  const [catalog, setCatalog] = useState<Section[]>([]);
  const [sections, setSections] = useState<ReviewSectionScore[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  // Load review + section catalog.
  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    Promise.all([api.review(id), api.sections()])
      .then(([r, secs]) => {
        if (cancelled) return;
        setCatalog(secs);
        setReview(r);
        setSections(buildSections(r, secs, includedIdsFor(r.categoryId)));
      })
      .catch((e: unknown) => !cancelled && setError(e instanceof Error ? e.message : String(e)))
      .finally(() => !cancelled && setLoading(false));
    return () => { cancelled = true; };
  }, [id]);

  // Persist the whole editable payload and refresh the server-computed verdict.
  const persist = useCallback(async (overrides?: Partial<ReviewDetail>, secOverride?: ReviewSectionScore[]) => {
    if (!review) return;
    const merged = { ...review, ...overrides };
    const secs = secOverride ?? sections;
    setSaving(true);
    try {
      const resp = await api.updateReview(id, {
        vendorName: merged.vendorName, productName: merged.productName,
        categoryId: merged.categoryId, entityId: merged.entityId, ownerName: merged.ownerName,
        reviewRef: merged.reviewRef, date: merged.date, rawPitch: merged.rawPitch,
        ndaContactName: merged.ndaContactName, ndaContactEmail: merged.ndaContactEmail,
        nda: merged.nda, recommendation: merged.recommendation, status: merged.status,
        openQuestions: merged.openQuestions.map((q) => ({ id: q.id, text: q.text, resolved: q.resolved })),
        sections: secs.map((s) => ({
          id: s.id, sectionId: s.sectionId, sectionName: s.sectionName,
          items: s.items.map((i) => ({
            id: i.id, sectionItemId: i.sectionItemId, label: i.label, weight: i.weight,
            status: i.status, note: i.note, mitigation: i.mitigation,
          })),
        })),
      } as Partial<ReviewDetail> & { sections: unknown; openQuestions: unknown });
      // Keep local editable arrays; adopt server verdict + metadata baseline.
      setReview((prev) => (prev ? { ...prev, ...overrides, verdict: resp.verdict, reviewRef: resp.reviewRef, updatedUtc: resp.updatedUtc } : resp));
    } catch (e) {
      toast(e instanceof Error ? e.message : "Save failed");
    } finally {
      setSaving(false);
    }
  }, [review, sections, id, toast]);

  // Debounced save for free-text fields.
  const timer = useRef<number | null>(null);
  const scheduleSave = useCallback((overrides?: Partial<ReviewDetail>) => {
    if (timer.current) window.clearTimeout(timer.current);
    timer.current = window.setTimeout(() => void persist(overrides), 500);
  }, [persist]);

  const set = useCallback(<K extends keyof ReviewDetail>(key: K, value: ReviewDetail[K], immediate = false) => {
    setReview((r) => (r ? { ...r, [key]: value } : r));
    if (immediate) void persist({ [key]: value } as Partial<ReviewDetail>);
    else scheduleSave({ [key]: value } as Partial<ReviewDetail>);
  }, [persist, scheduleSave]);

  function changeCategory(categoryId: string) {
    if (!review) return;
    const cat = categories.find((c) => c.id === categoryId);
    const next = buildSections({ ...review, categoryId, sections }, catalog, cat?.includedSectionIds ?? null);
    setSections(next);
    setReview((r) => (r ? { ...r, categoryId, categoryName: cat?.name ?? "" } : r));
    void persist({ categoryId, categoryName: cat?.name ?? "" }, next);
  }

  function score(sectionId: string, itemId: string, status: ItemStatus) {
    const next = sections.map((s) => s.sectionId !== sectionId ? s : {
      ...s, items: s.items.map((i) => (i.id === itemId ? { ...i, status } : i)),
    });
    setSections(next);
    void persist(undefined, next);
  }

  function editItemText(sectionId: string, itemId: string, field: "note" | "mitigation", value: string, save: boolean) {
    const next = sections.map((s) => s.sectionId !== sectionId ? s : {
      ...s, items: s.items.map((i) => (i.id === itemId ? { ...i, [field]: value } : i)),
    });
    setSections(next);
    if (save) void persist(undefined, next);
  }

  async function runScan() {
    if (!review) return;
    try {
      const r = await api.scan(id);
      const patch: Partial<ReviewDetail> = {};
      for (const s of r.suggestions) {
        if (s.field === "productName" && !review.productName) patch.productName = s.value;
        if (s.field === "ndaContactEmail" && !review.ndaContactEmail) patch.ndaContactEmail = s.value;
        if (s.field === "categoryName") {
          const cat = categories.find((c) => c.name === s.value);
          if (cat && !review.categoryId) { patch.categoryId = cat.id; patch.categoryName = cat.name; }
        }
      }
      setReview((prev) => (prev ? { ...prev, ...patch } : prev));
      await persist(patch);
      toast(r.detectedSignals.length ? `Scan found: ${r.detectedSignals.join(", ")}` : "Scan complete — no strong signals");
    } catch (e) {
      toast(e instanceof Error ? e.message : "Scan failed");
    }
  }

  function addQuestion() {
    if (!review) return;
    const q = { id: crypto.randomUUID(), text: "", resolved: false };
    void persist({ openQuestions: [...review.openQuestions, q] });
    setReview((r) => (r ? { ...r, openQuestions: [...r.openQuestions, q] } : r));
  }

  async function exportMemo(download: boolean) {
    const md = await api.memo(id);
    if (download) {
      const blob = new Blob([md], { type: "text/markdown" });
      triggerDownload(blob, `${review?.vendorName ?? "review"}.md`);
    } else {
      await navigator.clipboard.writeText(md);
      toast("Memo copied as Markdown");
    }
  }

  function exportExcel() {
    const rows = [["Section", "Item", "Weight", "Status", "Why", "Unblock/Mitigation"]];
    for (const s of sections)
      for (const i of s.items)
        rows.push([s.sectionName, i.label, i.weight, i.status, i.note ?? "", i.mitigation ?? ""]);
    const csv = rows.map((r) => r.map((c) => `"${c.replace(/"/g, '""')}"`).join(",")).join("\n");
    triggerDownload(new Blob([csv], { type: "text/csv" }), `${review?.vendorName ?? "review"}.csv`);
  }

  async function finish() {
    try { await api.finish(id); toast("Finished & archived"); nav("/archive"); }
    catch (e) { toast(e instanceof Error ? e.message : "Finish failed"); }
  }
  async function remind() {
    try { const r = await api.remind(id); toast(r.mock ? `Reminder queued (mock) to ${r.to.length} recipient(s)` : `Reminder sent to ${r.to.length} recipient(s)`); }
    catch (e) { toast(e instanceof Error ? e.message : "Reminder failed"); }
  }

  const memoReview = useMemo<ReviewDetail | null>(
    () => (review ? { ...review, sections } : null),
    [review, sections],
  );

  if (loading) return <Loading />;
  if (error || !review || !memoReview) return <ErrorState message={error ?? "Review not found"} />;

  return (
    <div style={{ display: "grid", gridTemplateColumns: "minmax(0,1fr) minmax(0,1fr)", gap: 20, height: "calc(100vh - 132px)" }}>
      {/* LEFT — intake + scoring */}
      <div className="no-print" style={{ overflowY: "auto", paddingRight: 6 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 14, marginBottom: 12 }}>
          <Button variant="soft" onClick={() => nav("/")}>← Dashboard</Button>
          <div style={{ fontFamily: "'Space Grotesk',sans-serif", fontSize: 20, fontWeight: 700 }}>{review.vendorName}</div>
          <div style={{ flex: 1 }} />
          <span style={{ fontSize: 12, color: "var(--muted)" }}>{saving ? "Saving…" : "Saved"}</span>
        </div>

        <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 12 }}>
          <span style={{ fontSize: 12, color: "var(--muted)" }}>Status</span>
          <select value={review.status} onChange={(e) => set("status", e.target.value as ReviewStatus, true)} style={{ ...inputStyle, width: "auto" }}>
            {STATUS_OPTS.map((s) => <option key={s} value={s}>{STATUS_LABEL[s]}</option>)}
          </select>
          <div style={{ flex: 1 }} />
          <Button variant="soft" onClick={remind}>Remind approvers</Button>
        </div>

        {/* Export toolbar */}
        <div style={{ display: "flex", gap: 8, marginBottom: 16, flexWrap: "wrap" }}>
          <Button variant="soft" onClick={() => void exportMemo(false)}>Copy .md</Button>
          <Button variant="soft" onClick={() => void exportMemo(true)}>.md</Button>
          <Button variant="soft" onClick={exportExcel}>Excel</Button>
          <Button variant="soft" onClick={() => window.print()}>Print</Button>
          <Button variant="success" onClick={finish}>Finish &amp; archive</Button>
        </div>

        {/* Metadata */}
        <Card style={{ padding: 18, marginBottom: 16 }}>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 14 }}>
            <Field label="Vendor"><input style={inputStyle} value={review.vendorName} onChange={(e) => set("vendorName", e.target.value)} /></Field>
            <Field label="Product / proposal"><input style={inputStyle} value={review.productName} onChange={(e) => set("productName", e.target.value)} /></Field>
            <Field label="Vendor category">
              <select style={inputStyle} value={review.categoryId ?? ""} onChange={(e) => changeCategory(e.target.value)}>
                <option value="">— Select category —</option>
                {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </Field>
            <Field label="Entity">
              <select style={inputStyle} value={review.entityId ?? ""} onChange={(e) => set("entityId", e.target.value || null, true)}>
                <option value="">— Select entity —</option>
                {entities.map((en) => <option key={en.id} value={en.id}>{en.name}</option>)}
              </select>
            </Field>
            <Field label="Owner"><input style={inputStyle} value={review.ownerName} onChange={(e) => set("ownerName", e.target.value)} /></Field>
            <Field label="Review ref"><input style={inputStyle} value={review.reviewRef} onChange={(e) => set("reviewRef", e.target.value)} /></Field>
            <Field label="Date"><input type="date" style={inputStyle} value={review.date.slice(0, 10)} onChange={(e) => set("date", e.target.value, true)} /></Field>
          </div>
        </Card>

        {/* Vendor info & NDA */}
        <Card style={{ padding: 18, marginBottom: 16 }}>
          <div style={{ display: "flex", alignItems: "center", marginBottom: 14 }}>
            <div style={{ fontFamily: "'Space Grotesk',sans-serif", fontWeight: 700, fontSize: 16 }}>Vendor information &amp; NDA</div>
            <div style={{ flex: 1 }} />
            <select value={review.nda} onChange={(e) => set("nda", e.target.value as Nda, true)} style={{ ...inputStyle, width: "auto", marginRight: 10 }}>
              <option value="None">No NDA</option>
              <option value="Requested">NDA pending</option>
              <option value="Signed">NDA signed</option>
            </select>
            <NdaPill nda={review.nda} />
          </div>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 14 }}>
            <Field label="Contact name"><input style={inputStyle} value={review.ndaContactName ?? ""} onChange={(e) => set("ndaContactName", e.target.value)} /></Field>
            <Field label="Contact email"><input style={inputStyle} value={review.ndaContactEmail ?? ""} onChange={(e) => set("ndaContactEmail", e.target.value)} /></Field>
          </div>
        </Card>

        {/* Raw pitch + scan */}
        <Card style={{ padding: 18, marginBottom: 16 }}>
          <div style={{ display: "flex", alignItems: "center", marginBottom: 10 }}>
            <div style={{ fontFamily: "'Space Grotesk',sans-serif", fontWeight: 700, fontSize: 16 }}>Raw pitch</div>
            <div style={{ flex: 1 }} />
            <Button onClick={runScan}>Scan &amp; suggest</Button>
          </div>
          <textarea
            value={review.rawPitch}
            onChange={(e) => set("rawPitch", e.target.value)}
            placeholder="Paste the vendor's pitch, proposal or email here, then Scan & suggest…"
            style={{ ...inputStyle, minHeight: 110, resize: "vertical" }}
          />
        </Card>

        {/* Completeness checks */}
        <Card style={{ padding: 18, marginBottom: 16 }}>
          <div style={{ fontFamily: "'Space Grotesk',sans-serif", fontWeight: 700, fontSize: 16, marginBottom: 10 }}>Completeness checks</div>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8 }}>
            {review.verdict.completeness.map((c) => (
              <div key={c.key} style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13 }}>
                <span style={{ color: c.ok ? STATUS.pass : "var(--faint)" }}>{c.ok ? "✓" : "○"}</span>
                <span style={{ color: c.ok ? "var(--text)" : "var(--muted)" }}>{c.label}</span>
              </div>
            ))}
          </div>
        </Card>

        {/* Scoring sections */}
        {sections.length === 0 ? (
          <Card style={{ padding: 18, color: "var(--muted)" }}>Select a vendor category to load its review sections.</Card>
        ) : sections.map((s) => (
          <Card key={s.sectionId} style={{ padding: 18, marginBottom: 16 }}>
            <div style={{ fontFamily: "'Space Grotesk',sans-serif", fontWeight: 700, fontSize: 16, marginBottom: 12 }}>{s.sectionName}</div>
            <div style={{ display: "flex", flexDirection: "column", gap: 14 }}>
              {s.items.map((i) => (
                <div key={i.id} style={{ borderTop: "1px solid var(--line)", paddingTop: 12 }}>
                  <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
                    <div style={{ fontWeight: 600, flex: 1 }}>
                      {i.label} <span style={{ fontSize: 11, color: "var(--faint)" }}>· {i.weight}</span>
                    </div>
                    {SCORES.map((sc) => {
                      const active = i.status === sc.value;
                      return (
                        <button key={sc.value} onClick={() => score(s.sectionId, i.id, sc.value)}
                          style={{
                            padding: "6px 12px", borderRadius: 8, fontSize: 12, fontWeight: 700,
                            border: `1px solid ${active ? sc.color : "var(--line)"}`,
                            background: active ? sc.color : "transparent",
                            color: active ? "#fff" : "var(--muted)",
                          }}>{sc.label}</button>
                      );
                    })}
                  </div>
                  {(i.status === "Concern" || i.status === "Blocker") && (
                    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10, marginTop: 10 }}>
                      <textarea placeholder="Why?" value={i.note ?? ""}
                        onChange={(e) => editItemText(s.sectionId, i.id, "note", e.target.value, false)}
                        onBlur={(e) => editItemText(s.sectionId, i.id, "note", e.target.value, true)}
                        style={{ ...inputStyle, minHeight: 54, resize: "vertical" }} />
                      <textarea placeholder="Unblock / mitigation" value={i.mitigation ?? ""}
                        onChange={(e) => editItemText(s.sectionId, i.id, "mitigation", e.target.value, false)}
                        onBlur={(e) => editItemText(s.sectionId, i.id, "mitigation", e.target.value, true)}
                        style={{ ...inputStyle, minHeight: 54, resize: "vertical" }} />
                    </div>
                  )}
                </div>
              ))}
            </div>
          </Card>
        ))}

        {/* Open questions + recommendation */}
        <Card style={{ padding: 18, marginBottom: 16 }}>
          <div style={{ display: "flex", alignItems: "center", marginBottom: 10 }}>
            <div style={{ fontFamily: "'Space Grotesk',sans-serif", fontWeight: 700, fontSize: 16 }}>Open questions</div>
            <div style={{ flex: 1 }} />
            <Button variant="ghost" onClick={addQuestion}>+ Add question</Button>
          </div>
          {review.openQuestions.length === 0 && <div style={{ color: "var(--muted)", fontSize: 13 }}>No open questions.</div>}
          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            {review.openQuestions.map((q, idx) => (
              <input key={q.id} style={inputStyle} value={q.text} placeholder="Question…"
                onChange={(e) => { const qs = review.openQuestions.map((x, j) => (j === idx ? { ...x, text: e.target.value } : x)); setReview((r) => (r ? { ...r, openQuestions: qs } : r)); }}
                onBlur={() => void persist()} />
            ))}
          </div>
        </Card>

        <Card style={{ padding: 18, marginBottom: 24 }}>
          <div style={{ fontFamily: "'Space Grotesk',sans-serif", fontWeight: 700, fontSize: 16, marginBottom: 10 }}>Recommendation</div>
          <textarea value={review.recommendation ?? ""} onChange={(e) => set("recommendation", e.target.value)}
            placeholder="Your recommendation…" style={{ ...inputStyle, minHeight: 80, resize: "vertical" }} />
          {!isAdmin && <div style={{ fontSize: 12, color: "var(--faint)", marginTop: 8 }}>Verdict is computed by the platform. Leadership signs off.</div>}
        </Card>
      </div>

      {/* RIGHT — live memo */}
      <div style={{ overflowY: "auto" }}>
        <MemoView review={memoReview} />
      </div>
    </div>
  );
}

// Merge the category's catalog sections with any existing scores on the review, so
// unscored rubric items still appear for scoring. `includedIds` is the category's
// includedSectionIds (from the catalog); null falls back to sections already scored.
function buildSections(review: ReviewDetail, catalog: Section[], includedIds: string[] | null): ReviewSectionScore[] {
  const existing = review.sections;
  const wanted = includedIds && includedIds.length
    ? catalog.filter((s) => includedIds.includes(s.id))
    : catalog.filter((s) => existing.some((e) => e.sectionId === s.id));
  if (wanted.length === 0) return existing;
  return wanted.map((sec) => {
    const ex = existing.find((s) => s.sectionId === sec.id);
    return {
      id: ex?.id ?? crypto.randomUUID(),
      sectionId: sec.id,
      sectionName: sec.name,
      items: sec.items.map((ci) => {
        const exi = ex?.items.find((i) => i.sectionItemId === ci.id);
        return exi ?? {
          id: crypto.randomUUID(), sectionItemId: ci.id, label: ci.label,
          weight: ci.weight, status: "Unscored" as ItemStatus, note: null, mitigation: null,
        };
      }),
    };
  });
}

function triggerDownload(blob: Blob, name: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url; a.download = name; a.click();
  URL.revokeObjectURL(url);
}
