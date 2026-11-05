# Plan: Cross-Cutting Requirements

**Branch**: `012-cross-cutting-requirements` | **Date**: 2026-11-03 | **Spec**: [spec.md](spec.md)

## Summary

This feature finishes four non-functional requirements without adding business features, endpoints or database changes:
- **NFR-008, navigation and layout (US1, US2):**
  - A shared navigation (`AppLayout` + `NavBar`) on every logged-in page, with the company name and the role's items. Below 768 px it's a hamburger with a full-height panel.
  - The four lists, plus the two estimate lists, turn into cards below 768 px through shared table classes. Each cell is labelled with its column, so the markup isn't duplicated.
  - Wrapping headings no longer overlap.
  - The Viewer's Estimate View links back to its project.
- **NFR-010, errors (US3):** the client always shows its own Lithuanian text, which a table-driven test checks for every page when the server fails with English text. A server test checks that an unexpected failure returns no technical details.
- **NFR-001:** HSTS outside development.
- **NFR-012:** form fields prefilled with a decimal comma, Lithuanian percentages in the editor totals and the Excel labels.

Decisions are in [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10, TypeScript 6 + React 19

**Primary Dependencies**: unchanged (React Router 7 layout routes and `NavLink`, Tailwind CSS 4.3.3 `max-md:` variants). No new dependencies.

**Storage**: no change

**Testing**: Vitest + Testing Library (navigation, mobile panel, error messages per page), xUnit + WebApplicationFactory (HSTS, 500 without details, Lithuanian labels in the export)

**Project Type**: Web application

**Constraints**: from 360 to 1920 px with no horizontal page scroll; the breakpoint is 768 px (spec Q1); the server's error texts stay unchanged (spec Q2)

## Constitution Check

| Article | Check | Result |
|---|---|---|
| II.1 / III business logic | Navigation only shows links; authorization stays on the server and in the route guards. No business rule moves to the client | Pass |
| II.4 / X.1 DRY | One layout route instead of a header per page; one logout (spec Q3); cards made by shared classes in `utils/ui.ts` instead of a second card markup per list; one `toInputNumber` helper for prefilled numbers; one fallback error text | Pass |
| II.5 tests | Menu per role, mobile panel, every page's error message, HSTS on/off, 500 without details, Lithuanian export labels | Pass |
| III no features beyond spec | No new endpoints; the Viewer back link uses navigation state instead of changing the names API (the 009 types stay number-free); no Lithuanian server texts (Q2) | Pass |
| IV security | HSTS (NFR-001); no technical details in responses or on screen (NFR-010); menu items hidden by role, while the server still enforces 403 | Pass |
| VI / VIII | Spec/plan/tasks committed separately; no migration | Pass |
| X.7 | Tailwind only (`max-md:` variants), one component per file (`AppLayout`, `NavBar`, `MobileMenu`) | Pass |

**Post-design re-check**: Pass, Complexity Tracking empty.

## Project Structure

```text
client/src/
├── App.tsx                                   # logged-in routes nested under one layout route
├── components/AppLayout.tsx, NavBar.tsx, MobileMenu.tsx   # new
├── pages/Dashboard/DashboardPage.tsx         # DashboardHeader removed (spec Q3)
├── utils/ui.ts                               # h1 line height; table → card classes below 768 px
├── utils/format.ts                           # + toInputNumber, formatPercent
├── utils/errors.ts                           # new: one fallback text helper for unexpected failures
├── pages/**/…Table.tsx                       # data-label on cells for the cards
├── pages/Projects/ProjectEstimateNamesTable.tsx, pages/Estimates/EstimateViewPage.tsx   # back link via state
└── pages/… forms, EstimateTotals, EstimatePercentagesForm, EstimateWorkLineRow   # Lithuanian number display
server/SmartEstimatePlatform.Api/
├── Program.cs                                # UseHsts outside Development
└── Services/EstimateExportService.cs         # Lithuanian culture for percentage labels
tests/client/NavigationTests/, ErrorMessageTests/
tests/server/CrossCuttingTests/HstsTests.cs, UnexpectedErrorTests.cs; EstimateExportTests (labels)
```

## Implementation Order

1. **US1 navigation:** layout route, NavBar, MobileMenu, removing the Dashboard header, tests.
2. **US2 layout:** the h1 fix, cards for the lists, the Viewer back link, NFR-012 number display, a check at 360 / 768 / 1920 px.
3. **US3 errors:** a fallback helper where pages lack one, the table-driven client test, the server 500 test.
4. **NFR-001:** HSTS and its tests.

## Complexity Tracking

No violations.
