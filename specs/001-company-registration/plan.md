# Plan: Company Registration

## Constitution Check
No business logic in controllers or React components. No authentication/authorization skipped. No hardcoded secrets. No conflicts with the Constitution.

## Approach
Create a User and Company together, in one transaction, and issue a JWT on success.

## Decisions
- ASP.NET Core Identity is used only for its password hasher (`PasswordHasher<User>`), not for its entity model. `User` is a plain entity class, not an `IdentityUser`, and stores the hash in `PasswordHash`.
- The user's role (Manager / Viewer) is stored as a `Role` field on `User`, not in Identity role tables.
- `Company` and `User` use integer IDs.
