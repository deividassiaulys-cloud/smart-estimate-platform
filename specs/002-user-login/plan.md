# Plan: User Login

## Constitution Check
No business logic in controllers or React components, no authentication/authorization skipped, no hardcoded secrets. No conflicts with the Constitution — no exceptions needed.

## Approach
Verify credentials with Identity’s PasswordHasher (email matched case-insensitively, as in registration). Lockout state lives on User: FailedLoginCount, FirstFailedLoginAt and LockoutEnd (new migration). Only existing accounts are counted and locked; an unknown email just gets the generic error. A locked attempt returns the minutes remaining so the Login page can show them. The current time comes from .NET’s built-in TimeProvider so tests can move the clock.

Reuse TokenService from feature 001 to issue the JWT (60-minute lifetime). The API validates JWT Bearer tokens on every request and rejects a token whose user no longer exists. GET /api/auth/me (authenticated) returns the user’s name, company name and role for the Dashboard.

Logout is client-side: remove the stored token and redirect to Login. A 401 from the API does the same, with a “session expired” message. The Landing Page links to Login and Register; the Dashboard greets the user and shows role-based quick actions (disabled until their page exists), with Log out in its header.

## Dependencies
- react-router-dom (client): routing and redirecting protected pages to Login.
- vitest, @testing-library/react, @testing-library/user-event, jsdom (client, dev only): run the frontend tests required by T018 (`npm run test`).
