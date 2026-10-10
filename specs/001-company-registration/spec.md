# Feature: Company Registration

## Specification
As a visitor, I register and my company is created at the same time, so I become its Manager. Must reject duplicate emails and weak passwords.

## User Story (P1)
As a visitor, I want to register and have my company created at the same time, so that I can start working immediately as its Manager.

**Independent Verification:** A visitor completes registration with valid data and reaches their Dashboard as Manager.

## Acceptance Scenarios
Given a visitor on the Register page, when they submit valid registration data, then a company and Manager account are created and they are logged in to the Dashboard.

**Negative case:** when a visitor submits an email already in use, the registration is rejected and no account or company is created.

## Edge Cases
- Email matching is case-insensitive.
- Two simultaneous registration requests with the same email: only one succeeds, the other is rejected as a duplicate.

## Clarifications
- Can a company name be duplicated across different companies? Only the account email must be unique, not the company name.
