import { useState } from "react";
import { api } from "../../api/client";
import { useApp } from "../../app/AppProvider";
import { useAsync } from "../../hooks/useAsync";
import { Button, Card, ErrorState, Loading } from "../../components/ui";
import { RowInput, TabHint } from "./parts";
import type { Category, Section } from "../../api/types";

export function CategoriesTab() {
  const { isAdmin, toast, refreshReference } = useApp();
  const { data, loading, error, reload } = useAsync(
    () => Promise.all([api.categories(), api.sections()]),
    [],
  );
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [newName, setNewName] = useState("");

  if (loading) return <Loading />;
  if (error) return <ErrorState message={error} onRetry={reload} />;
  const [categories, sections] = (data ?? [[], []]) as [Category[], Section[]];

  const val = (c: Category) => drafts[c.id] ?? c.name;

  async function saveName(c: Category) {
    try {
      await api.saveCategory({ id: c.id, name: val(c), includedSectionIds: c.includedSectionIds });
      toast("Category updated");
      await refreshReference();
      reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Update failed");
    }
  }

  async function remove(c: Category) {
    try {
      await api.deleteCategory(c.id);
      toast("Category removed");
      await refreshReference();
      reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Remove failed");
    }
  }

  async function toggleSection(c: Category, sectionId: string) {
    const active = c.includedSectionIds.includes(sectionId);
    const nextIds = active
      ? c.includedSectionIds.filter((id) => id !== sectionId)
      : [...c.includedSectionIds, sectionId];
    try {
      await api.saveCategory({ id: c.id, name: val(c), includedSectionIds: nextIds });
      toast("Sections updated");
      await refreshReference();
      reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Update failed");
    }
  }

  async function add() {
    if (!newName.trim()) return;
    try {
      await api.saveCategory({ name: newName.trim(), includedSectionIds: [] });
      setNewName("");
      toast("Category added");
      await refreshReference();
      reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Add failed");
    }
  }

  return (
    <div>
      <TabHint>
        Vendor categories and the review sections each one includes.
        {!isAdmin && " (read-only — administrators can edit)"}
      </TabHint>

      <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
        {categories.map((c) => (
          <Card key={c.id} style={{ padding: 18, marginBottom: 12 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
              <RowInput
                value={val(c)}
                disabled={!isAdmin}
                onChange={(v) => setDrafts((d) => ({ ...d, [c.id]: v }))}
              />
              {isAdmin && (
                <>
                  {drafts[c.id] !== undefined && drafts[c.id] !== c.name && (
                    <Button variant="soft" onClick={() => saveName(c)}>Save</Button>
                  )}
                  <Button variant="danger" onClick={() => remove(c)}>Remove</Button>
                </>
              )}
            </div>

            <div style={{ display: "flex", flexWrap: "wrap", gap: 8, marginTop: 14 }}>
              {sections.map((s) => {
                const active = c.includedSectionIds.includes(s.id);
                return (
                  <button
                    key={s.id}
                    onClick={() => isAdmin && toggleSection(c, s.id)}
                    disabled={!isAdmin}
                    style={{
                      borderRadius: 999,
                      padding: "6px 12px",
                      fontSize: 13,
                      cursor: isAdmin ? "pointer" : "default",
                      border: `1px solid ${active ? "var(--brandA)" : "var(--line)"}`,
                      background: active
                        ? "color-mix(in srgb, var(--brandA) 14%, transparent)"
                        : "transparent",
                      color: active ? "var(--brandA)" : "var(--muted)",
                    }}
                  >
                    {s.name}
                  </button>
                );
              })}
            </div>
          </Card>
        ))}
      </div>

      {isAdmin && (
        <div style={{ display: "flex", gap: 12, marginTop: 16 }}>
          <RowInput value={newName} onChange={setNewName} placeholder="New category name…" />
          <Button onClick={add}>+ Add category</Button>
        </div>
      )}
    </div>
  );
}
