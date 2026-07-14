import type { ReactNode } from "react";
import { STATUS } from "../../theme/tokens";

export function Toggle({ on, onChange, disabled }: { on: boolean; onChange: (v: boolean) => void; disabled?: boolean }) {
  return (
    <button
      onClick={() => !disabled && onChange(!on)}
      disabled={disabled}
      style={{
        width: 62, height: 30, borderRadius: 999, border: "none", position: "relative",
        background: on ? STATUS.pass : "var(--faint)", cursor: disabled ? "default" : "pointer",
        opacity: disabled ? 0.6 : 1, flexShrink: 0,
      }}
      aria-pressed={on}
    >
      <span style={{ position: "absolute", top: 3, left: on ? 35 : 3, width: 24, height: 24, borderRadius: 999, background: "#fff", transition: "left .15s", display: "flex", alignItems: "center", justifyContent: "center", fontSize: 9, fontWeight: 800, color: on ? STATUS.pass : "var(--muted)" }}>
        {on ? "ON" : ""}
      </span>
    </button>
  );
}

export function Badge({ kind }: { kind: "FIXED" | "TEMPLATE" }) {
  const c = kind === "FIXED" ? "var(--brandA)" : STATUS.concern;
  return <span style={{ fontSize: 11, fontWeight: 800, letterSpacing: 1, color: c, background: `color-mix(in srgb, ${c} 14%, transparent)`, borderRadius: 999, padding: "4px 12px" }}>{kind}</span>;
}

export function selectStyle(): React.CSSProperties {
  return { padding: "9px 12px", borderRadius: 10, border: "1px solid var(--line)", background: "var(--panel)", color: "var(--text)" };
}

export function RowInput({ value, onChange, placeholder, disabled }: { value: string; onChange: (v: string) => void; placeholder?: string; disabled?: boolean }) {
  return (
    <input
      value={value}
      placeholder={placeholder}
      disabled={disabled}
      onChange={(e) => onChange(e.target.value)}
      style={{ flex: 1, padding: "11px 14px", borderRadius: 10, border: "1px solid var(--line)", background: disabled ? "var(--panel2)" : "var(--panel)", color: "var(--text)" }}
    />
  );
}

export function TabHint({ children }: { children: ReactNode }) {
  return <div style={{ color: "var(--muted)", marginBottom: 18 }}>{children}</div>;
}
