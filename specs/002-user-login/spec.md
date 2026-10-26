# Feature 2: User Login

## Specification
As a user, I log in to reach my Dashboard and I can log out again. Invalid credentials show one generic error; 5 failed attempts in 10 minutes lock the account for 15 minutes. After logout, protected pages redirect to Login.

## User Stories
**US1 (P1):** As a registered user, I want to log in with my email and password, so that I can access my company’s data.
**Independent verification:** A user with valid credentials reaches the Dashboard; a locked-out user sees a clear message.

**US2 (P1):** As a logged-in user, I want to log out, so that nobody else using my device can access my company’s data.
**Independent verification:** After choosing Log out, the user is on the Login page and opening a protected page redirects back to Login.

## Acceptance Scenarios
Given a registered user, when they submit valid credentials, then they are authenticated and redirected to their Dashboard, which greets them by name and shows their company, their role and the quick actions for that role (an action whose page does not exist yet is shown disabled; later features add their own summary figures). Given a logged-in user, when they choose Log out, then they return to the Login page and opening any protected page redirects them to Login. Negative case: when a user fails 5 login attempts within 10 minutes, the account is locked for 15 minutes.

## Edge Cases
- The same generic error is shown whether the email or the password is wrong.
- Email matching is case-insensitive, as in registration.
- Only existing accounts are counted and locked; an attempt with an unknown email just gets the generic error.
- A login token is valid for 60 minutes. When it has expired, the next protected page or API call that is rejected as unauthenticated removes the stored token and sends the user to Login with the message “Your session has expired, please log in again.”
- While the account is locked, every login attempt (correct or wrong password) is rejected with a clear “account locked, try again later” message that shows the minutes remaining.
- Failed attempts are counted from the first failure in the current streak: if 10 minutes pass without reaching 5 failures, the count starts over, and a successful login also resets it.

## Clarifications
- What does logging out do? The client discards its stored token and returns to Login. Tokens are not stored on the server, so an issued token simply expires after its configured lifetime; a token of a removed user is rejected immediately because the user is checked on every request.

### Session 2026-10-25
- Q: How should the "5 failed attempts in 10 minutes" window be counted? → A: From the first failure in the current streak; the count starts over after 10 minutes without reaching 5, and a successful login resets it.
- Q: While an account is locked, what should the login page say when someone tries to log in? → A: Any attempt, with a correct or wrong password, gets "account locked, try again later" with the minutes remaining.
- Q: What should the Dashboard show, given that projects, estimates and the catalog don't exist yet? → A: A greeting with the user's name, company and role, plus quick-action links for that role (disabled until their page exists); no summary figures yet, later features add theirs.
- Q: What should happen when a user's login token expires while they're using the app? → A: Tokens last 60 minutes; on expiry the token is removed and the user is sent to Login with "Your session has expired, please log in again."
