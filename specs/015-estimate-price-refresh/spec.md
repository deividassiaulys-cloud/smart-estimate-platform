# Feature 15: Estimate Price Refresh

**Feature Branch**: `015-estimate-price-refresh`
**Created**: 2026-11-09
**Status**: Draft
**Input**: FR-024 (Should Have) in docs/requirements.md. While an estimate is a Draft, a Manager updates its prices and norms from the current catalog, after first seeing which lines will change.

## Specification
An estimate keeps its own copy of prices and norms from the moment each work was added (BR-010), so catalog price changes never reach it by accident. When prices have moved (a supplier raised prices, labour rates went up), the Manager wants an older Draft estimate brought up to date without rebuilding it. The Manager first sees exactly what would change (old value → new value, and the new total), then confirms. Only this estimate changes.

## User Scenarios & Testing

### User Story 1: See what a refresh would change (Priority: P1)
As a Manager, I want to see which lines of a Draft estimate differ from the current catalog and how, so that I can decide whether to update it.

**Why this priority**: FR-024 requires showing the changes beforehand; without the preview the update would silently overwrite prices, including ones changed on purpose in this estimate (FR-023).

**Independent Test**: change a catalog material's price, open the refresh preview of an estimate using it; the preview lists that line with old and new price and the new total, and the estimate itself is unchanged.

**Acceptance Scenarios**:
1. Given the worked example estimate (total 1263,26 €) and the catalog now has "Siena A++" at 16,00 €/h and "Gipso plokštė" at 5,00 €, when the Manager opens the refresh preview, then it lists the work line's labour rate 15,00 → 16,00 €/h and the material line's price 4,50 → 5,00 €, and shows the total 1263,26 € → 1365,05 €; nothing is saved yet.
2. Given an estimate whose values all equal the catalog, when the Manager asks for the preview, then a message says there is nothing to update and no confirmation is offered.
3. Given a line whose work or material is no longer in the catalog (deleted or renamed), then the preview lists it as "not found in the catalog — stays unchanged".
4. Given the preview, when the Manager cancels, then the estimate is unchanged.

### User Story 2: Update the estimate from the catalog (Priority: P1)
As a Manager, I want to confirm the previewed update, so that the estimate uses the current catalog prices and norms.

**Why this priority**: the purpose of FR-024; together with US1 it is the minimum useful feature.

**Independent Test**: confirm the preview; the estimate shows the new values and total, and the catalog and other estimates are unchanged.

**Acceptance Scenarios**:
1. Given the preview of scenario US1-1, when the Manager confirms, then the work line's labour rate is 16,00 €/h (labour cost 448,00 €), the material line's price is 5,00 € (cost 385,00 €), and the totals are 891,81 € direct, 89,18 € overhead, 147,15 € profit, 236,91 € VAT, 1365,05 € total.
2. Given the update, then quantities, the overhead, profit and VAT percentages, and the lines not found in the catalog are unchanged.
3. Given a changed consumption norm or waste % in the work's composition in the catalog, then the update applies it to the matching material line and its quantity and cost are recalculated.
4. Given a Confirmed estimate, then the refresh is not offered and the system refuses it.

### Edge Cases
- A line is matched to the catalog by name and unit, the way the catalog identifies them (BR-006). A work line is "not found" when its work was renamed or deleted; a material line is "not found" when its material was. Both then stay as they are. If only a material line's work is missing, the material line still takes its catalog price but keeps its norm and waste %.
- If an estimate contains the same work twice, the preview lists each line's changes under the same work name; both lines get the same catalog values, so no line number is shown.
- A price changed on purpose in this estimate (FR-023) is overwritten by the update if it differs from the catalog; the preview shows it like any other change, so the Manager can cancel.
- A material that replaced another in this estimate (FR-023) takes its price from the catalog material; its norm and waste % come from the work's composition only if that material is part of it, otherwise they stay.
- Materials added to or removed from a work's composition in the catalog since the work was added do not add or remove lines in the estimate; only existing lines are updated (spec Q1). To take a work's new composition, the Manager removes and re-adds the work line.
- If the catalog changes between the preview and the confirmation, the update applies the catalog as it is when confirmed, and the response shows the result.
- If the estimate was confirmed in another tab meanwhile, the update is refused with the existing "status changed" message.
- A Viewer cannot see the preview or update (FR-007); another company's estimate is "not found" (FR-006).
- Amounts are recalculated with the existing rules (BR-015–BR-017); the Viewer's names view and the Excel export show the updated estimate.

## Requirements

### Functional Requirements
- **FR-015-1**: A Manager can request a preview of a Draft estimate's refresh: for each line whose values differ from the current catalog, the old and new labour rate and labour norm (work lines) or price, consumption norm and waste % (material lines); the lines not found in the catalog; and the current and resulting total. The preview changes nothing.
- **FR-015-2**: A Manager can confirm the refresh, which updates the Draft estimate's lines from the current catalog as in FR-015-1 and recalculates it.
- **FR-015-3**: Lines are matched by name and unit: a work line to the catalog work, a material line's price to the catalog material, and a material line's norm and waste % to the matching material in the catalog work's composition.
- **FR-015-4**: Quantities, percentages, the set of lines and the lines not found in the catalog are never changed by the refresh.
- **FR-015-5**: The refresh changes only this estimate, never the catalog or other estimates (BR-010).
- **FR-015-6**: The preview and the refresh are refused for Confirmed estimates (BR-013), for Viewers (BR-004) and for another company's estimate (FR-006), with the same responses as the other estimate edits.
- **FR-015-7**: The confirmation is an explicit second step after the preview (it uses the shared confirmation dialog, like the NFR-011 confirmations, since it overwrites values). The preview is read-only: confirming applies every listed change (spec Q2).
- **FR-015-8**: In the Estimate Editor (Draft only), an "Atnaujinti kainas iš katalogo" action opens the shared confirmation dialog listing the changes (line: field old → new), the lines not found in the catalog and the total old → new, with "Atnaujinti" and "Atšaukti"; with nothing to update, no dialog opens and a message next to the action says the estimate already matches the catalog (spec Q3).

### Key Entities
- **Refresh preview** (not stored): the list of changes (line, field, old value, new value), the lines not found in the catalog, and the current and resulting total.
- **Work line / material line** (existing): their copied prices and norms are overwritten by the refresh. No new stored entity.

## Success Criteria

### Measurable Outcomes
- **SC-015-1**: A Manager brings a Draft estimate up to date with the catalog in under 1 minute, seeing every change before it is applied.
- **SC-015-2**: The preview's resulting total equals the total after confirming, to the cent, when the catalog did not change in between (automated test).
- **SC-015-3**: After a refresh, every matched line's prices and norms equal the catalog's, and every other value is unchanged (automated tests on the worked example).
- **SC-015-4**: 0 catalog records or other estimates change (automated test).
- **SC-015-5**: 100% of previews and refreshes are refused for Confirmed estimates, Viewers and other companies (automated tests); all existing tests keep passing.

## Assumptions
- Name-and-unit matching is enough, because the catalog cannot hold two works or two materials with the same name and unit (BR-006); names are compared as stored (the catalog's comparison rules apply).
- If a work's composition contains the same material more than once, its lines are matched to the estimate's lines of that material in their order.
- The whole estimate is refreshed at once; individual changes cannot be picked (spec Q2). A price the Manager wants to keep can be set again afterwards (FR-023).
- FR-025 (copying estimates) is a separate feature.

## Clarifications

### Session 2026-11-09
- Q: If a work's material list in the catalog has changed since the work was added, should the refresh also add or remove material lines in the estimate? → A: No. Only existing lines are updated (prices, norms, waste %); no line is added or removed.
- Q: In the preview, can the Manager choose which changes to apply, or does confirming always apply every listed change? → A: Confirming applies every listed change; the preview is read-only.
- Q: How should the preview be shown in the Estimate Editor? → A: An "Atnaujinti kainas iš katalogo" button opens the shared confirmation dialog listing the changes, the lines not found and the total old → new.
