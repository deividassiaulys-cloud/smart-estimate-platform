1.	Technology Stack
Layer	Choice	Reason
Backend	ASP.NET Core (latest stable .NET version)	Required by assignment; mature Web API framework with built-in DI and testability.
Frontend	React + Vite + TypeScript	Component-based SPA fits the role-dependent, form-heavy UI; TypeScript reduces runtime errors in estimate calculation data.
Database	MySQL (managed via MySQL Workbench)	Free, relational.
ORM / data access	Entity Framework Core, with Microting.EntityFrameworkCore.MySql provider	Standard .NET data-access approach; provides migrations. Microting chosen over Pomelo because it actively tracks the current EF Core version, while Pomelo’s latest release only supports EF Core 9.
Authentication	ASP.NET Core Identity (hashing only) + JWT Bearer tokens	Identity’s hasher; JWT fits a separate SPA + API architecture without server-side sessions.
Backend testing	xUnit + WebApplicationFactory (integration tests)	Standard .NET test framework; integration tests verify role/company data isolation end-to-end.
Frontend testing	Vitest + React Testing Library	Native Vite integration; tests component behaviour, not implementation details.
Excel export	ClosedXML	Open-source, MIT-licensed, generates .xlsx server-side without commercial licensing concerns.
API documentation	Swashbuckle (Swagger/OpenAPI)	Built into the ASP.NET Core template; useful for testing endpoints before frontend exists.
HTTP client	Fetch API (native)	No extra dependency needed for this project’s size.
Routing	React Router	Standard for role-dependent, multi-screen SPA navigation.


 2.	Application Architecture
Layer	Responsibility
Frontend	Renders UI, handles role-based navigation and client-side validation, calls the API. Contains no business logic — e.g. it does not calculate estimate totals itself, only displays what the API returns.
API layer (Controllers)	Receives HTTP requests, authenticates the user (JWT), authorizes by role, delegates to the Application layer, returns HTTP responses. Contains no business rules.
Application / business logic layer	Implements all business rules: estimate calculations, status transitions, company/role permission checks.
Data access layer	Entity Framework Core, entity configurations, migrations, queries. Translates application-layer requests into database operations.
Database	MySQL; persists all companies, users, materials, works, projects, and estimates.

3.	Repository Structure
/
├── server/
│   ├── Controllers/        # API endpoints — HTTP concerns only, no business logic
│   ├── Data/                # EF Core DbContext, database configuration
│   ├── Migrations/          # EF Core migrations
│   ├── Models/
│   │   ├── DTOs/             # Request/response objects exposed via the API
│   │   └── Entities/         # Domain entities (Company, User, Material, Work, Estimate, ...)
│   └── Services/             # Business logic, calculations, business rules
├── client/
│   └── src/
│       ├── api/               # Typed functions calling the backend API
│       ├── assets/            # Images, icons, static files
│       ├── interfaces/        # Shared TypeScript types/interfaces
│       ├── pages/              # Screen-level components (Dashboard, EstimateEditor, ...)
│       ├── styles/             # Global/shared CSS
│       └── utils/               # Helper functions (formatting, validation helpers)
├── tests/
│   ├── server/                # xUnit unit + integration tests
│   └── client/                # Vitest + React Testing Library tests
├── docs/
├── specs/                      # Spec Kit feature specifications
├── .github/
├── AGENTS.md
└── README.md
