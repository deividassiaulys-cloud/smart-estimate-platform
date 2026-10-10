# Feature: Estimate Export

## Specification
As a Manager, I export an estimate to Excel matching what I see on screen. Viewer export attempts are rejected.

## User Story (P1)
As a Manager, I want to export an estimate to Excel, so that I can send it to a client or keep an offline copy.

**Independent Verification:** The exported file's data matches the on-screen estimate.

## Acceptance Scenarios
Given a Manager on an estimate, when they confirm the export action, then a file downloads matching the on-screen data.

**Negative case:** when a Viewer attempts to export an estimate, the request is rejected.

## Edge Cases
- Values that look like formulas are exported as plain text.

## Clarifications
- Can a Draft (not yet Confirmed) estimate be exported? Yes — export is available regardless of status, reflecting current data.
