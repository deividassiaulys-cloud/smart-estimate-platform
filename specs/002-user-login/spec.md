# Feature: User Login

## Specification
As a user, I log in to reach my Dashboard. Invalid credentials show one generic error; 5 failed attempts in 10 minutes lock the account for 15 minutes.

## User Story (P1)
As a registered user, I want to log in with my email and password, so that I can access my company's data.

**Independent Verification:** A user with valid credentials reaches the Dashboard; a locked-out user sees a clear message.

## Acceptance Scenarios
Given a registered user, when they submit valid credentials, then they are authenticated and redirected to their Dashboard.

**Negative case:** when a user fails 5 login attempts within 10 minutes, the account is locked for 15 minutes.
