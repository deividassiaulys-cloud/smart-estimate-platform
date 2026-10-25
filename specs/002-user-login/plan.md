# Plan: User Login

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
Verify credentials with Identity’s PasswordHasher; keep the failed-attempt counter and lock-out time on User. Issue a JWT Bearer token. Logout is client-side: remove the stored token and redirect to Login. The Landing Page and Dashboard are plain pages without feature logic.
