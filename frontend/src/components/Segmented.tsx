import type { ReactNode } from "react";

export interface SegOption<T extends string> { value: T; label: ReactNode; }

export function Segmented<T extends string>({ options, value, onChange, dark }: {
  options: SegOption<T>[]; value: T; onChange: (v: T) => void; dark?: boolean;
}) {
  return (
    <div
      style={{
        display: "inline-flex",
        gap: 4,
        padding: 4,
        borderRadius: 12,
        background: dark ? "rgba(255,255,255,.06)" : "var(--panel2)",
        border: "1px solid var(--line)",
      }}
    >
      {options.map((o) => {
        const active = o.value === value;
        return (
          <button
            key={o.value}
            onClick={() => onChange(o.value)}
            style={{
              padding: "7px 14px",
              borderRadius: 9,
              border: "none",
              fontSize: 13,
              fontWeight: 600,
              display: "inline-flex",
              alignItems: "center",
              gap: 6,
              background: active ? "var(--brandA)" : "transparent",
              color: active ? "#fff" : "var(--muted)",
              transition: "background .15s",
            }}
          >
            {o.label}
          </button>
        );
      })}
    </div>
  );
}
