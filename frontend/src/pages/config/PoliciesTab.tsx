import { useEffect, useState } from "react";
import { api } from "../../api/client";
import { useApp } from "../../app/AppProvider";
import { useAsync } from "../../hooks/useAsync";
import { Button, Card, ErrorState, Loading } from "../../components/ui";
import { Toggle, RowInput, TabHint, selectStyle } from "./parts";
import type { Policy, Section } from "../../api/types";

export function PoliciesTab() {
  const { isAdmin, toast } = useApp();
  const { data, loading, error, reload } = useAsync(
    () => Promise.all([api.policies(), api.sections()]),
    []
  );
  const [rows, setRows] = useState<Policy[]>([]);

  useEffect(() => {
    if (data) setRows(data[0]);
  }, [data]);

  if (loading) return <Loading />;
  if (error) return <ErrorState message={error} onRetry={reload} />;

  const sections: Section[] = data?.[1] ?? [];

  function patch(id: string, fields: Partial<Policy>) {
    setRows((rs) => rs.map((r) => (r.id === id ? { ...r, ...fields } : r)));
  }

  function withSectionName(p: Policy, sectionId: string): Partial<Policy> {
    const name = sections.find((s) => s.id === sectionId)?.name ?? p.sectionName;
    return { sectionId, sectionName: name };
  }

  async function save(p: Policy) {
    try {
      await api.savePolicy({
        id: p.id,
        rule: p.rule,
        sectionId: p.sectionId,
        severity: p.severity,
        weight: p.weight,
        active: p.active,
      });
      toast("Policy saved");
    } catch (e) {
      toast(e instanceof Error ? e.message : "Save failed");
    }
  }

  async function toggleActive(p: Policy, active: boolean) {
    patch(p.id, { active });
    try {
      await api.savePolicy({
        id: p.id,
        rule: p.rule,
        sectionId: p.sectionId,
        severity: p.severity,
        weight: p.weight,
        active,
      });
      toast(active ? "Policy activated" : "Policy deactivated");
    } catch (e) {
      patch(p.id, { active: !active });
      toast(e instanceof Error ? e.message : "Save failed");
    }
  }

  async function remove(id: string) {
    try {
      await api.deletePolicy(id);
      toast("Policy removed");
      reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Remove failed");
    }
  }

  async function add() {
    try {
      await api.savePolicy({
        rule: "New rule",
        sectionId: sections[0]?.id,
        severity: "Concern",
        weight: "Med",
        active: true,
      });
      toast("Policy added");
      reload();
    } catch (e) {
      toast(e instanceof Error ? e.message : "Add failed");
    }
  }

  return (
    <div>
      <TabHint>
        Weighted policy library. Each rule maps to a review section with a severity and weight.
        Changes apply to every review.{!isAdmin && " (read-only — administrators can edit)"}
      </TabHint>

      <Card style={{ overflowX: "auto" }}>
        <table style={{ borderCollapse: "collapse", width: "100%", minWidth: 820 }}>
          <thead>
            <tr style={head}>
              <Th>POLICY RULE</Th>
              <Th>APPLIES TO SECTION</Th>
              <Th>SEVERITY</Th>
              <Th>WEIGHT</Th>
              <Th>STATUS</Th>
              <Th></Th>
            </tr>
          </thead>
          <tbody>
            {rows.map((p) => (
              <tr key={p.id} style={{ borderTop: "1px solid var(--line)" }}>
                <Td>
                  <RowInput
                    value={p.rule}
                    disabled={!isAdmin}
                    onChange={(v) => patch(p.id, { rule: v })}
                  />
                </Td>
                <Td>
                  <select
                    style={selectStyle()}
                    value={p.sectionId}
                    disabled={!isAdmin}
                    onChange={(e) => patch(p.id, withSectionName(p, e.target.value))}
                  >
                    {sections.map((s) => (
                      <option key={s.id} value={s.id}>{s.name}</option>
                    ))}
                  </select>
                </Td>
                <Td>
                  <select
                    style={selectStyle()}
                    value={p.severity}
                    disabled={!isAdmin}
                    onChange={(e) => patch(p.id, { severity: e.target.value as Policy["severity"] })}
                  >
                    <option value="Blocker">Blocker</option>
                    <option value="Concern">Concern</option>
                  </select>
                </Td>
                <Td>
                  <select
                    style={selectStyle()}
                    value={p.weight}
                    disabled={!isAdmin}
                    onChange={(e) => patch(p.id, { weight: e.target.value as Policy["weight"] })}
                  >
                    <option value="High">High</option>
                    <option value="Med">Med</option>
                    <option value="Low">Low</option>
                  </select>
                </Td>
                <Td>
                  <Toggle
                    on={p.active}
                    disabled={!isAdmin}
                    onChange={(v) => toggleActive(p, v)}
                  />
                </Td>
                <Td>
                  {isAdmin && (
                    <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
                      <Button variant="soft" onClick={() => save(p)}>Save</Button>
                      <Button variant="danger" onClick={() => remove(p.id)}>Remove</Button>
                    </div>
                  )}
                </Td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>

      {isAdmin && (
        <div style={{ marginTop: 16 }}>
          <Button onClick={add}>+ Add policy</Button>
        </div>
      )}
    </div>
  );
}

const head: React.CSSProperties = {
  background: "var(--panel2)",
  color: "var(--muted)",
  fontSize: 11,
  letterSpacing: 0.8,
  textAlign: "left",
};
const Th = ({ children }: { children?: React.ReactNode }) => (
  <th style={{ padding: "12px 16px", fontWeight: 700, textTransform: "uppercase" }}>{children}</th>
);
const Td = ({ children }: { children: React.ReactNode }) => (
  <td style={{ padding: "12px 16px", color: "var(--text)", fontSize: 14 }}>{children}</td>
);
