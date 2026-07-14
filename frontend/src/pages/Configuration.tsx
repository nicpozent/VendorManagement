import { useState } from "react";
import { useApp } from "../app/AppProvider";
import { Card } from "../components/ui";
import { CategoriesTab } from "./config/CategoriesTab";
import { EntitiesTab } from "./config/EntitiesTab";
import { SectionsTab } from "./config/SectionsTab";
import { PoliciesTab } from "./config/PoliciesTab";
import { SettingsTab } from "./config/SettingsTab";
import { DirectoryTab } from "./config/DirectoryTab";

type TabKey = "categories" | "entities" | "sections" | "policies" | "settings" | "directory";

const TABS: { key: TabKey; label: string; adminOnly?: boolean }[] = [
  { key: "categories", label: "Vendor categories" },
  { key: "entities", label: "Entities" },
  { key: "sections", label: "Review sections" },
  { key: "policies", label: "Policy library" },
  { key: "settings", label: "Settings" },
  { key: "directory", label: "Directory", adminOnly: true },
];

export function Configuration() {
  const { isAdmin } = useApp();
  const [tab, setTab] = useState<TabKey>("categories");
  const tabs = TABS.filter((t) => !t.adminOnly || isAdmin);

  return (
    <div>
      <h1 style={{ fontSize: 30, margin: 0 }}>Configuration</h1>
      <div style={{ color: "var(--muted)", marginTop: 4, marginBottom: 20 }}>
        Define vendor categories, the review sections they include, and the weighted policy library. Changes apply to every review.
      </div>

      <Card style={{ display: "inline-flex", gap: 4, padding: 6, marginBottom: 22 }}>
        {tabs.map((t) => {
          const active = t.key === tab;
          return (
            <button
              key={t.key}
              onClick={() => setTab(t.key)}
              style={{
                padding: "10px 18px", borderRadius: 10, border: "none", fontSize: 14, fontWeight: 600,
                background: active ? "var(--brandA)" : "transparent",
                color: active ? "#fff" : "var(--muted)",
              }}
            >
              {t.label}
            </button>
          );
        })}
      </Card>

      {tab === "categories" && <CategoriesTab />}
      {tab === "entities" && <EntitiesTab />}
      {tab === "sections" && <SectionsTab />}
      {tab === "policies" && <PoliciesTab />}
      {tab === "settings" && <SettingsTab />}
      {tab === "directory" && <DirectoryTab />}
    </div>
  );
}
