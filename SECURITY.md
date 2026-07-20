# Security

Threat model and security posture for the Vendor Technical Review ("Assess") platform.

- **Full report:** [`docs/security/threat-model.html`](docs/security/threat-model.html) — a STRIDE + LINDDUN
  threat model with trust boundaries, a 22-item threat register, a 12-dimension scorecard and a
  remediation plan. Open it in a browser (self-contained, light/dark aware).

## Posture at a glance

Moderate (**3.4 / 5**). The core is sound — Entra authentication, **server-authoritative
authorization** (the client role switch is a display hint only), **server-computed verdicts** (a
client cannot forge an approval), and parameterized/output-encoded I/O close the classic injection
classes. Remaining work is pre-production hardening.

## Fixed in code (runtime-verified)

| Finding | Fix |
|---|---|
| Dev-auth fallback could run open in a misconfigured deploy | The API now **fails closed** — it refuses to start outside `Development` unless `Auth:AllowDevFallback=true` is explicitly set. |
| NDA / reminder mail could be sent to arbitrary addresses at any volume | Both endpoints are **per-user rate-limited** (`Mail:RateLimitPerWindow` / `RateWindowMinutes`) and recipients are checked against a **domain allowlist** (`Mail:AllowedRecipientDomains`); the background sweep filters too. |
| Dependency drift | **Dependabot** enabled for NuGet, npm and GitHub Actions (`.github/dependabot.yml`). |
| No CI security gate | **CI pipeline** (`.github/workflows/ci.yml`): build/lint/typecheck + **CodeQL** (SAST) + **Trivy** (SCA/secret/IaC) on every push and PR. |
| No record of sensitive actions | **Append-only audit log** of sign-off, finish, NDA/reminder mail, directory import, role changes and settings/reset (actor, role, target, summary); admins-only **Activity** view (`GET /admin/audit`). |
| Personal data stored in clear | **AES-256-GCM at-rest encryption** (`Encryption:Enabled` + base64 32-byte `Encryption:Key`) of vendor contacts, imported-user identity, and review NDA/owner emails — envelope-marked (`enc:v1:`), key-rotatable, transparent read/write. |

## Highest open item (Entra tenant action, not code)

The app-only Microsoft Graph **`Mail.Send`** permission is tenant-wide, so a leaked client secret
could send mail as any mailbox. Constrain it to the single sender mailbox with an
[Application Access Policy](https://learn.microsoft.com/graph/auth-limit-mailbox-access), and move the
secret to a **managed identity or certificate credential** in Key Vault.

## Remaining hardening (tracked in the report)

**At-rest encryption** of personal data + DPIA/ROPA · SIEM export of the audit trail · TLS/HSTS
enforcement · managed Postgres with PITR/replicas.

## Relevant configuration

| Setting | Purpose | Default |
|---|---|---|
| `Auth:AllowDevFallback` | Permit the open dev-auth handler outside Development (dev/demo only) | `false` |
| `Mail:AllowedRecipientDomains` | Outbound mail recipient allowlist (empty = allow all) | `[]` |
| `Mail:RateLimitPerWindow` / `Mail:RateWindowMinutes` | Per-user mail rate limit | `10` / `10` |
| `Encryption:Enabled` / `Encryption:Key` | AES-256-GCM at-rest encryption of personal data; base64 32-byte key (`openssl rand -base64 32`, store in Key Vault) | `false` / _(unset)_ |

## Reporting

Report suspected vulnerabilities to the Global IT Infrastructure team rather than opening a public
issue.
