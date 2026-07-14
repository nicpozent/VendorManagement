import { useState } from "react";
import { api } from "../../api/client";
import { useApp } from "../../app/AppProvider";
import { useAsync } from "../../hooks/useAsync";
import { Button, Card, ErrorState, Loading } from "../../components/ui";
import { RowInput, TabHint } from "./parts";
import { STATUS } from "../../theme/tokens";

export function EntitiesTab() {
  const { isAdmin, toast, refreshReference } = useApp();
  const { data, loading, error, reload } = useAsync(() => api.entities(), []);
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [newName, setNewName] = useState("");

  if (loading) return <Loading />;
  if (error) return <ErrorState message={error} onRetry={reload} />;
  const entities = data ?? [];

  const val = (id: string, name: string) => drafts[id] ?? name;

  async function save(id: string, name: string) {
    try { await api.updateEntity(id, name); toast("Entity updated"); await refreshReference(); reload(); }
    catch (e) { toast(e instanceof Error ? e.message : "Update failed"); }
  }
  async function remove(id: string) {
    try { await api.deleteEntity(id); toast("Entity removed"); await refreshReference(); reload(); }
    catch (e) { toast(e instanceof Error ? e.message : "Remove failed"); }
  }
  async function add() {
    if (!newName.trim()) return;
    try { await api.createEntity(newName.trim()); setNewName(""); toast("Entity added"); await refreshReference(); reload(); }
    catch (e) { toast(e instanceof Error ? e.message : "Add failed"); }
  }

  return (
    <div>
      <TabHint>Business entities used to tag and filter vendor reviews.{!isAdmin && " (read-only — administrators can edit)"}</TabHint>

      <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
        {entities.map((e) => (
          <Card key={e.id} style={{ padding: 14, display: "flex", alignItems: "center", gap: 14 }}>
            <span style={{ width: 10, height: 10, borderRadius: 999, background: STATUS.brand, flexShrink: 0 }} />
            <RowInput
              value={val(e.id, e.name)}
              disabled={!isAdmin}
              onChange={(v) => setDrafts((d) => ({ ...d, [e.id]: v }))}
            />
            {isAdmin && (
              <>
                {(drafts[e.id] !== undefined && drafts[e.id] !== e.name) && (
                  <Button variant="soft" onClick={() => save(e.id, val(e.id, e.name))}>Save</Button>
                )}
                <Button variant="danger" onClick={() => remove(e.id)}>Remove</Button>
              </>
            )}
          </Card>
        ))}
      </div>

      {isAdmin && (
        <div style={{ display: "flex", gap: 12, marginTop: 16 }}>
          <RowInput value={newName} onChange={setNewName} placeholder="New entity name…" />
          <Button onClick={add}>+ Add entity</Button>
        </div>
      )}
    </div>
  );
}
