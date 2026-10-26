# Plan: Estimate Status Workflow

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
Add Status (default Draft) to Estimate; status transitions and the edit lock are enforced in the Application layer on every write, including adding or removing a line and changing percentages. The estimate list returns Status to Managers and omits it for Viewers.
