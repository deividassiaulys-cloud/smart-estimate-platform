# Data Model: Estimate Status Workflow

**No schema change and no migration.** `Estimates.Status` (`EstimateStatus`: Draft = 0, Confirmed = 1) exists since 007's `AddEstimate`.

## Entity changes

| Entity | Change |
|---|---|
| Estimate | + `Confirm()` sets `Status = Confirmed`; + `ReturnToDraft()` sets `Status = Draft`. Both are plain mutators; the transition rule lives in `EstimateService` (X.6, Article II) |

## State transitions

```text
          create (BR-012)
               │
               ▼
          ┌─────────┐   PATCH /confirm (Manager, after confirmation)   ┌───────────┐
          │  Draft  │ ───────────────────────────────────────────────▶ │ Confirmed │
          │(editable)│ ◀─────────────────────────────────────────────── │(read-only)│
          └─────────┘   PATCH /draft (Manager, after confirmation)     └───────────┘

Confirm on Confirmed → 409 "The estimate is already confirmed."
Draft on Draft       → 409 "The estimate is already a draft."
Any edit on Confirmed → 409 "The estimate is confirmed and cannot be changed." (since 007)
```

## Errors

| Exception | Status | Detail |
|---|---|---|
| EstimateStatusException (new) | 409 | "The estimate is already confirmed." / "The estimate is already a draft." |
| EstimateNotDraftException (007) | 409 | "The estimate is confirmed and cannot be changed." |
