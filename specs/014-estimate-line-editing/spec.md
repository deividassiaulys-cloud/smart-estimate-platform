# Feature 14: Estimate Line Editing

**Feature Branch**: `014-estimate-line-editing`
**Created**: 2026-11-05
**Status**: Draft
**Input**: FR-023 (Should Have) in docs/requirements.md. While an estimate is a Draft, a Manager adjusts it without touching the company catalog: change a work line's labour rate or a material line's price, remove a material line, or replace a material line's material with another catalog material.

## Specification
An estimate copies its prices and norms from the catalog when a work is added (BR-010). Real offers often need a different price for one job (a supplier discount, a harder site) or a different material (the client wants another brand). The Manager can make these changes in that one estimate. The catalog and every other estimate stay unchanged, and the totals update straight away.

## User Scenarios & Testing

### User Story 1: Change a price in one estimate (Priority: P1)
As a Manager, I want to change a work line's labour rate or a material line's price in a Draft estimate, so that the offer reflects this job's real prices without changing my catalog.

**Why this priority**: prices are what differ most often between jobs; without it the Manager must change the catalog (which affects future estimates) or export and edit by hand.

**Independent Test**: change a material line's price in a Draft estimate; the line cost and all totals update, and the catalog material still has its old price.

**Acceptance Scenarios**:
1. Given a Draft estimate with the work line "Siena A++" (35 m², labour 0,8 h/m² × 15,00 €/h), when the Manager changes its labour rate to 18,00 €/h, then the labour cost becomes 504,00 € and the direct costs, overhead, profit, VAT and total are recalculated.
2. Given the material line "Gipso plokštė" at 4,50 €, when the Manager changes its price to 4,00 €, then the line cost is 77 × 4,00 = 308,00 € and the totals are recalculated; the catalog material "Gipso plokštė" still costs 4,50 €.
3. Given a negative price, or one with more than 2 decimals, when the Manager saves it, then the change is rejected with a Lithuanian message and nothing changes.
4. Given a Confirmed estimate, then the prices cannot be changed (the controls are not offered, and the system refuses such a change).

### User Story 2: Remove a material line (Priority: P1)
As a Manager, I want to remove a material line from a work line in a Draft estimate, so that materials the client supplies or this job does not need are not charged.

**Why this priority**: as common as price changes; today the only option is removing the whole work line.

**Independent Test**: remove one material line after confirming; it disappears, its cost leaves the totals, and the work line and its other materials stay.

**Acceptance Scenarios**:
1. Given a work line with three material lines, when the Manager chooses to remove "Plėvelė" and confirms, then that line disappears, the totals drop by its cost (with overhead, profit and VAT recalculated), and the other two lines stay.
2. Given the confirmation dialog, when the Manager cancels, then nothing changes.
3. Given a work line's last material line, when it is removed, then the work line stays with labour cost only.
4. Given a Confirmed estimate, then material lines cannot be removed.

### User Story 3: Replace a line's material (Priority: P2)
As a Manager, I want to replace a material line's material with another material from my catalog in a Draft estimate, so that I can offer an alternative without rebuilding the work line.

**Why this priority**: less frequent than price changes and removal; it can be worked around by removing and re-adding the whole work line.

**Independent Test**: replace "Gipso plokštė" with another catalog material; the line shows the new material's name, unit and current catalog price, keeps its consumption norm and waste %, and the totals update.

**Acceptance Scenarios**:
1. Given the material line "Gipso plokštė" (norm 2, waste 10 %, quantity 77) and the catalog material "Drėgmei atspari plokštė" at 6,20 €/m², when the Manager replaces it, then the line shows "Drėgmei atspari plokštė", m², 6,20 €, the same norm, waste % and quantity, cost 477,40 €, and the totals are recalculated.
2. Given the replacement, then the catalog and the work's composition in the catalog are unchanged, and later catalog changes do not affect the line (BR-010).
3. Given a material from another company, one that no longer exists, or one with a different unit, then the replacement is refused and nothing changes; the choice list offers only same-unit materials.
4. Given a Confirmed estimate, then material lines cannot be replaced.

### Edge Cases
- A change to an estimate, line or material of another company is answered as "not found", exactly like other estimate actions (FR-006).
- A Viewer cannot change, remove or replace anything (FR-007); the Viewer's view keeps showing only names, and shows the edited lines (removed lines are gone, replaced lines show the new name).
- The Excel export shows the edited prices and lines, matching the screen (FR-027).
- Changing a price to the same value is accepted and changes nothing.
- A material line keeps only its own copy: changing a price in one estimate never changes another estimate that uses the same work or material.
- Quantity and amounts follow the existing rules: quantities are not rounded; money is rounded to 2 decimals only at line and total level (BR-015–BR-017).
- If the estimate was confirmed in another browser tab meanwhile, the change is refused with the existing "confirmed estimate cannot be changed" message.

## Requirements

### Functional Requirements
- **FR-014-1**: A Manager can change the labour rate (€/h) of a work line in a Draft estimate; the value must be ≥ 0 with at most 2 decimals (BR-007).
- **FR-014-2**: A Manager can change the price (€ per unit) of a material line in a Draft estimate; the value must be ≥ 0 with at most 2 decimals.
- **FR-014-2a**: The labour norm, consumption norms and waste % of estimate lines cannot be changed in the estimate; they stay as copied from the catalog.
- **FR-014-2b**: Prices are edited in a field within the line itself, the same way as quantity: the value is saved on leaving the field or pressing Enter, and an invalid value shows a Lithuanian message under the field while the previous value stays saved.
- **FR-014-3**: A Manager can remove a material line from a work line in a Draft estimate, after explicit confirmation (NFR-011); the work line and its other material lines stay.
- **FR-014-4**: A Manager can replace a material line's material with another material of the same company's catalog **that has the same unit of measurement** in a Draft estimate (only such materials are offered; any other is refused); the line takes the new material's name, unit and current price and keeps its consumption norm and waste %.
- **FR-014-5**: None of these changes alter the company catalog or any other estimate (BR-010).
- **FR-014-6**: After each change, line costs and totals are recalculated with the existing rules (BR-015–BR-017) and shown immediately.
- **FR-014-7**: These changes are refused for Confirmed estimates (BR-013), for Viewers (BR-004) and for another company's data (FR-006), with the same responses as the other estimate edits.
- **FR-014-8**: The Viewer's names view and the Excel export reflect the edited lines.

### Key Entities
- **Work line** (existing): its copied labour rate becomes editable.
- **Material line** (existing): its copied price becomes editable; it can be removed; its material name, unit and price can be replaced by another catalog material's. No new entity.

## Success Criteria

### Measurable Outcomes
- **SC-014-1**: A Manager changes a price, removes a material line or replaces a material in under 30 seconds, without leaving the Estimate Editor.
- **SC-014-2**: After any of these changes, the shown totals equal a manual calculation by BR-015–BR-017 to the cent (automated tests on the worked example).
- **SC-014-3**: 0 catalog records or other estimates change as a result (automated tests).
- **SC-014-4**: 100% of these changes are refused for Confirmed estimates, Viewers and other companies (automated tests).
- **SC-014-5**: All existing automated tests keep passing.

## Assumptions
- Only the labour rate and material price are editable here; quantity editing already exists (FR-017). Norms and waste % stay as copied from the catalog (spec Q2).
- A material in another unit cannot replace a line (the kept norm would lose its meaning); the Manager removes the line instead.
- A material line cannot be added to a work line (FR-023 does not require it); removing and re-adding the work line restores its catalog composition.
- Price limits match the catalog's (≥ 0, 2 decimals); an upper limit, if any, is the catalog's.
- FR-024 (refreshing prices from the catalog) and FR-025 (copying estimates) are separate features.

## Clarifications

### Session 2026-11-05
- Q: When a Manager replaces a material line's material, does the new material have to use the same unit of measurement as the old one? → A: Yes. Only same-unit catalog materials are offered, and any other is refused, so the kept norm stays correct.
- Q: Besides the labour rate and the material price, should the Manager also be able to change norms or waste % within one estimate? → A: No. Only the two prices are editable; norms and waste % stay as copied (FR-023 scope).
- Q: How should the Manager change a price in the Estimate Editor? → A: In a field within the line, the same way as quantity: saved on leaving the field or on Enter.
