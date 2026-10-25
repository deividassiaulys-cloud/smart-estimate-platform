# Feature 2: User Login

## Specification
As a user, I log in to reach my Dashboard and I can log out again. Invalid credentials show one generic error; 5 failed attempts in 10 minutes lock the account for 15 minutes. After logout, protected pages redirect to Login.

## User Stories
**US1 (P1):** As a registered user, I want to log in with my email and password, so that I can access my company’s data.
**Independent verification:** A user with valid credentials reaches the Dashboard; a locked-out user sees a clear message.

**US2 (P1):** As a logged-in user, I want to log out, so that nobody else using my device can access my company’s data.
**Independent verification:** After choosing Log out, the user is on the Login page and opening a protected page redirects back to Login.

## Acceptance Scenarios
Given a registered user, when they submit valid credentials, then they are authenticated and redirected to their Dashboard. Given a logged-in user, when they choose Log out, then they return to the Login page and opening any protected page redirects them to Login. Negative case: when a user fails 5 login attempts within 10 minutes, the account is locked for 15 minutes.

## Edge Cases
- The same generic error is shown whether the email or the password is wrong.
- A locked-out user sees a clear “try again later” message.

## Clarifications
- What does logging out do? The client discards its stored token and returns to Login. Tokens are not stored on the server, so an issued token simply expires after its configured lifetime; a token of a removed user is rejected immediately because the user is checked on every request.
