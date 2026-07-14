import { useEffect, useState } from "react";
import { api } from "../../api/client";
import { useApp } from "../../app/AppProvider";
import { useAsync } from "../../hooks/useAsync";
import { Button, Card, ErrorState, Loading } from "../../components/ui";
import { Badge, RowInput, TabHint, selectStyle } from "./parts";
import type { Section, SectionItem } from "../../api/types";

const WEIGHTS: SectionItem["weight"][] = ["High", "Med", "Low"];

export function SectionsTab() {
  const { isAdmin, toast, refreshReference } = useApp();
  const { data, loading, error, reload } = useAsync(() => api.sections(), []);
  const [sections, setSections] = useState<Section[]>([]);

  useEffect(() => {
    if (data) setSections(data);
  }, [data]);

  if (loading) return <Loading />;
  if (error) return <ErrorState message={error} onRetry={reload} />;

  function patchSection(id: string, patch: Partial<Section>) {
    setSections((prev) => prev.map((s) => (s.id === id ? { ...s, ...patch } : s)));
  }
  function patchItem(sectionId: string, itemId: string, patch: Partial<SectionItem>) {
    setSections((prev) =>
      prev.map((s) =>
        s.id === sectionId
          ? { ...s, items: s.items.map((it) => (it.id === itemId ? { ...it, ...patch } : it)) }
          : s,
      ),
    );
  }
  function addItem(sectionId: string) {
    const item: SectionItem = { id: crypto.randomUUID(), label: "", weight: "Med", selectable: false };
    setSections((prev) => prev.map((s) => (s.id === sectionId ? { ...s, items: [...s.items, item] } : s)));
  }
  function removeItem(sectionId: string, itemId: string) {
    setSections((prev) =>
      prev.map((s) => (s.id === sectionId ? { ...s, items: s.items.filter((it) => it.id !== itemId) } : s)),
    );
  }

  async function save(section: Section) {
    try {
      await api.saveSection({
        id: section.id,
        name: section.name,
        kind: section.kind,
        items: section.items.map((it) => ({ id: it.id, label: it.label, weight: it.weight, selectable: it.selectable })),
      });
      toast("Section saved");
      await refreshReference();
      reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Save failed");
    }
  }
  async function remove(id: string) {
    try {
      await api.deleteSection(id);
      toast("Section removed");
      await refreshReference();
      reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Remove failed");
    }
  }
  async function addSection() {
    try {
      await api.saveSection({ name: "New section", kind: "Fixed", items: [] });
      toast("Section added");
      await refreshReference();
      reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Add failed");
    }
  }

  return (
    <div>
      <TabHint>
        Review sections and their weighted items. Fixed sections always apply; template sections offer selectable options.
        {!isAdmin && " (read-only — administrators can edit)"}
      </TabHint>

      {sections.map((section) => (
        <Card key={section.id} style={{ padding: 18, marginBottom: 16 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 12, marginBottom: 14 }}>
            <RowInput
              value={section.name}
              disabled={!isAdmin}
              onChange={(v) => patchSection(section.id, { name: v })}
            />
            <Badge kind={section.kind === "Fixed" ? "FIXED" : "TEMPLATE"} />
            {isAdmin && (
              <>
                <Button
                  variant="soft"
                  onClick={() => patchSection(section.id, { kind: section.kind === "Fixed" ? "Template" : "Fixed" })}
                >
                  {section.kind === "Fixed" ? "Make template" : "Make fixed"}
                </Button>
                <Button variant="danger" onClick={() => remove(section.id)}>Remove</Button>
              </>
            )}
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
            {section.items.map((item) => (
              <div key={item.id} style={{ display: "flex", alignItems: "center", gap: 10 }}>
                <RowInput
                  value={item.label}
                  disabled={!isAdmin}
                  placeholder="Item label…"
                  onChange={(v) => patchItem(section.id, item.id, { label: v })}
                />
                <select
                  value={item.weight}
                  disabled={!isAdmin}
                  onChange={(e) => patchItem(section.id, item.id, { weight: e.target.value as SectionItem["weight"] })}
                  style={selectStyle()}
                >
                  {WEIGHTS.map((w) => (
                    <option key={w} value={w}>{w}</option>
                  ))}
                </select>
                {isAdmin && (
                  <Button variant="ghost" onClick={() => removeItem(section.id, item.id)}>×</Button>
                )}
              </div>
            ))}
          </div>

          {isAdmin && (
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginTop: 14 }}>
              <Button variant="ghost" style={{ color: "var(--brandA)", borderColor: "transparent" }} onClick={() => addItem(section.id)}>
                + Add item
              </Button>
              <Button onClick={() => save(section)}>Save</Button>
            </div>
          )}
        </Card>
      ))}

      {isAdmin && (
        <Button onClick={addSection}>+ Add section</Button>
      )}
    </div>
  );
}
