# Instructor feedback

| # | Remark | Decision | Status |
|---|---|---|---|
| 1 | Controllers must not contain logic; extract it | Thin controllers, logic in services (Article X.2) | planned (013) |
| 2 | DTOs in their own files in the DTO folder | One DTO per file under Models/DTOs (X.4) | planned (013) |
| 3 | DTOs separate from entities | DTOs do not reference entities; one mapper (X.4) | planned (013) |
| 4 | Use records where possible | DTOs, options, results are records; entities stay classes (X.5) | planned (013) |
| 5 | Entities with private setters | A1: private setters, constructors and simple mutators without business rules; rules stay in services (X.6) | planned (013) |
| 6 | Is CancellationToken needed at all? | Not needed: removed from controllers and services; added only when a spec requires cancelling a long-running operation (X.9) | planned (013) |
| 7 | Password length checks in one place | One shared rule definition applied declaratively on the request records; services do not repeat it (X.10). The client keeps one documented mirror (utils/validation.ts) for immediate Lithuanian feedback | planned (013) |
| 8 | Every service has its own interface | IXxxService per service (X.3) | planned (013) |
| 9 | Use Tailwind, avoid CSS files | Tailwind utilities only (X.7) | planned (013) |
| 10 | Extract Dashboard / QuickActions | One component per file (X.7) | planned (013) |
| 11 | Avoid repeated ProducesResponseType boilerplate | Common responses once in a base controller (X.8) | planned (013) |
| 12 | Follow DRY, KISS, YAGNI | Article X.1 | planned (013) |