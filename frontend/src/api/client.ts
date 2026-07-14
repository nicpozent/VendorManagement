import { authBridge, ENTRA_CONFIGURED } from "./authBridge";
import type {
  AppUser, ArchiveDetail, ArchiveItem, Category, CompareMatrix, Entity, ImportKind,
  ImportResult, ImportSource, GraphMember, Me, Policy, ReviewDetail, ReviewListItem,
  ScanResult, Section, Settings, Vendor,
} from "./types";

const BASE = (import.meta.env.VITE_API_BASE as string) || "/api";

export class ApiError extends Error {
  constructor(public status: number, message: string) { super(message); }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = { "Content-Type": "application/json" };

  // Auth on every call. Bearer token when Entra is configured; otherwise the dev
  // header (the API only honours it when real Entra auth is not registered).
  const token = await authBridge.getToken();
  if (token) headers["Authorization"] = `Bearer ${token}`;
  if (!ENTRA_CONFIGURED) headers["X-Debug-Role"] = authBridge.getRole();

  const res = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (!res.ok) {
    let msg = `${res.status} ${res.statusText}`;
    try {
      const txt = await res.text();
      if (txt) msg = txt;
    } catch { /* ignore */ }
    throw new ApiError(res.status, msg);
  }
  if (res.status === 204) return undefined as T;
  const ct = res.headers.get("content-type") ?? "";
  if (ct.includes("application/json")) return (await res.json()) as T;
  return (await res.text()) as unknown as T;
}

export const api = {
  me: () => request<Me>("GET", "/me"),

  // Reviews
  reviews: (q: { status?: string; categoryId?: string; entityId?: string } = {}) => {
    const p = new URLSearchParams();
    if (q.status) p.set("status", q.status);
    if (q.categoryId) p.set("categoryId", q.categoryId);
    if (q.entityId) p.set("entityId", q.entityId);
    const qs = p.toString();
    return request<ReviewListItem[]>("GET", `/reviews${qs ? `?${qs}` : ""}`);
  },
  review: (id: string) => request<ReviewDetail>("GET", `/reviews/${id}`),
  createReview: (b: { vendorName: string; productName: string; categoryId?: string | null; entityId?: string | null; ownerName?: string | null; }) =>
    request<ReviewDetail>("POST", "/reviews", b),
  updateReview: (id: string, b: Partial<ReviewDetail> & { sections?: unknown; openQuestions?: unknown }) =>
    request<ReviewDetail>("PUT", `/reviews/${id}`, b),
  scan: (id: string) => request<ScanResult>("POST", `/reviews/${id}/scan`),
  signoff: (id: string) => request<ReviewDetail>("POST", `/reviews/${id}/signoff`),
  finish: (id: string) => request<ReviewDetail>("POST", `/reviews/${id}/finish`),
  remind: (id: string) => request<{ sent: boolean; mock: boolean; to: string[]; cc: string[] }>("POST", `/reviews/${id}/remind`),
  memo: (id: string) => request<string>("GET", `/reviews/${id}/memo`),
  deleteReview: (id: string) => request<void>("DELETE", `/reviews/${id}`),

  // Catalog
  categories: () => request<Category[]>("GET", "/catalog/categories"),
  saveCategory: (c: Partial<Category>) =>
    c.id ? request<Category>("PUT", `/catalog/categories/${c.id}`, c)
         : request<Category>("POST", "/catalog/categories", c),
  deleteCategory: (id: string) => request<void>("DELETE", `/catalog/categories/${id}`),

  sections: () => request<Section[]>("GET", "/catalog/sections"),
  saveSection: (s: Partial<Section>) =>
    s.id ? request<Section>("PUT", `/catalog/sections/${s.id}`, s)
         : request<Section>("POST", "/catalog/sections", s),
  deleteSection: (id: string) => request<void>("DELETE", `/catalog/sections/${id}`),

  policies: () => request<Policy[]>("GET", "/catalog/policies"),
  savePolicy: (p: Partial<Policy>) =>
    p.id ? request<Policy>("PUT", `/catalog/policies/${p.id}`, p)
         : request<Policy>("POST", "/catalog/policies", p),
  deletePolicy: (id: string) => request<void>("DELETE", `/catalog/policies/${id}`),

  // Entities
  entities: () => request<Entity[]>("GET", "/entities"),
  createEntity: (name: string) => request<Entity>("POST", "/entities", { name }),
  updateEntity: (id: string, name: string) => request<Entity>("PUT", `/entities/${id}`, { name }),
  deleteEntity: (id: string) => request<void>("DELETE", `/entities/${id}`),

  // Vendors
  vendors: () => request<Vendor[]>("GET", "/vendors"),
  sendNda: (id: string) => request<{ message: string; ccTo: string }>("POST", `/vendors/${id}/send-nda`),

  // Compare / Archive
  compare: (categoryId: string) => request<CompareMatrix>("GET", `/compare?categoryId=${categoryId}`),
  archive: () => request<ArchiveItem[]>("GET", "/archive"),
  archiveDetail: (id: string) => request<ArchiveDetail>("GET", `/archive/${id}`),

  // Settings
  settings: () => request<Settings>("GET", "/settings"),
  saveSettings: (s: Settings) => request<Settings>("PUT", "/settings", s),
  resetSampleData: () => request<{ message: string }>("POST", "/settings/reset"),

  // Admin / Entra import
  graphStatus: () => request<{ configured: boolean }>("GET", "/admin/graph-status"),
  users: () => request<AppUser[]>("GET", "/admin/users"),
  updateUser: (id: string, role: string, enabled: boolean) =>
    request<AppUser>("PUT", `/admin/users/${id}`, { role, enabled }),
  deleteUser: (id: string) => request<void>("DELETE", `/admin/users/${id}`),
  importSources: (kind: ImportKind) => request<ImportSource[]>("GET", `/admin/import/sources?kind=${kind}`),
  importPreview: (kind: ImportKind, sourceId: string) =>
    request<GraphMember[]>("GET", `/admin/import/preview?kind=${kind}&sourceId=${encodeURIComponent(sourceId)}`),
  importUsers: (b: { sourceId: string; sourceKind: string; sourceName: string; defaultRole: string }) =>
    request<ImportResult>("POST", "/admin/import", b),
  runReminders: () => request<{ attempted: number }>("POST", "/admin/reminders/run"),
};
