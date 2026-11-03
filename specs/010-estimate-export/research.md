# Research: Estimate Export

## R1: Library

- **Decision**: `ClosedXML` 0.105.1.
  - MIT licence.
  - Published 2026-07-25, so more than two weeks old.
  - Targets .NET Standard 2.0/2.1, so it runs on .NET 10.
  - Built on Microsoft's `DocumentFormat.OpenXml`.
- **Rationale**: FR-027 and requirements §11 require a server-side `.xlsx`. ClosedXML gives number formats and `Style.IncludeQuotePrefix` (needed for NFR-007) in a few lines, and the tests can read the generated file back with the same library.
- **Alternatives considered**:
  - Raw `DocumentFormat.OpenXml`: same output, but many more lines of low-level XML parts. Rejected as unnecessary complexity (Article II.6).
  - EPPlus: commercial licence since v5. Rejected.
  - CSV: not Excel, and more exposed to formula injection. Rejected.

## R2: Same data as the screen (FR-027)

- **Decision**: `EstimateExportService.ExportAsync(id)` takes the estimate from `IEstimateService.GetAsync(id)`, which is the response the editor renders, and the project name from `IProjectService.GetAsync`. It writes exactly those values.
- **Rationale**: no second calculation or query. If the screen is right, the file is right, and company scoping (404) comes for free.

## R3: Layout (spec Q1)

- **Decision**: one sheet named "Sąmata".

  | Rows | Content |
  |---|---|
  | 1–4 | `Projektas`, `Sąmata`, `Būsena`, `Eksportuota`, each with its value (status name in Lithuanian; export date `yyyy-MM-dd` in Europe/Vilnius, NFR-012) |
  | Table header | Pavadinimas, Mato vnt., Kiekis, Norma, Atliekos %, Kaina / Įkainis, Suma |
  | Each work line | name, unit, quantity, labour norm, (empty), labour rate, labour cost |
  | Its material rows | indented name, unit, quantity, consumption norm, waste %, price, cost |
  | Totals | Tiesioginės išlaidos, Pridėtinės išlaidos (p %), Pelnas (p %), PVM (p %), Iš viso su PVM |
- **Column widths**: adjusted to the contents.

## R4: Cells (spec Q2, NFR-007)

- **Numbers**: amounts, quantities, norms and percentages are written as `decimal` number cells with exactly the server's values; nothing is computed and there are no formulas.

  | Kind | Number format | Shows |
  |---|---|---|
  | Money | `#,##0.00 "€"` | 1 263,26 € (with Lithuanian regional settings) |
  | Quantities and norms | `#,##0.###` | up to 3 decimals |
  | Percentages | `0.## "%"` | the value stays 21, matching the screen, not 0.21 |
- **Text**: names, units, labels and the status are written through **one helper** that sets the value as text and `IncludeQuotePrefix = true`. A value like `=HYPERLINK(...)` or `-5` is therefore stored as text and never evaluated (NFR-007). Every text cell gets it, so there's no exception to forget.

## R5: HTTP and file name

- **Decision**: `GET /api/estimates/{id}/export` (Manager only) returns `ExportFile` as `File(content, contentType, fileName)`:
  - content type `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
  - file name = the estimate name with characters invalid in file names replaced by `_`, plus `.xlsx`
  - ASP.NET Core writes `Content-Disposition` with an RFC 5987 `filename*`, so Lithuanian letters survive
- **Errors**: 404 for a missing or another company's estimate; 403 for a Viewer; 401 anonymous.
- **Status**: Draft and Confirmed can both be exported (existing clarification).

## R6: Client

- **`apiDownload(path)`** in `api/http.ts` returns `{ blob, fileName }`. It shares one internal `send` helper with `apiFetch`: the token header and the session-expiry handling are written once. The file name comes from `filename*` or `filename`, falling back to `samata.xlsx`.
- **`EstimateExportButton`**: "Eksportuoti į Excel", then `ConfirmDialog` with the title "Eksportuoti sąmatą?" and the message "Eksportuoti „<name>“ į Excel? Faile bus tie patys duomenys, kurie matomi ekrane." While it runs, `busy`. On success, the browser downloads the file through an object URL. On failure: "Eksportuoti nepavyko. Bandykite dar kartą." (wf15).
- **`ConfirmDialog` gets `confirmTone: 'danger' | 'primary'`**, default `danger`, so existing deletes don't change. Export and the two status changes aren't destructive, so they use `primary`. This closes 008's analysis note U1, now that three non-destructive confirmations exist.
