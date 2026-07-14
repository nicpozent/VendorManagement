import { useApp } from "../app/AppProvider";

export function Toasts() {
  const { toasts } = useApp();
  return (
    <div style={{ position: "fixed", bottom: 24, right: 24, display: "flex", flexDirection: "column", gap: 10, zIndex: 1000 }}>
      {toasts.map((t) => (
        <div
          key={t.id}
          style={{
            background: "var(--shell)",
            color: "#fff",
            padding: "12px 18px",
            borderRadius: 12,
            boxShadow: "0 8px 30px rgba(0,0,0,.25)",
            fontSize: 14,
            maxWidth: 380,
          }}
        >
          {t.message}
        </div>
      ))}
    </div>
  );
}
