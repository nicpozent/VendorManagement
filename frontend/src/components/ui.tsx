import type { CSSProperties, ReactNode } from "react";
import { STATUS } from "../theme/tokens";
import type { ItemStatus, Nda, ReviewStatus, VerdictName } from "../api/types";

export function Card({ children, style, className }: { children: ReactNode; style?: CSSProperties; className?: string }) {
  return (
    <div
      className={className}
      style={{
        background: "var(--panel)",
        border: "1px solid var(--line)",
        borderRadius: 14,
        ...style,
      }}
    >
      {children}
    </div>
  );
}

/** Soft-filled status pill: translucent tint with the constant semantic colour. */
export function Pill({ color, children, style }: { color: string; children: ReactNode; style?: CSSProperties }) {
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 6,
        padding: "4px 12px",
        borderRadius: 999,
        fontSize: 12,
        fontWeight: 700,
        letterSpacing: 0.3,
        color,
        background: tint(color, 0.14),
        border: `1px solid ${tint(color, 0.28)}`,
        whiteSpace: "nowrap",
        ...style,
      }}
    >
      {children}
    </span>
  );
}

export function Dot({ color, size = 8 }: { color: string; size?: number }) {
  return <span style={{ width: size, height: size, borderRadius: 999, background: color, display: "inline-block" }} />;
}

const ITEM: Record<ItemStatus, { c: string; label: string }> = {
  Pass: { c: STATUS.pass, label: "PASS" },
  Concern: { c: STATUS.concern, label: "CONCERN" },
  Blocker: { c: STATUS.blocker, label: "BLOCKER" },
  NA: { c: "var(--faint)", label: "N/A" },
  Unscored: { c: "var(--faint)", label: "—" },
};
export function ItemStatusPill({ status }: { status: ItemStatus }) {
  const s = ITEM[status];
  return <Pill color={s.c}>{s.label}</Pill>;
}

const REVIEW: Record<ReviewStatus, { c: string; label: string }> = {
  Draft: { c: "var(--faint)", label: "DRAFT" },
  InProgress: { c: STATUS.brand, label: "IN PROGRESS" },
  Concern: { c: STATUS.concern, label: "CONCERN" },
  Approved: { c: STATUS.pass, label: "APPROVED" },
  Rejected: { c: STATUS.blocker, label: "REJECTED" },
  Finished: { c: "var(--muted)", label: "FINISHED" },
};
export function ReviewStatusPill({ status }: { status: ReviewStatus }) {
  const s = REVIEW[status];
  return <Pill color={s.c}>{s.label}</Pill>;
}

const VERDICT: Record<VerdictName, { c: string; label: string }> = {
  Proceed: { c: STATUS.pass, label: "PROCEED" },
  ProceedWithConditions: { c: STATUS.concern, label: "PROCEED WITH CONDITIONS" },
  DoNotProceed: { c: STATUS.blocker, label: "DO NOT PROCEED" },
  InProgress: { c: STATUS.brand, label: "IN PROGRESS" },
};
export function VerdictPill({ verdict }: { verdict: VerdictName }) {
  const s = VERDICT[verdict];
  return <Pill color={s.c}>{s.label}</Pill>;
}

const NDA: Record<Nda, { c: string; label: string }> = {
  Signed: { c: STATUS.pass, label: "NDA SIGNED ✓" },
  Requested: { c: STATUS.concern, label: "NDA PENDING" },
  None: { c: "var(--faint)", label: "NO NDA" },
};
export function NdaPill({ nda }: { nda: Nda }) {
  const s = NDA[nda];
  return <Pill color={s.c}>{s.label}</Pill>;
}

export function Button({ children, onClick, variant = "primary", disabled, style, type }: {
  children: ReactNode; onClick?: () => void; variant?: "primary" | "ghost" | "soft" | "danger" | "success";
  disabled?: boolean; style?: CSSProperties; type?: "button" | "submit";
}) {
  const styles: Record<string, CSSProperties> = {
    primary: { background: "var(--brandA)", color: "#fff", border: "1px solid transparent" },
    success: { background: STATUS.pass, color: "#fff", border: "1px solid transparent" },
    danger: { background: tint(STATUS.blocker, 0.12), color: STATUS.blocker, border: `1px solid ${tint(STATUS.blocker, 0.3)}` },
    soft: { background: "var(--panel2)", color: "var(--text)", border: "1px solid var(--line)" },
    ghost: { background: "transparent", color: "var(--text)", border: "1px solid var(--line)" },
  };
  return (
    <button
      type={type ?? "button"}
      onClick={onClick}
      disabled={disabled}
      style={{
        padding: "9px 16px",
        borderRadius: 10,
        fontSize: 14,
        fontWeight: 600,
        opacity: disabled ? 0.5 : 1,
        ...styles[variant],
        ...style,
      }}
    >
      {children}
    </button>
  );
}

export function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label style={{ display: "flex", flexDirection: "column", gap: 6 }}>
      <span style={{ fontSize: 11, fontWeight: 700, letterSpacing: 0.6, color: "var(--muted)", textTransform: "uppercase" }}>
        {label}
      </span>
      {children}
    </label>
  );
}

export const inputStyle: CSSProperties = {
  padding: "10px 12px",
  borderRadius: 10,
  border: "1px solid var(--line)",
  background: "var(--panel)",
  color: "var(--text)",
  width: "100%",
};

export function Loading({ label = "Loading…" }: { label?: string }) {
  return <div style={{ padding: 40, textAlign: "center", color: "var(--muted)" }}>{label}</div>;
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <Card style={{ padding: 24, borderColor: tint(STATUS.blocker, 0.3) }}>
      <div style={{ color: STATUS.blocker, fontWeight: 700, marginBottom: 6 }}>Something went wrong</div>
      <div style={{ color: "var(--muted)", fontSize: 14, marginBottom: 12, wordBreak: "break-word" }}>{message}</div>
      {onRetry && <Button variant="soft" onClick={onRetry}>Retry</Button>}
    </Card>
  );
}

export function EmptyState({ title, hint }: { title: string; hint?: string }) {
  return (
    <Card style={{ padding: 40, textAlign: "center" }}>
      <div style={{ fontWeight: 700, marginBottom: 6 }}>{title}</div>
      {hint && <div style={{ color: "var(--muted)", fontSize: 14 }}>{hint}</div>}
    </Card>
  );
}

/** Convert a hex or css var colour to a translucent tint background. */
export function tint(color: string, alpha: number): string {
  if (color.startsWith("#")) {
    const h = color.slice(1);
    const r = parseInt(h.slice(0, 2), 16);
    const g = parseInt(h.slice(2, 4), 16);
    const b = parseInt(h.slice(4, 6), 16);
    return `rgba(${r},${g},${b},${alpha})`;
  }
  // CSS var — layer with color-mix for translucency.
  return `color-mix(in srgb, ${color} ${Math.round(alpha * 100)}%, transparent)`;
}
