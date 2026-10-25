# Plan: Role-Based Estimate Viewing

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
Same estimate data, but the Application layer returns a different response shape for the Viewer role, excluding price, quantity and status.
