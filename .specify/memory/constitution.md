# Smart Estimate Platform — Project Constitution

## Preamble — Purpose

This Constitution establishes the engineering principles, constraints, and expected practices for building the Smart Estimate Platform. It ensures consistency, quality, and architectural integrity across all features, whether implemented by a human or an AI coding assistant.

## Article I — Core Values

- **Correctness:** Business calculations (prices, quantities, totals) MUST be accurate and verifiable.
- **Clarity:** Code and documentation MUST be understandable without requiring the author's explanation.
- **Simplicity:** Prefer the simplest solution that satisfies the specification.
- **Security:** Protect user credentials and company data isolation at all times.
- **Traceability:** Every implemented behavior MUST trace back to a documented requirement.
- **Accountability:** All code, human- or AI-written, is reviewed and owned by the student before being committed.

## Article II — Behavioral Directives (Engineering Practices)

1. Business rules MUST be implemented only in the Application layer, never in controllers, UI components, or data-access code.
2. Public classes and methods in Application and Domain MUST have doc comments explaining purpose, not implementation detail.
3. Code MUST follow consistent naming conventions: PascalCase for C# public members, camelCase for TypeScript.
4. Duplicated logic MUST NOT exist — shared calculations (e.g. estimate totals) live in exactly one place and are called, not reimplemented.
5. Every functional requirement (FR-xxx) involving a business rule MUST have at least one automated test before it is considered done.
6. A simpler solution that meets the specification is preferred over a more complex one, even if the complex one seems more "robust."

## Article III — Red Lines / Prohibitions

- Business logic MUST NOT be implemented in API controllers or React components.
- Passwords MUST NOT be stored or logged in plain text.
- Secrets (connection strings, JWT keys) MUST NOT be committed to the repository.
- Cross-company data access MUST NOT be allowed, even if only filtered in the UI rather than blocked at the API.
- A pull request MUST NOT be merged with failing automated tests.
- New dependencies MUST NOT be added without a documented reason; "the AI assistant suggested it" is not a valid reason.
- Features MUST NOT be implemented beyond what the current specification requires.

## Article IV — Security & Risk Policies

- All API endpoints MUST verify both authentication (valid JWT) and authorization (correct role and company) before returning data.
- Passwords MUST be hashed.
- Role-based and company-isolation rules MUST be covered by integration tests, not unit tests alone.
- When a proposed design risks violating a security principle, the risk MUST be documented in the feature's plan before implementation proceeds.

## Article V — AI-Assisted Development Rules

- AI agents operating in this repository MUST follow AGENTS.md and MUST NOT bypass the Spec Kit workflow (spec → clarify → plan → tasks → analyze → implement).
- AI-generated code MUST be reviewed and understood by the student before being committed; it MUST NOT be committed unreviewed.
- AI-generated architectural suggestions (e.g. in data-model.md, plan.md) are proposals only and MUST be explicitly accepted or rejected by the student.
- An AI agent MAY adopt a feature-specific working context (e.g. "focus only on the Estimates feature"), but remains bound to this Constitution regardless of that context.

## Article VI — Documentation & Version Control

- Every feature MUST have a corresponding specification in specs/ before implementation begins.
- Each feature MUST be developed on its own branch, created from an up-to-date main.
- Commit messages MUST follow the Conventional Commits format (e.g. feat(estimates): add status transition).
- Spec Kit workflow changes (constitution, specs, plans, tasks) MUST be committed separately from implementation code changes.
- README.md MUST be updated whenever setup or run instructions change.

## Article VII — Error Handling (Specification Ambiguity)

When a specification is ambiguous or incomplete:
1. Use the Spec Kit clarification stage (/speckit.clarify) before technical planning.
2. Do not proceed to implementation while unresolved clarification markers remain, unless the instructor explicitly approves the uncertainty.
3. When a requested design is impossible or conflicts with this Constitution, the conflict MUST be documented and resolved before coding — not silently worked around.

## Article VIII — Autonomy Constraints (for AI Agents)

- An AI agent may generate code and modify files only within the scope of the currently active feature task.
- An AI agent MUST NOT create, modify, or delete files outside server/, client/, tests/, docs/, specs/, or this Constitution's own governance process.
- An AI agent MUST NOT edit .env files, generated build output, or database migration files unless the active task explicitly requires a new migration.
- Every AI-driven change MUST be reviewable as a diff before commit (no silent/automatic commits).

## Article IX — Governance & Versioning

- **Version:** 1.2.0
- **Owner:** Deividas Šiaulys
- **Revision Method:** Amendments require explicit student (and, where applicable, instructor) approval; AI agents MUST NOT self-amend this Constitution.
- **Versioning scheme:** Semantic versioning — MAJOR for principle removal/redefinition, MINOR for new principles, PATCH for wording clarifications.
- **Compliance:** Every feature's technical plan MUST include a Constitution Check confirming the design does not violate any article of this Constitution.
- **Exceptions:** Allowed only when strictly necessary; MUST name the violated section, state the reason, and be recorded in the feature's plan.md. Undocumented violations are not permitted.
- **Change Log:**
  - 1.1.0 (2026-11-02) — Added Article X (Code Structure and Design Principles) based on instructor feedback (docs/review/instructor-feedback.md).
  - 1.2.0 (2026-11-02) — Added Article X.9 (CancellationToken only when a specification requires cancellation) and X.10 (declarative request validation, one shared definition per rule).

## Article X — Code Structure and Design Principles

1. DRY, KISS and YAGNI apply to all code. Duplicated code MUST be extracted when it appears a second time; abstractions, parameters, options and layers that no current requirement needs MUST NOT be added.
2. Controllers MUST only bind the request, call one service method and map the result to an HTTP response. They MUST NOT contain validation logic, queries, mapping or business rules.
3. Every application service MUST have its own interface (IXxxService) and MUST be consumed through that interface by dependency injection. Pure static helpers (e.g. MoneyRounding) do not need one.
4. DTOs MUST be declared in their own files under Models/DTOs, never inside controllers or services, and MUST NOT reference entity types. Mapping between entities and DTOs MUST happen in one explicit place.
5. DTOs, options and result types MUST be records. EF Core entities, services and exceptions MUST be classes.
6. Entity properties MUST have private setters. State is set through constructors and small mutator methods that contain no business rules (rules stay in services, Article II).
7. Frontend styling MUST use Tailwind utility classes; new CSS files MUST NOT be added. A component file MUST contain one component, and distinct page sections MUST be extracted into their own components.
8. Repeated boilerplate (response-type attributes, error handling, mapping) MUST be defined once and reused.
9. CancellationToken MUST NOT be added unless a specification requires cancellation for a specific long-running operation.
10. Request validation MUST be declarative on the request records (data annotations, IValidatableObject) with one shared definition per rule (e.g. password rules); services MUST NOT repeat it.
