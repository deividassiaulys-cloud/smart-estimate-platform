# Research: Cross-Cutting Requirements

## R1: Navigation (US1, spec Q3)

- **Decision**: one React Router **layout route** wraps every logged-in route.
  - `<Route element={<ProtectedRoute><AppLayout /></ProtectedRoute>}>` contains the pages. `ManagerRoute` stays on the Manager-only pages.
  - `AppLayout` renders `NavBar` and an `<Outlet />`. Landing, Login and Register stay outside it, so they get no menu.
- **NavBar**:
  - The company name on the left, from `getCurrentUser()`, loaded once by the layout.
  - The role's items as `NavLink` (marked with `aria-current="page"` on the active one): Manager gets Pagrindinis /dashboard, Projektai /projects, Medžiagos /materials, Darbai /works, Komanda /team; Viewer gets Pagrindinis and Projektai.
  - "Atsijungti" runs the existing `logout()` and goes to `/login`.
- **Mobile**:
  - Below 768 px (`max-md:`) the items are replaced by a "Meniu" button (`aria-expanded`, `aria-controls`).
  - It opens `MobileMenu`, a fixed full-height panel with the same items stacked.
  - Escape, choosing an item, or "Uždaryti" closes it, and focus returns to the button.
  - The menu also closes on any route change, including after a session expires.
- **Dashboard**: `DashboardHeader` (company name + logout) is removed. The greeting and quick actions stay.
- **Alternatives considered**: a NavBar on every page, rejected as duplication. Hiding the Dashboard's logout instead, rejected because it would give two places with the same job.

## R2: Lists as cards below 768 px (US2, spec Q1)

- **Decision**: CSS-only cards via the shared classes in `utils/ui.ts`. Below 768 px:
  - the table, `tbody` and rows display as blocks, each row a bordered card
  - the `thead` is hidden
  - each cell is a flex line whose label comes from its `data-label` attribute, through `before:content-[attr(data-label)]`
  - the first cell, the name, has no label and appears bold as the card title
  - the actions cell lays its buttons out in a row

  Each `…Table` component only adds `data-label` to its cells and the new row/head classes. Applies to the Projects, Materials, Works and Team tables and the two estimate lists on Project Detail.
- **Estimate Editor**: its line table keeps its own horizontal scroll inside `tableWrapper` (spec assumption).
- **Rationale**: no second markup per list (DRY); the cards are built from the same data and actions.
- **Alternatives considered**: separate card components per list. Rejected: four extra components repeating every field.

## R3: Heading overlap (US2)

- **Cause**: `<body>` has `leading-[145%]` (013, to match the old CSS). A percentage line height is inherited as a **fixed length** (145 % of 16–18 px), so a 36–56 px `h1` on two lines overlaps.
- **Decision**: give `ui.h1` its own line height, `leading-tight` (1.25 × its own font size). Other text keeps the body's.

## R4: Viewer back link (US2)

- **Decision**: `ProjectEstimateNamesTable` links with `state={{ projectId }}`. `EstimateViewPage` reads `useLocation().state?.projectId` and links to `/projects/{id}` ("← Projektas"); without the state, for example a bookmark, it links to `/projects`.
- **Rationale**: no change to the 009 names API, whose types deliberately carry no numbers except the estimate ID.

## R5: Lithuanian numbers (NFR-012)

- **Findings from the audit**:
  - Forms prefill numbers with a point: `toFixed(2)` / `String(...)` in MaterialFormPage, WorkFormPage, EstimateWorkLineRow, EstimatePercentagesForm.
  - EstimateTotals shows `${percent} %`, which gives "12.5 %".
  - EstimateExportService builds its percentage labels with the server's current culture.
- **Decision**:
  - `toInputNumber(value)` in `utils/format.ts`: the number with a decimal comma, no grouping, the minimum decimals (e.g. 12,5 / 918,75); money fields keep 2 decimals (12,50).
  - `formatPercent(value)`: "12,5 %".
  - The export uses `CultureInfo("lt-LT")` for its labels.
  - `parseDecimal` already accepts a comma, so saving is unchanged.

## R6: Errors in Lithuanian (US3, spec Q2)

- **Findings**: every page already maps known statuses to Lithuanian texts and never displays `ApiError.message` (grep shows no `.message` in pages or components). The English fallback in `ApiError` exists only for developers.
- **Decision**:
  - Keep the server texts.
  - Add `utils/errors.ts` with `unexpectedError(action: string)`, which returns "Nepavyko {action}. Bandykite dar kartą.", for the generic branches, so the wording is one rule.
  - A **table-driven frontend test** opens every page with a mocked API that always fails (500 with the English `detail` "Internal failure XYZ"):
    - list and detail pages fail on load
    - form pages fail on submit after valid input

    For each page it asserts that a Lithuanian "Nepavyko…"/"…nepavyko…" message is shown, that neither "XYZ" nor "Request failed" appears, and that the navigation stays usable.
- **Server**: a test replaces a service with one that throws `InvalidOperationException("secret detail")` and checks the 500 response:
  - it's ProblemDetails
  - it doesn't contain "secret detail", a stack trace or an exception type name
  - `UseExceptionHandler` already does this outside Development; the test pins it

## R7: HSTS (NFR-001)

- **Decision**: `if (!app.Environment.IsDevelopment()) app.UseHsts();` before `UseHttpsRedirection`, with the default max-age (30 days).
- **Test**: through `https://smart-estimate.test`, because HSTS skips localhost and plain HTTP by design. In the "Testing" environment the `Strict-Transport-Security` header is present; with the environment set to Development, it's absent.
