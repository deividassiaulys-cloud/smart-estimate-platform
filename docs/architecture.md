# Architecture

## 1. Technology Stack

| Layer | Choice | Reason |
|---|---|---|
| Backend | ASP.NET Core (latest stable .NET version) | Required by assignment; mature Web API framework with built-in DI and testability. |
| Frontend | React + Vite + TypeScript | Component-based SPA fits the role-dependent, form-heavy UI; TypeScript reduces runtime errors in estimate calculation data. |
| Database | MySQL (managed via MySQL Workbench) | Free relational database with an exact DECIMAL type for money values (NFR-006); managed with MySQL Workbench. |
| ORM / data access | Entity Framework Core, with Microting.EntityFrameworkCore.MySql provider | Standard .NET data-access approach; provides migrations. Microting chosen over Pomelo because it actively tracks the current EF Core version, while Pomelo's latest release only supports EF Core 9. |
| Authentication | ASP.NET Core Identity (hashing only) + JWT Bearer tokens | Identity's hasher; JWT fits a separate SPA + API architecture without server-side sessions. |
| Backend testing | xUnit + WebApplicationFactory (integration tests) | Standard .NET test framework; integration tests verify role/company data isolation end-to-end. |
| Frontend testing | Vitest + React Testing Library | Native Vite integration; tests component behaviour, not implementation details. |
| Excel export | ClosedXML | Open-source, MIT-licensed, generates .xlsx server-side without commercial licensing concerns. |
| API documentation | Hand-written endpoint contracts in specs/*/contracts | No generated API description is published (feature 018): nothing read the OpenAPI document, and its 89 response-type attributes had to be maintained by hand. The OpenAPI and the never-used Swashbuckle packages were removed. |
| HTTP client | Fetch API (native) | No extra dependency needed for this project's size. |
| Routing | React Router | Standard for role-dependent, multi-screen SPA navigation. |

## 2. Application Architecture

| Layer | Responsibility |
|---|---|
| Frontend | Renders UI, handles role-based navigation and client-side validation, calls the API. Contains no business logic — e.g. it does not calculate estimate totals itself, only displays what the API returns. |
| API layer (Controllers) | Receives HTTP requests, authenticates the user (JWT), authorizes by role, delegates to the Application layer, returns HTTP responses. Contains no business rules. |
| Application / business logic layer | Implements all business rules: estimate calculations (decimal arithmetic and rounding), status transitions, input validation (ranges, fixed units, duplicates), company/role permission checks. |
| Data access layer | Entity Framework Core, entity configurations, migrations, queries. Translates application-layer requests into database operations. |
| Database | MySQL; persists all companies, users, materials, works, projects, and estimates. |

## 3. Repository Structure

```
/
├── server/
│   ├── SmartEstimatePlatform.slnx
│   └── SmartEstimatePlatform.Api/       # ASP.NET Core Web API project
│       ├── Controllers/                 # API endpoints — HTTP concerns only, no business logic
│       ├── Data/                        # EF Core DbContext, database configuration
│       ├── Migrations/                  # EF Core migrations
│       ├── Models/
│       │   ├── DTOs/                    # Request/response objects exposed via the API
│       │   └── Entities/                # Domain entities and fixed lists (e.g. measurement units)
│       └── Services/                    # Business logic, calculations, rules (the Application layer)
├── client/
│   └── src/
│       ├── api/                         # Typed functions calling the backend API
│       ├── assets/                      # Images, icons, static files
│       ├── components/                  # Shared UI pieces (ConfirmDialog, form fields, tables)
│       ├── interfaces/                  # Shared TypeScript types/interfaces
│       ├── pages/                       # Screen-level components (Dashboard, EstimateEditor, ...)
│       ├── styles/                      # Global/shared CSS
│       └── utils/                       # Helpers (auth storage, formatting)
├── tests/
│   ├── server/                          # xUnit unit + integration tests
│   └── client/                          # Vitest + React Testing Library tests
├── docs/                                # requirements.md, ux.md, architecture.md, wireframes/
├── specs/                               # Spec Kit feature specifications
├── .specify/                            # Spec Kit templates, scripts and the constitution
├── .github/
├── AGENTS.md
└── README.md
```

## 4. Deployment Security Settings

Set outside the repository, as environment variables, in every environment other than Development (features 021 and 022):

| Setting | Value | Why |
|---|---|---|
| `AllowedHosts` | The API's host names, separated by `;` (e.g. `estimates.example.lt`) | Only these hosts are served. The app refuses to start outside Development while it is missing or `*` (F7). `appsettings.json` keeps `*` for local runs; the tests list their own hosts. |
| `ReverseProxy__TrustedNetwork` | The reverse proxy's network as a CIDR (e.g. `172.30.0.0/24`, the Docker network) | `X-Forwarded-For` and `X-Forwarded-Proto` are accepted only from this network, for one hop, so the rate limit sees the real client and a forwarded HTTPS request gets HSTS. Required outside Development: the app refuses to start while it is missing or not a CIDR (feature 022). Optional in Development. |
| `APP_VERSION` | Optional; the commit SHA the image was built from | Reported by `GET /health`; `dev` when not set. |
| `RateLimiting__Auth__PermitLimit`, `RateLimiting__Auth__WindowSeconds` | Optional; default 10 requests per 60 seconds | Login and registration together are limited per client address; above the limit the API answers 429 with `Retry-After` (F4). |
| `Jwt__Key`, `ConnectionStrings__DefaultConnection` | Secrets | See docs/security-review.md section 7. |

- **Reverse proxy**: the rate limit uses the client's IP address. Behind a reverse proxy every request would come from the proxy's address, so one attacker could block logins for everyone. Since feature 022 (T358), `UseForwardedHeaders` runs first in the pipeline and trusts `X-Forwarded-For` and `X-Forwarded-Proto` only from `ReverseProxy__TrustedNetwork`, with one hop; the same headers from any other address are ignored (`ForwardedHeadersTests`).
- **Health endpoints** (feature 022, T357): `GET /health` (liveness) answers 200 with `{"status":"Healthy","version":"<APP_VERSION>"}` and never touches the database. `GET /health/ready` answers `Healthy` (200) when the database can be reached, otherwise `Unhealthy` (503), with no other detail. Both are public, need no token and are not rate limited.
- **Logs and correlation id** (feature 022, T359, T360): every response carries `X-Correlation-ID`, the request's W3C trace id.
  - Outside Development the console log is JSON, one entry per line, with UTC timestamps and the request's scopes (`TraceId`, `SpanId`, `RequestId`, `RequestPath`).
  - `TraceId` equals `X-Correlation-ID`, so it finds every entry of one request; a `ProblemDetails` body's `traceId` contains the same id.
  - No entry holds a password, token, key or connection string (`ProductionLoggingTests.Requests_LogNoKeyOrConnectionString`).
  - Behind the proxy the API listens on HTTP only, so its HTTPS redirection logs "Failed to determine the https port for redirect" on the first request. This is expected: the proxy redirects HTTP to HTTPS.
- **Security headers**: every API response carries `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` and `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` (the API serves only JSON and files, never pages).
- **Hosted client CSP**: the web server that serves the built React client (`client/dist`) must send its own policy, at least `default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'`, plus `X-Content-Type-Options: nosniff` and `Referrer-Policy: no-referrer`. The client uses no inline scripts, no `eval` and no external origins, so it needs nothing more; the API must be reached on the same origin (as the Vite proxy does today), otherwise `connect-src` and a CORS policy must name it. This policy is the main protection of the token kept in `localStorage` (docs/security-review.md, Accepted limitations).

