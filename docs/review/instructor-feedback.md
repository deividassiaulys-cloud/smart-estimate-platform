# Instructor feedback

| # | Remark | Decision | Status |
|---|---|---|---|
| 1 | Controllers must not contain logic; extract it | Thin controllers, logic in services (Article X.2) | planned |
| 2 | DTOs in their own files in the DTO folder | One DTO per file under Models/DTOs (X.4) | planned |
| 3 | DTOs separate from entities | DTOs do not reference entities; one mapper (X.4) | planned |
| 4 | Use records where possible | DTOs, options, results are records; entities stay classes (X.5) | planned |
| 5 | Entities with private setters | Private setters, no business rules in entities (X.6) | DECISION A |
| 6 | Is CancellationToken needed at all? | | DECISION B |
| 7 | Password length checks in one place | | DECISION C |
| 8 | Every service has its own interface | IXxxService per service (X.3) | planned |
| 9 | Use Tailwind, avoid CSS files | Tailwind utilities only (X.7) | planned |
| 10 | Extract Dashboard / QuickActions | One component per file (X.7) | planned |
| 11 | Avoid repeated ProducesResponseType boilerplate | Common responses once in a base controller (X.8) | planned |
| 12 | Follow DRY, KISS, YAGNI | Article X.1 | planned |