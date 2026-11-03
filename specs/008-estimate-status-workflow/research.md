# Research: Estimate Status Workflow

## R1: What 007 already provides

- **Decision**: build on 007; don't redo it.

  | Already in 007 | Where |
  |---|---|
  | `EstimateStatus { Draft, Confirmed }` and `Estimates.Status`, Draft on creation (BR-012) | `AddEstimate` migration |
  | Every change passes the Draft-only check (`FindDraftAsync` → 409 `EstimateNotDraftException`) | `EstimateService` |
  | `EstimateSummaryResponse.Status`, shown as text in the Project Detail list | US3 |

  The old tasks T089 (Status field), T090 (migration), T091 (lock enforcement), T093 (status in the list) and T097 (status in the UI) are therefore already done or obsolete. Their remaining parts are tests.

## R2: Transitions

- **Decision**:
  - `Estimate.Confirm()` and `Estimate.ReturnToDraft()` only set the status (X.6).
  - `EstimateService.ConfirmAsync(id)` and `ReturnToDraftAsync(id)` share one private `ChangeStatusAsync(id, from, to, message)`. It loads the estimate (404 if missing), throws `EstimateStatusException(message)` unless the current status is `from`, sets `to`, saves, and returns the whole estimate.
  - The messages are "The estimate is already confirmed." and "The estimate is already a draft." (spec Q1).
- **Rationale**: one place for the transition rule. Confirming doesn't depend on the number of lines (Q2).
- **Alternatives considered**: a generic "set status" endpoint taking the target status. Rejected: two explicit actions match FR-019 and FR-021 and can't be misused to set an unknown status.

## R3: HTTP

- **Decision**:
  - `PATCH /api/estimates/{id}/confirm` and `PATCH /api/estimates/{id}/draft`, with no body (as in the earlier tasks T094/T095).
  - Both are Manager only and return 200 with the `EstimateResponse`.
  - Errors: 404 for a missing or another company's estimate; 409 `EstimateStatusException`.

## R4: Edits on a Confirmed estimate

- **Decision**: rewrite 007's `EstimateDraftGuardTests` to confirm through the real endpoint instead of setting the status in the database. Then add a work, change a quantity or the percentages, and remove a line: each gives 409. After return to Draft, the same edits succeed (US2's check).
- **Rationale**: the test now goes through the real way an estimate gets confirmed ("by any means", spec).

## R5: Client

- **Decision**:
  - `EstimateStatusActions` shows the status badge ("Juodraštis" / "Patvirtinta", text, not colour only), plus either "Patvirtinti" or "Grąžinti į juodraštį". Each opens `ConfirmDialog` (NFR-011).
  - The editor passes `readOnly = status === Confirmed`:
    - `AddWorkForm` isn't rendered
    - `EstimateWorkLineRow` disables its quantity input and hides "Pašalinti"
    - `EstimatePercentagesForm` disables its inputs
- **On 409** (a stale page): show "Sąmatos būsena jau pasikeitė." and reload the estimate (Q1).
- **Rationale**: one editor, no duplicated table or totals (Q3). The server still enforces everything; the UI only hides what would be rejected.
