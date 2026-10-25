# Plan: Company Registration

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
Create the Company and its Manager User together in one transaction. Hash the password with ASP.NET Core Identity’s PasswordHasher (Identity’s own user model is not used, so User stays a plain entity with a PasswordHash) and issue a JWT on success.
