# Plan: Estimate Export

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
The Application service builds the export data from the same source used on screen; ClosedXML generates the file.
