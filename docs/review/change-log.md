# Change log

## 2026-11-17 — Removed the OpenAPI document (feature 018)

Source: specs/018-remove-openapi.
Reason: nothing read the generated OpenAPI description. The React client uses its own TypeScript interfaces,
and there is no Swagger UI and no client generation. Its 89 [ProducesResponseType] attributes had to be kept in
sync with the code by hand.

- Removed AddOpenApi()/MapOpenApi(), the Microsoft.AspNetCore.OpenApi package, the never-used
  Swashbuckle.AspNetCore package and every [ProducesResponseType] attribute.
- Changed docs/architecture.md: the hand-written contracts in specs/*/contracts are the endpoint documentation.
- Not changed: every route, status code, response body, authorization rule and validation response;
  all requirements (FR/NFR/BR).

---

## 2026-11-17 — Higher price and norm precision (gap A)

Source: docs/reference/ANALYSIS.md §3, gap A; feature specs/016-price-norm-precision.
Reason: real sample estimates need sub-cent prices and norms below 0,0005. With 4 / 6 decimal places the
sample estimate totals 135 414,34 € against 135 414,29 € in the source PDF (135 385,31 € with today's 2 / 3).

- Changed NFR-006 (unit prices shown with 2–4 decimal places) and BR-017 (prices up to 4 decimals, norms up to 6,
  quantities up to 3; norms shown with up to 6, spec 016 clarification Q1).
- Changed specs/shared-technical-rules.md: DECIMAL(18,4) for prices and labour rates, DECIMAL(18,6) for norms.
- Not changed: BR-016 calculation, the Out of Scope list.

---

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