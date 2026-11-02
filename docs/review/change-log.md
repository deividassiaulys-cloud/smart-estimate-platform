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