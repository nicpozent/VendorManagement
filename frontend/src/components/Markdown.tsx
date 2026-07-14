import type { ReactNode } from "react";

// Minimal Markdown renderer for memo snapshots (#, ##, **bold**, - lists, ---).
export function Markdown({ text }: { text: string }) {
  const lines = text.split("\n");
  const out: ReactNode[] = [];
  let list: ReactNode[] = [];
  const flush = () => {
    if (list.length) { out.push(<ul key={out.length} style={{ margin: "8px 0", paddingLeft: 22 }}>{list}</ul>); list = []; }
  };

  lines.forEach((raw, i) => {
    const line = raw.replace(/\s+$/, "");
    if (line.startsWith("## ")) { flush(); out.push(<h3 key={i} style={{ fontSize: 16, margin: "18px 0 6px", fontFamily: "'Space Grotesk',sans-serif" }}>{inline(line.slice(3))}</h3>); }
    else if (line.startsWith("# ")) { flush(); out.push(<h2 key={i} style={{ fontSize: 22, margin: "6px 0 4px", fontFamily: "'Space Grotesk',sans-serif" }}>{inline(line.slice(2))}</h2>); }
    else if (line.startsWith("- ")) { list.push(<li key={i} style={{ marginBottom: 4 }}>{inline(line.slice(2))}</li>); }
    else if (line.startsWith("  - ")) { list.push(<li key={i} style={{ marginBottom: 4, listStyle: "circle", marginLeft: 16 }}>{inline(line.slice(4))}</li>); }
    else if (line.trim() === "---") { flush(); out.push(<hr key={i} style={{ border: "none", borderTop: "1px solid #E7EBF2", margin: "14px 0" }} />); }
    else if (line.trim() === "") { flush(); }
    else { flush(); out.push(<p key={i} style={{ margin: "4px 0", lineHeight: 1.5 }}>{inline(line)}</p>); }
  });
  flush();
  return <div>{out}</div>;
}

function inline(text: string): ReactNode {
  const parts = text.split(/(\*\*[^*]+\*\*|_[^_]+_)/g);
  return parts.map((p, i) => {
    if (p.startsWith("**") && p.endsWith("**")) return <strong key={i}>{p.slice(2, -2)}</strong>;
    if (p.startsWith("_") && p.endsWith("_")) return <em key={i}>{p.slice(1, -1)}</em>;
    return <span key={i}>{p}</span>;
  });
}
