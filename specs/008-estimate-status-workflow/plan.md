# Plan: Estimate Status Workflow

## Constitution Check
No business logic in controllers or React components. No conflicts with the Constitution.

## Approach
Add a Status field to Estimate; status transitions and the edit-lock are enforced in the Application layer on every write.
