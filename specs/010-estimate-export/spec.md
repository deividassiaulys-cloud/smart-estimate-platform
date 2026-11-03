# Feature 10: Estimate Export

## Specification
As a Manager, I export an estimate to Excel matching what I see on screen. Viewer export attempts are rejected. Edge case: values that look like formulas are exported as plain text.

## User Stories
**US1 (P1):** As a Manager, I want to export an estimate to Excel, so that I can send it to a client or keep an offline copy.
**Independent verification:** The exported file’s data matches the on-screen estimate.

## Acceptance Scenarios
Given a Manager on an estimate, when they confirm the export action, then a file downloads matching the on-screen data. Negative case: when a Viewer attempts to export an estimate, the request is rejected.

## Edge Cases
- Values that look like formulas are exported as plain text.
- The file has one sheet laid out like the Estimate Editor: a header with the project name, the estimate name, its status ("Juodraštis" or "Patvirtinta") and the export date; then, for each work line, a row with its name, unit, quantity, labour norm, labour rate and labour cost, followed by its material rows (name, unit, quantity, consumption norm, waste %, price, cost); then the totals: direct costs, overhead, profit and VAT each with its percentage, and the total with VAT.
- Amounts, quantities, norms and percentages are written as number cells holding exactly the values the server calculated, formatted like the screen (money with 2 decimals and €, quantities and norms with up to 3 decimals, percentages with %); the file contains no formulas, so Excel never recalculates them. Names, units and the status are text cells, and a text value starting with =, +, - or @ is explicitly stored as text (NFR-007).
- The export is confirmed in a dialog titled "Eksportuoti sąmatą?" with the message "Eksportuoti „<sąmata>“ į Excel? Faile bus tie patys duomenys, kurie matomi ekrane." (the Lithuanian form of wireframe wf15, NFR-012); while it runs the button is disabled, and a failure shows "Eksportuoti nepavyko. Bandykite dar kartą."

## Clarifications
- Can a Draft (not yet Confirmed) estimate be exported? Yes — export is available regardless of status, reflecting current data.

### Session 2026-11-03
- Q: What goes into the Excel file, and in what order? → A: One sheet like the editor: header (project, estimate, status, export date), each work line followed by its material lines, then the totals block with percentages and the total with VAT.
- Q: Numbers, text or formulas in the cells? → A: Number cells with formatting holding the server's exact values, no formulas; text cells for names, units and status, with values starting with =, +, - or @ stored explicitly as text (NFR-007).
