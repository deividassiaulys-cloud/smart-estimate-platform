# Plan: Company Registration

## Constitution Check
No business logic in controllers or React components. No authentication/authorization skipped. No hardcoded secrets. No conflicts with the Constitution.

## Approach
Extend the existing ASP.NET Core Identity setup to create a User and Company together, in one transaction, and issue a JWT on success.
