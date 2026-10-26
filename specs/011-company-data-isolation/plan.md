# Plan: Company Data Isolation

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
A shared authorization check, applied centrally across all company-scoped requests, rather than repeated per endpoint. It is built immediately after Login and applied to each controller as that controller is created.
