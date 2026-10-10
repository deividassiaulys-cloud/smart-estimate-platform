# Plan: Company Data Isolation

## Constitution Check
No business logic in controllers or React components. No conflicts with the Constitution.

## Approach
A shared authorization check, applied centrally across all company-scoped requests, rather than repeated per endpoint. Built early (immediately after Feature 2 — Login) and applied to each controller as it's created, rather than retrofitted at the end.
