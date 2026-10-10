# Plan: User Login

## Constitution Check
No business logic in controllers or React components. No conflicts with the Constitution.

## Approach
Use ASP.NET Core Identity's password verification, with a failed-attempt counter added to User for lockout.
