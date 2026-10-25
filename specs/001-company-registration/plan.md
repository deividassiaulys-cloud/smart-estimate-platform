# Plan: Company Registration

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
Create the Company and its Manager User together in one transaction. Hash the password with ASP.NET Core Identity’s PasswordHasher (Identity’s own user model is not used, so User stays a plain entity with a PasswordHash) and issue a JWT on success.

## Dependencies
- xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk (tests/server only): test framework and runner so `dotnet test` can run the required automated tests.
- Microsoft.EntityFrameworkCore.Sqlite (tests/server only): a real relational database in memory, so the integration tests exercise the unique email index and case-insensitive collation without needing a MySQL server.
