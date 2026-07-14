// Theme token sets (CLAUDE.md §2). The active theme's variables are written onto
// the app root; every surface/text/border is built from these. Semantic status
// colours stay constant across themes.

export type ThemeName = "daylight" | "command" | "carbon";

export interface Tokens {
  canvas: string;
  panel: string;
  panel2: string;
  line: string;
  text: string;
  muted: string;
  faint: string;
  brandA: string;
  shell: string;
}

export const THEMES: Record<ThemeName, Tokens> = {
  daylight: {
    canvas: "#EEF1F6",
    panel: "#ffffff",
    panel2: "#F1F3F8",
    line: "#E7EBF2",
    text: "#11163A",
    muted: "#69728A",
    faint: "#9aa2b4",
    brandA: "#0F6CBD",
    shell: "#11163A",
  },
  command: {
    canvas: "#0a1024",
    panel: "#111c42",
    panel2: "#0e1738",
    line: "rgba(150,175,255,.14)",
    text: "#eef2ff",
    muted: "#9fb0d8",
    faint: "#6c7fb0",
    brandA: "#2E93E6",
    shell: "#0c1430",
  },
  carbon: {
    canvas: "#0a0c12",
    panel: "#101319",
    panel2: "#0c0f15",
    line: "rgba(255,255,255,.09)",
    text: "#f2f4f8",
    muted: "#9aa3b4",
    faint: "#5e6676",
    brandA: "#3aa0ff",
    shell: "#0b0d14",
  },
};

// Semantic status colours — CONSTANT across all themes.
export const STATUS = {
  pass: "#16A37B", // Pass / Approved / NDA-signed
  concern: "#F2A516", // Concern / Info
  blocker: "#DB1A52", // Blocker / Rejected
  brand: "var(--brandA)", // In-progress / brand
};

export const THEME_LABELS: Record<ThemeName, string> = {
  daylight: "Daylight",
  command: "Command",
  carbon: "Carbon",
};

export function applyTheme(name: ThemeName) {
  const t = THEMES[name];
  const root = document.documentElement;
  root.style.setProperty("--canvas", t.canvas);
  root.style.setProperty("--panel", t.panel);
  root.style.setProperty("--panel2", t.panel2);
  root.style.setProperty("--line", t.line);
  root.style.setProperty("--text", t.text);
  root.style.setProperty("--muted", t.muted);
  root.style.setProperty("--faint", t.faint);
  root.style.setProperty("--brandA", t.brandA);
  root.style.setProperty("--shell", t.shell);
  root.setAttribute("data-theme", name);
}
