# Tasks: Cross-Cutting Requirements

**Input**: spec.md, plan.md, research.md, data-model.md, contracts/ui-navigation.md, quickstart.md in `specs/012-cross-cutting-requirements/`

`[US#]` refers to the user stories of this feature in `spec.md`. The IDs T111–T113 are kept, with their content updated to the clarified spec. New tasks continue the project-wide numbering from T222.

**Rule for every task**: after each task, `dotnet build`, `dotnet test`, `npm run lint`, `npm run build` and `npm run test` must all pass. There's no migration, endpoint or data change in this feature.

## Phase 1: User Story 1: Navigate from anywhere (P1) 🎯 MVP

**Goal**: Every logged-in page has the role's menu, which becomes a hamburger panel below 768 px.
**Independent Test**: T223 passes, and quickstart.md steps 1, 2 and 6 work.

- [X] T113 [US1] Add client/src/components/AppLayout.tsx, NavBar.tsx and MobileMenu.tsx, one component per file, Tailwind:
  - **AppLayout**: loads the company name once (`getCurrentUser`) and renders `NavBar` plus an `<Outlet />`.
  - **NavBar**: the company name and the role's `NavLink`s, as in contracts/ui-navigation.md (`aria-current` on the active one), plus "Atsijungti" (logout → /login). Below 768 px the links are replaced by a "Meniu" button (`aria-expanded`, `aria-controls`).
  - **MobileMenu**: a fixed full-height panel with the same items stacked and "Uždaryti". Escape, an item or "Uždaryti" closes it and returns focus to the button; it also closes on any route change.
- [X] T222 [US1] Nest every logged-in route under one layout route `<Route element={<ProtectedRoute><AppLayout /></ProtectedRoute>}>` in client/src/App.tsx, keeping `ManagerRoute` on the Manager-only pages. Landing, Login and Register stay outside. Remove client/src/pages/Dashboard/DashboardHeader.tsx and its use in DashboardPage (spec Q3)
- [X] T223 [US1] Add a frontend test in tests/client/NavigationTests/Navigation.test.tsx:
  - a Manager's menu shows the company name and exactly Pagrindinis, Projektai, Medžiagos, Darbai, Komanda and Atsijungti
  - a Viewer's shows exactly Pagrindinis, Projektai and Atsijungti
  - the current page's link has `aria-current="page"`
  - "Atsijungti" logs out to Login, and the Dashboard has only one logout
  - "Meniu" opens the panel; Escape closes it; choosing an item navigates and closes it
  - Landing, Login and Register have no menu
  - with the panel open, a session expiry (`sessionExpiredEvent`) shows Login with no menu (spec edge case)
  - the existing tests/client/LogoutTests/Logout.test.tsx keeps passing unchanged; jsdom loads no CSS, so the single logout is counted with the panel closed

## Phase 2: User Story 2: Use the app on a phone (P1)

**Goal**: From 360 to 1920 px there's no horizontal page scroll; lists are cards below 768 px; headings never overlap; numbers look Lithuanian.
**Independent Test**: T227 passes, and quickstart.md steps 3–5, 6 and 8 work.

- [X] T224 [US2] In client/src/utils/ui.ts, give `h1` its own `leading-tight` (it fixes the two-line overlap, research R3). Add row/head/cell classes that turn a table into cards below 768 px:
  - the table, body and rows become blocks, each row a bordered card
  - the head is hidden
  - each cell is a flex line labelled by `before:content-[attr(data-label)]`
  - the first cell is the bold title with no label
  - the actions cell lays its buttons out in a row
- [X] T225 [US2] Apply the card classes and add `data-label` to every cell in client/src/pages/Projects/ProjectsTable.tsx, ProjectEstimatesTable.tsx, ProjectEstimateNamesTable.tsx, Materials/MaterialsTable.tsx, Works/WorksTable.tsx and Team/TeamTable.tsx. The Estimate Editor table keeps its own frame scroll
- [X] T226 [US2] Make the Viewer's Estimate View link back to its project: `ProjectEstimateNamesTable` passes `state={{ projectId }}`, and `EstimateViewPage` shows "← Projektas" to `/projects/{projectId}`, or "← Projektai" to `/projects` without state. In client/src/pages/Projects/ProjectEstimateNamesTable.tsx and client/src/pages/Estimates/EstimateViewPage.tsx
- [X] T112 [US2] Add `toInputNumber(value, decimals?)` (a decimal comma, no grouping; money fields keep 2 decimals) and `formatPercent(value)` ("12,5 %") to client/src/utils/format.ts. Use them:
  - for the prefilled inputs in MaterialFormPage, WorkFormPage, EstimateWorkLineRow and EstimatePercentagesForm
  - for the percentage labels in EstimateTotals

  Format the export's percentage labels with `CultureInfo("lt-LT")` in server/SmartEstimatePlatform.Api/Services/EstimateExportService.cs (NFR-012)
- [X] T227 [US2] Add tests:
  - in tests/client/NavigationTests/Layout.test.tsx: list cells carry their `data-label`s; the Viewer's Estimate View links to `/projects/4` when opened from that project and to `/projects` otherwise; the material form prefills "4,50"; the editor totals show "12,5 %"
  - in tests/server/EstimateExportTests/EstimateExportTests.cs: the percentage labels read "Pridėtinės išlaidos (12,5 %)" for 12.5 %
- [X] T228 [US2] Check every page at 360, 768 and 1920 px (Dashboard, Projects, Project Detail, project form, Materials, material form, Works, work form, Team, Viewer and reset forms, Estimate create, Editor, View). There must be no horizontal page scroll (`document.documentElement.scrollWidth <= innerWidth`), cards below 768 px, and no overlapping headings. A Chrome window can't be made narrower than about 600 px, so measure through a temporary untracked same-origin iframe harness under client/ (deleted afterwards), with frames 360, 768 and 1920 px wide, reading `scrollWidth` inside each; use a mock API if the real one isn't configured. Fix any finding in client/src

## Phase 3: User Story 3: Clear errors in Lithuanian (P2)

**Goal**: Users only ever see Lithuanian "what failed" messages; responses carry no technical details.
**Independent Test**: T229 and T230 pass, and quickstart.md step 7 works.

- [X] T111 [US3] Add client/src/utils/errors.ts with `unexpectedError(action)`, which returns "Nepavyko {action}. Bandykite dar kartą.". Use it for the generic (non-status-specific) branches of every page's error handling; the server texts are unchanged (spec Q2)
- [X] T229 [US3] Add a table-driven frontend test in tests/client/ErrorMessageTests/ErrorMessages.test.tsx. For every page, with a mocked API answering 500 with `detail` "Internal failure XYZ" (list and detail pages on load; forms on submit after valid input):
  - a Lithuanian message containing "Nepavyko" or "nepavyko" is shown
  - neither "XYZ" nor "Request failed" appears
  - the navigation menu stays visible for logged-in pages
- [X] T230 [US3] Add a server test in tests/server/CrossCuttingTests/UnexpectedErrorTests.cs: with `IProjectService` replaced by one that throws `InvalidOperationException("secret detail")`, `GET /api/projects` returns 500 ProblemDetails without "secret detail", a stack trace or the exception's type name

## Phase 4: Polish (NFR-001 and checks)

- [X] T231 Add `app.UseHsts()` outside Development, before `UseHttpsRedirection`, in server/SmartEstimatePlatform.Api/Program.cs. Test in tests/server/CrossCuttingTests/HstsTests.cs, through `https://smart-estimate.test`:
  - in the Testing environment the response has `Strict-Transport-Security`
  - in Development it doesn't
- [X] T232 Run the full check set and quickstart.md; fix any regression

## Dependencies

- T113 → T222 → T223.
- T224 → T225 → T227. T226 is independent. T112 is independent; its test is in T227. T228 needs T222, T224, T225 and T112.
- T111 → T229. T230 is independent.
- T231 is independent. T232 runs last.

## Parallel Execution Examples

- Server tasks (T230, T231, the export part of T112) in parallel with the client navigation (T113/T222).
- T226, T112 and T111 touch different files and can run in parallel after T222.

## Implementation Strategy

US1 (navigation) is the MVP, because every other page check assumes the layout. Then US2 (layout and numbers), then US3 (errors), then HSTS. Spec/plan/tasks commits stay separate from code commits (Article VI).
