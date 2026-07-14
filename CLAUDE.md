# CLAUDE.md — Build the Vendor Technical Review front end

> Read this first. Build the **front end** for the Vendor Technical Review platform so every screen
> matches the screenshots in `screenshots/` exactly. Wire it to the API described in §7. **No seed
> data** — screens load from the API and show empty states until data exists.

## 0. Golden rule — match the screenshots

`screenshots/` is the spec of record. Reproduce layout, spacing, palette, type, columns, states and
copy exactly. Do not redesign, re-theme, or reorganise. The only intended difference from a filled
prototype is **no hard-coded data**.

Screens (files in `screenshots/`):
- `01-dash.png` Dashboard — **Daylight** theme (default)
- `02-dash.png` Dashboard — **Command** theme (dark navy)
- `03-dash.png` Dashboard — **Carbon** theme (near-black)
- `04-dash.png` Compare — vendor matrix (verdict + readiness + per-item status)
- `05-dash.png` Vendors — Approved + Rejected, NDA status, Send NDA
- `06-dash.png` Archive — finished version list
- `01-cfg.png` Configuration ▸ Vendor categories
- `02-cfg.png` Configuration ▸ **Entities**
- `03-cfg.png` Configuration ▸ Review sections (fixed + template)
- `04-cfg.png` Configuration ▸ Policy library (weighted)
- `05-cfg.png` Configuration ▸ Settings
- `01-review.png` / `02-review.png` Review editor — two-pane (intake + scoring · live memo)

## 1. Layout & shell (all screens)

- **Fixed dark left sidebar (248px, `--shell`)**: stacked Birgma/Biltema logo lockup on a white
  plate; "Vendor Review · Assess" wordmark (Space Grotesk); grouped nav — **Workspace** (Dashboard,
  Compare, Vendors, Archive) and **Configure** (Configuration); active item has a translucent
  highlight; signed-in user chip pinned at the bottom. Sidebar stays dark in every theme.
- **White topbar (`--panel`)**: page title + subtitle (Space Grotesk), then the **theme switch**
  (Daylight / Command / Carbon segmented control) and the **role switch** (IT Manager / CIO-CTO /
  CFO).
- **Content** scrolls in the main column.

## 2. Theme modes (Daylight / Command / Carbon)

Theming is driven by **CSS custom properties on the app root**; the switch sets the active theme and
writes the variable set onto the root. Persist the choice. Token sets:

| var | Daylight | Command | Carbon |
|-----|----------|---------|--------|
| `--canvas` | `#EEF1F6` | `#0a1024` | `#0a0c12` |
| `--panel` | `#ffffff` | `#111c42` | `#101319` |
| `--panel2` | `#F1F3F8` | `#0e1738` | `#0c0f15` |
| `--line` | `#E7EBF2` | `rgba(150,175,255,.14)` | `rgba(255,255,255,.09)` |
| `--text` | `#11163A` | `#eef2ff` | `#f2f4f8` |
| `--muted` | `#69728A` | `#9fb0d8` | `#9aa3b4` |
| `--faint` | `#9aa2b4` | `#6c7fb0` | `#5e6676` |
| `--brandA` | `#0F6CBD` | `#2E93E6` | `#3aa0ff` |
| `--shell` (sidebar) | `#11163A` | `#0c1430` | `#0b0d14` |

Rules: build all surfaces/text/borders from these vars. Keep **semantic status colours constant**
across themes — Pass/Approved/NDA-signed `#16A37B`, Concern/Info `#F2A516`, Blocker/Rejected
`#DB1A52`, In-progress/brand `--brandA`. The **memo document stays light in every theme** (set light
var overrides locally on the memo `<article>`), because it is a printable paper artifact.

## 3. Type

Space Grotesk (headings + wordmark + numerics), IBM Plex/Public Sans-style body — the screenshots use
**Space Grotesk** for headings and **Public Sans** for body. Load both from Google Fonts. Do not
substitute Inter/Roboto.

## 4. Roles

- Switch: **IT Manager**, **CIO / CTO**, **CFO**. Manager sees only their own reviews; CIO/CTO and
  CFO see the whole portfolio (leadership).
- **Platform administrator = CIO/CTO and CFO.** Only they can create/edit **Entities** (see §5);
  IT Manager sees Entities read-only.
- Role is a **view hint only** on the client — the API enforces real authorisation. Never gate
  security on client role.

## 5. Entities (this iteration's new feature)

- An **Entity** is a Birgma group business (e.g. Birgma International SA, Biltema Sweden/Norway/
  Finland/Denmark). Managed in **Configuration ▸ Entities** — create/rename/remove, admins only.
- **Dashboard** has an **Entity filter** dropdown next to Status and Category ("All entities" +
  each entity). The vendor cell in the table shows the review's entity (brand-coloured line).
- The **Review editor** metadata has an **Entity** `<select>` (blank "— Select entity —" + entities).
  New reviews default to the first entity.
- Persist entity assignments on the review; filter the list by entity.

## 6. Screens (behaviour)

- **Dashboard**: role-aware title; 7 KPI cards; "Needs attention" banner (open blockers); Status +
  Category + **Entity** filters; reviews table (Vendor/Category/Owner/Status/Blk/Cnc/Updated).
- **Compare**: category selector; matrix — columns = vendors in that category, rows = rubric items by
  section; top Verdict + Readiness rows; status pills per cell. Blocker-first.
- **Vendors**: Approved table (Vendor, Category, Contact, Last review, **NDA** pill, Send NDA) +
  Rejected table (Vendor, Category, Rejected, Reason). Send NDA → toast; the requesting user is cc'd
  (real send is MS Graph server-side).
- **Archive**: finished version list → read-only memo snapshot with exports.
- **Configuration** (5 tabs): Vendor categories (name + included-section chips) · **Entities** ·
  Review sections (fixed items or template/selectable options, each item weighted High/Med/Low) ·
  Policy library (rule → section, severity Blocker/Concern, weight, active) · Settings (blocker-caps-
  verdict toggle; reset sample data).
- **Review editor**: left = metadata (incl. **Entity**), Vendor info & **NDA** card, raw-pitch +
  "Scan & suggest" (deterministic assisted intake), completeness checks, category-driven sections
  with Pass/Concern/Blocker scoring (+ why/unblock or mitigation), open questions. Right = **live
  blocker-first memo** (Verdict → Blockers → Concerns → Acceptable → Open questions → Recommendation),
  attributed to "Nick [Surname], Senior Manager, Global IT Infrastructure, Birgma International SA".
  Verdict is **computed by the API**, not the client. Exports: Copy .md / .md / Excel / Print /
  Finish & archive.

## 7. Data & API

- All content via a typed API client; nothing hard-coded. Endpoints (mirror the .NET backend):
  `/reviews`, `/reviews/{id}` (returns review + computed verdict), `/reviews/{id}/scan|signoff|finish`,
  `/vendors` + `/vendors/{id}/contact|nda|send-nda`, `/catalog/categories|sections|policies`,
  **`/entities`** (list + admin create/update/delete). Add `entityId` to the review model and an
  `Entity { id, name }` type.
- Server-authoritative: verdict, readiness %, completeness checks and the intake scan come from the
  API. Render empty + loading + error states everywhere.

## 8. Stack & done

React 18 + TypeScript + Vite; MSAL (Entra ID); inline styles driven by the CSS-var tokens (no CSS
framework, no new fonts). Auth on every call; role only for display; no secrets committed.
Definition of done: matches the screenshots in all three themes; entities filter + form + admin CRUD
work; typecheck/lint/build green; container image builds.
