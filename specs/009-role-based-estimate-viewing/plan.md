# Plan: Role-Based Estimate Viewing

## Constitution Check
No business logic in controllers or React components. No conflicts with the Constitution.

## Approach
Same estimate data, but the Application layer returns a different response shape for the Viewer role, excluding price/quantity/status.
