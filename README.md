# Vendor Technical Review — “Assess”

Full-stack platform for Birgma/Biltema vendor technical reviews: a **React 18 + TypeScript + Vite**
front end matching the reference designs across three themes, backed by a **.NET 8** API on
**PostgreSQL**, wired to **Microsoft Entra ID** for auth and **Microsoft Graph** for administrator
user-import and email reminders.

The build follows [`CLAUDE.md`](./CLAUDE.md): screens match the screenshots in `screenshots/`, all
content is served from the API (nothing hard-coded on the client), and the verdict/readiness/
completeness/scan are server-authoritative.

---

## What’s here

```
backend/   .NET 8 minimal-API (EF Core + Npgsql), verdict engine, Graph integration, reminder worker
frontend/  React 18 + TS + Vite SPA (MSAL, CSS-var theming, inline styles — no CSS framework)
docker-compose.yml   Postgres + API + web (nginx)
```

### Features

- **Dashboard** — role-aware, 7 KPI cards, needs-attention banner, Status/Category/**Entity** filters, reviews table.
- **Compare** — blocker-first vendor matrix per category (verdict + readiness rows, per-item status pills).
- **Vendors** — approved + rejected tables, NDA status, **Send NDA** (Graph mail, requester cc’d).
- **Archive** — immutable finished-review memo snapshots with copy/print export.
- **Configuration** — Vendor categories · **Entities** (admin CRUD) · Review sections · Policy library · Settings · **Directory**.
- **Review editor** — two-pane intake + scoring with a **live, server-computed** blocker-first memo; Scan & suggest; Copy .md / .md / Excel / Print / Finish & archive.
- **Entities** (this iteration) — Birgma group businesses; dashboard filter, review metadata select, admin-only CRUD.
- **Directory (admin)** — import users from Entra **groups / enterprise apps / app registrations** via Graph; manage roles.
- **Reminders** — background worker emails owners + approvers of pending reviews via Graph (also on-demand).

### Roles & authorisation

Roles (**IT Manager**, **CIO/CTO**, **CFO**) are a **display hint** on the client. The API enforces
real authorisation from the validated token: managers see only their own reviews; CIO/CTO & CFO see
the whole portfolio and are the only ones who can manage Entities and import users.

---

## Run it locally (no tenant required)

With no Entra settings, the API uses a **dev principal** (defaults to CFO/admin) and Graph runs in
**mock mode**, so the whole app is demonstrable immediately. Sample data matching the screenshots is
seeded on first start.

### Option A — Docker

```bash
docker compose up --build
# web → http://localhost:8080   api → http://localhost:5080   (swagger at /swagger)
```

### Option B — local dev

```bash
# 1. Postgres (any local instance); default connection string expects:
#    Host=localhost Port=5432 Database=vendorreview User=vendorreview Password=devpassword

# 2. API
cd backend/src/VendorReview.Api
dotnet run            # applies EF migrations + seeds on startup → http://localhost:5080

# 3. Front end
cd frontend
npm install
npm run dev           # http://localhost:5173  (proxies /api → :5080)
```

Switch the **Role** control (top-right) in dev mode to see manager vs. leadership scoping in action.

---

## Go live with Entra ID + Microsoft Graph

Two app registrations (or one with both platforms). Fill `.env` (see `.env.example`).

### 1. API app registration (confidential client)

- **Expose an API** → Application ID URI `api://<api-client-id>`; add a scope `access_as_user`.
- **Certificates & secrets** → new client secret → `AZURE_API_CLIENT_SECRET`.
- **API permissions → Microsoft Graph → Application permissions** (then **Grant admin consent**):
  | Permission | Used for |
  |---|---|
  | `Directory.Read.All` | resolve group members & user details |
  | `GroupMember.Read.All` | import from security/M365 groups |
  | `Application.Read.All` | list enterprise apps / app registrations, owners, app-role assignments |
  | `Mail.Send` | send NDA + reminder mail (from `GRAPH_SENDER_USER_ID`) |
- Set `AZURE_TENANT_ID`, `AZURE_API_CLIENT_ID`, `AZURE_API_AUDIENCE` (`api://<api-client-id>`), `GRAPH_SENDER_USER_ID` (a licensed mailbox UPN/oid).

### 2. SPA app registration (public client, PKCE)

- **Authentication → Single-page application** → redirect URI = the web origin (e.g. `http://localhost:8080`).
- **API permissions** → add the API’s `access_as_user` delegated scope → grant consent.
- Set `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_TENANT_ID`, `VITE_API_SCOPE=api://<api-client-id>/access_as_user`.

When these are present, the API validates real bearer tokens (the dev principal is **not** registered)
and Graph performs live directory reads and mail sends. The client sends the access token on every call.

### App-role → platform-role mapping

The API maps token roles to `CFO` / `CIO-CTO` (admins) / `IT Manager`. Define **App roles** on the API
registration (`Cfo`, `CioCto`) and assign users/groups, or emit a `roles` claim; anything else is treated
as IT Manager.

---

## Configuration reference (API)

| Setting | Env var | Default |
|---|---|---|
| DB connection | `ConnectionStrings__Default` | local Postgres |
| Allow open dev-auth outside Development | `Auth__AllowDevFallback` | `false` (fails closed) |
| Outbound mail recipient allowlist | `Mail__AllowedRecipientDomains__0..n` | empty (allow all) |
| Per-user mail rate limit | `Mail__RateLimitPerWindow` / `Mail__RateWindowMinutes` | `10` / `10` |
| At-rest PII encryption (AES-256-GCM) | `Encryption__Enabled` / `Encryption__Key` (base64 32-byte) | `false` / _(unset)_ |
| Reminder sweep enabled | `Reminders__Enabled` | `true` |
| Sweep interval (min) | `Reminders__IntervalMinutes` | `360` |
| Min hours between reminders | `Reminders__MinHoursBetweenReminders` | `20` |
| CORS origins | `Cors__Origins__0..n` | `http://localhost:5173`, `:4173` |

> **Security:** the API **fails closed** — outside `Development` it refuses to start with the open
> dev-auth handler unless `Auth__AllowDevFallback=true`. See [`SECURITY.md`](./SECURITY.md) and the
> threat model in [`docs/security/threat-model.html`](./docs/security/threat-model.html).

## Notable API endpoints

`/me` · `/reviews` (+ `/{id}/scan|signoff|finish|memo|remind`) · `/vendors` (+ `/{id}/send-nda`) ·
`/catalog/categories|sections|policies` · `/entities` · `/compare` · `/archive` · `/settings` (+ `/reset`) ·
`/admin/users` · `/admin/import/sources|preview` · `/admin/import` · `/admin/reminders/run`.

## Definition of done

- Matches the screenshots across Daylight / Command / Carbon themes.
- Entities filter + form + admin CRUD work; verdict/readiness computed server-side.
- `dotnet build` and `npm run build` (tsc + vite) are green; container images build via `docker compose`.
