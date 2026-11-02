# Quickstart: Validating the Code Structure Refactor

## Prerequisites

.NET 10 SDK with `dotnet-ef` 10.x, Node 20+, the repository on branch `013-code-structure-refactor`.

## After every step

```bash
dotnet build && dotnet test                     # repository root
cd client && npm run lint && npm run build && npm run test
```

Expected: 0 build errors, all tests green, 0 lint errors.

## After steps that touch entities or the DbContext

```bash
dotnet ef migrations has-pending-model-changes --project server/SmartEstimatePlatform.Api
```

Expected: "No changes have been made to the model since the last migration." `git status server/SmartEstimatePlatform.Api/Migrations` shows nothing.

## Behaviour checks

- **Error responses**: the step-0 tests in `tests/server/ErrorResponseTests` pass before and after every step (SC-003).
- **API contract**: compare `/openapi/v1.json` (Development) before and after. The only differences allowed are those in [contracts/api-changes.md](contracts/api-changes.md).
- **Frontend**: `npm run dev` with the API running, then go through Landing → Register → Dashboard → Materials (search, add, edit, delete with confirmation) → Team (add Viewer, reset password, remove with confirmation) → Logout → Login with a wrong password 5 times (lock message). Content, buttons and navigation should match the previous version.

## Structure checks (SC-004, SC-005)

```bash
grep -rn "try\s*$\|catch" server/SmartEstimatePlatform.Api/Controllers     # only DomainExceptionHandler
grep -rn "CancellationToken" server/SmartEstimatePlatform.Api --include=*.cs | grep -v Migrations
                                                                           # only TryHandleAsync and SaveChangesAsync
grep -rn "{ get; set; }" server/SmartEstimatePlatform.Api/Models/Entities  # nothing
find client/src -name "*.css"                                              # only client/src/index.css
```
