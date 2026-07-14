import { NavLink, useLocation } from "react-router-dom";
import { useApp } from "../app/AppProvider";
import { Segmented } from "./Segmented";
import { THEME_LABELS, type ThemeName } from "../theme/tokens";
import type { Role } from "../api/types";

const NAV: Record<string, { to: string; label: string; end?: boolean }[]> = {
  Workspace: [
    { to: "/", label: "Dashboard", end: true },
    { to: "/compare", label: "Compare" },
    { to: "/vendors", label: "Vendors" },
    { to: "/archive", label: "Archive" },
  ],
  Configure: [{ to: "/configuration", label: "Configuration" }],
};

const PAGE_META: Record<string, { title: string; subtitle: string }> = {
  "/": { title: "Dashboard", subtitle: "Portfolio of vendor technical reviews" },
  "/compare": { title: "Compare", subtitle: "Side-by-side vendor matrix" },
  "/vendors": { title: "Vendors", subtitle: "Approved & rejected vendors" },
  "/archive": { title: "Archive", subtitle: "Finished review versions" },
  "/configuration": { title: "Configuration", subtitle: "Categories, sections, policies & entities" },
  "/review": { title: "Review", subtitle: "Vendor technical review editor" },
};

const ROLE_OPTIONS: { value: Role; label: string }[] = [
  { value: "ITManager", label: "IT Manager" },
  { value: "CioCto", label: "CIO / CTO" },
  { value: "Cfo", label: "CFO" },
];

const THEME_ICON: Record<ThemeName, string> = { daylight: "☀", command: "◗", carbon: "●" };

export function Shell({ children }: { children: React.ReactNode }) {
  const { theme, setTheme, role, setRole, me } = useApp();
  const loc = useLocation();
  const key = loc.pathname.startsWith("/review") ? "/review"
    : PAGE_META[loc.pathname] ? loc.pathname : "/";
  const meta = PAGE_META[key];

  return (
    <div style={{ display: "flex", height: "100%", background: "var(--canvas)" }}>
      {/* Fixed dark sidebar — stays dark in every theme */}
      <aside style={{ width: 248, flexShrink: 0, background: "var(--shell)", color: "#eef2ff", display: "flex", flexDirection: "column", height: "100%" }}>
        <div style={{ padding: 16 }}>
          <div style={{ background: "#fff", borderRadius: 12, padding: "14px 16px", textAlign: "center" }}>
            <div style={{ fontFamily: "'Space Grotesk',sans-serif", fontWeight: 700, letterSpacing: 4, color: "#11163A", fontSize: 18 }}>BIRGMA</div>
            <div style={{ fontFamily: "'Space Grotesk',sans-serif", fontWeight: 700, letterSpacing: 1, color: "#0F6CBD", fontSize: 22, marginTop: 2 }}>BILTEMA</div>
          </div>
          <div style={{ marginTop: 14, paddingLeft: 4 }}>
            <span style={{ fontFamily: "'Space Grotesk',sans-serif", fontWeight: 700, fontSize: 17 }}>Vendor Review</span>
            <span style={{ marginLeft: 8, fontSize: 11, letterSpacing: 2, color: "#8ea0cf" }}>ASSESS</span>
          </div>
        </div>

        <nav style={{ padding: "8px 12px", flex: 1, overflowY: "auto" }}>
          {Object.entries(NAV).map(([group, items]) => (
            <div key={group} style={{ marginBottom: 18 }}>
              <div style={{ fontSize: 10, letterSpacing: 2, color: "#6c7fb0", padding: "6px 12px", textTransform: "uppercase" }}>{group}</div>
              {items.map((it) => (
                <NavLink
                  key={it.to}
                  to={it.to}
                  end={it.end}
                  style={({ isActive }) => ({
                    display: "block",
                    padding: "10px 14px",
                    borderRadius: 10,
                    marginBottom: 2,
                    fontSize: 15,
                    fontWeight: 600,
                    color: isActive ? "#fff" : "#c3cdea",
                    background: isActive ? "rgba(255,255,255,.10)" : "transparent",
                  })}
                >
                  {it.label}
                </NavLink>
              ))}
            </div>
          ))}
        </nav>

        {/* Signed-in user chip */}
        <div style={{ padding: 16, borderTop: "1px solid rgba(255,255,255,.08)", display: "flex", alignItems: "center", gap: 10 }}>
          <div style={{ width: 40, height: 40, borderRadius: 999, background: "var(--brandA)", display: "flex", alignItems: "center", justifyContent: "center", fontWeight: 700, fontSize: 13, color: "#fff" }}>
            {roleAbbrev(role)}
          </div>
          <div style={{ overflow: "hidden" }}>
            <div style={{ fontWeight: 700, fontSize: 14, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
              {me?.displayName ?? "Signing in…"}
            </div>
            <div style={{ fontSize: 12, color: "#8ea0cf" }}>{roleLabel(role)}</div>
          </div>
        </div>
      </aside>

      {/* Main column */}
      <div style={{ flex: 1, display: "flex", flexDirection: "column", minWidth: 0 }}>
        <header style={{ background: "var(--panel)", borderBottom: "1px solid var(--line)", padding: "14px 28px", display: "flex", alignItems: "center", gap: 20 }}>
          <div style={{ minWidth: 180 }}>
            <div style={{ fontFamily: "'Space Grotesk',sans-serif", fontWeight: 700, fontSize: 22 }}>{meta.title}</div>
            <div style={{ color: "var(--muted)", fontSize: 13 }}>{meta.subtitle}</div>
          </div>
          <div style={{ flex: 1 }} />
          <Segmented
            value={theme}
            onChange={(t) => setTheme(t)}
            options={(["daylight", "command", "carbon"] as ThemeName[]).map((t) => ({
              value: t, label: <span>{THEME_ICON[t]} {THEME_LABELS[t]}</span>,
            }))}
          />
          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <span style={{ fontSize: 10, letterSpacing: 2, color: "var(--muted)" }}>ROLE</span>
            <Segmented value={role} onChange={(r) => setRole(r)} options={ROLE_OPTIONS} />
          </div>
        </header>

        <main style={{ flex: 1, overflowY: "auto", padding: "28px 32px" }}>{children}</main>
      </div>
    </div>
  );
}

function roleAbbrev(r: Role) { return r === "Cfo" ? "CFO" : r === "CioCto" ? "CIO" : "ITM"; }
function roleLabel(r: Role) { return r === "Cfo" ? "Office of the CFO" : r === "CioCto" ? "CIO / CTO" : "IT Manager"; }
