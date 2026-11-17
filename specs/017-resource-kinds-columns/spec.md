# Feature 17: Resource Kinds and Estimate Columns

**Feature Branch**: `017-resource-kinds-columns`
**Created**: 2026-11-17
**Status**: Draft
**Input**: Gaps B and C in docs/reference/ANALYSIS.md §3 (proposed FR-030–FR-032). A catalog material has a kind: Material or Machinery (default Material). Every work line and the whole estimate show amounts split into Labour / Materials / Machinery, and every work line shows its unit price (amount / quantity). The Excel export uses the columns No., Name, Unit, Norm, Price, Quantity, Amount, Labour, Materials, Machinery (see docs/reference/sample-estimates/sample-estimate-export.csv). Viewers see nothing new (BR-005). The BR-016 calculation does not change.

## Specification
Lithuanian local estimates split every amount into labour, materials and machinery (tools, equipment, transport). Clients and site managers expect this layout: they check the labour share, compare material costs with supplier offers, and see each work's price per unit ("€ per m² of wall"). Today the platform treats everything that is not labour as a material and shows only one amount per line, so an estimate exported from it does not look like the estimates clients are used to. This feature lets the Manager mark a catalog material as Machinery. Each work line and the estimate show the three amounts and a unit price per work line, on screen and in the Excel export. How the total is calculated does not change.

## User Scenarios & Testing

### User Story 1: Mark materials as machinery (Priority: P1)
As a Manager, I want to set a catalog material's kind to Material or Machinery, so that tools, equipment and transport are counted apart from materials.

**Why this priority**: the Labour / Materials / Machinery split (US2) depends on it; without kinds, Machinery is always 0.

**Independent Test**: create a material with kind Machinery, edit another from Material to Machinery; the catalog list shows each material's kind, and a new material without a chosen kind is Material.

**Acceptance Scenarios**:
1. Given the material form, when the Manager creates "Įrankiai ir įrengimai" (vnt., 0,20 €) with kind "Mechanizmas", then it is saved with that kind and the materials list shows "Mechanizmas" for it.
2. Given the material form, when the Manager does not change the kind, then the material is saved as "Medžiaga".
3. Given materials that existed before this feature, then all of them are "Medžiaga".
4. Given a material used in an estimate, when the Manager changes its kind in the catalog, then the estimate does not change (BR-010) until it is refreshed (US3).

### User Story 2: See labour, materials, machinery and unit price per work line (Priority: P1)
As a Manager, I want each work line and the estimate to show the amount split into Labour, Materials and Machinery, and each work line's unit price, so that I can check the estimate and give it to a client in the familiar format.

**Why this priority**: the purpose of the feature (gaps B and C). Together with US1 it is the minimum useful feature.

**Independent Test**: open the worked example estimate (specs/007, total 1263,26 €) with one machinery line added and check every split amount, the unit price and the unchanged totals.

**Acceptance Scenarios**:
1. Given the worked example ("Siena A++", 35 m²; all three materials "Medžiaga"), when the Manager opens the estimate, then the work line shows Labour 420,00 €, Materials 405,31 € (346,50 + 18,38 + 40,43), Machinery 0,00 €, amount 825,31 € and unit price 23,58 €/m² (825,31 ÷ 35), and the total is still 1263,26 €.
2. Given the same work line with a material line "Įrankiai ir įrengimai" (Machinery, norm 1, waste 0 %, 0,20 €) added to the work's composition before the work was added, then that line costs 7,00 €, the work line shows Machinery 7,00 €, amount 832,31 € and unit price 23,78 €/m².
3. Given an estimate with several work lines, then below the lines the estimate shows the total Labour, Materials and Machinery, and the direct costs equal their sum. Overhead, profit, VAT and the total are calculated as before (BR-016).
4. Given an estimate where a work line's quantity changes, then its split amounts, amount and unit price are recalculated together with the totals (FR-015).
5. Given a Viewer opens the estimate, then they see only the names of works and materials, as before: no split amounts, unit prices or kinds (BR-005, FR-026).

### User Story 3: Kinds follow estimate edits and refreshes (Priority: P2)
As a Manager, I want a material line's kind to stay correct when I replace its material or refresh the estimate from the catalog, so that the split amounts stay right.

**Why this priority**: without it, an edited estimate could count machinery as materials. It builds on US1 and US2.

**Independent Test**: replace a material line's material with a Machinery material, and refresh an estimate after changing a material's kind in the catalog; in both cases the line's amount moves to the right column.

**Acceptance Scenarios**:
1. Given a Draft estimate, when the Manager replaces a "Medžiaga" material line with a "Mechanizmas" material (FR-023), then the line takes the new material's kind and its cost moves from Materials to Machinery.
2. Given a catalog material changed from "Medžiaga" to "Mechanizmas" after it was added to a Draft estimate, when the Manager opens the refresh preview (FR-024), then the preview lists the line's kind "Medžiaga → Mechanizmas", and after confirming, the line's cost is counted under Machinery. The total is unchanged if nothing else changed.
3. Given a Confirmed estimate, then its kinds cannot change (BR-013).

### User Story 4: Export the estimate in the familiar column layout (Priority: P2)
As a Manager, I want the Excel export to use the columns No., Name, Unit, Norm, Waste %, Price, Quantity, Amount, Labour, Materials, Machinery, so that the file looks like the estimates my clients know.

**Why this priority**: FR-027 already requires the export to contain what the Manager sees; this gives it the expected layout. The screen (US2) comes first.

**Independent Test**: export the estimate of US2 scenario 2 and compare each cell with the screen and with the layout of sample-estimate-export.csv.

**Acceptance Scenarios**:
1. Given the estimate of US2 scenario 2, when the Manager exports it, then the table has the headings "Nr.", "Pavadinimas", "Mato vnt.", "Norma", "Atliekos %", "Kaina", "Kiekis", "Suma", "Darbas", "Medžiagos", "Mechanizmai", in that order.
2. Then the work line row has No. 1, the name "Siena A++", unit m², its unit price 23,78 € in "Kaina", quantity 35, amount 832,31 € and Labour 420,00 €, Materials 405,31 €, Machinery 7,00 €.
3. Then each material line row has no No., its name, unit, norm, waste %, price, calculated quantity and cost in "Suma", and its cost again in the column of its kind ("Medžiagos" or "Mechanizmai").
4. Then directly under the work line row comes its labour row: no No., name "Darbas", unit "val.", norm 0,8 (labour norm), price 15,00 € (labour rate), quantity 28 (hours = 35 × 0,8), and 420,00 € in "Suma" and in "Darbas" (spec Q1). The material line rows follow it.
5. Then the direct costs row shows the direct costs in "Suma" and the total Labour, Materials and Machinery in their columns; overhead, profit, VAT and the total follow with their amount in "Suma" only.
6. Then the header (project, estimate, status, export date), number cells and protection of text cells (NFR-007) are as before.

### Edge Cases
- Labour comes only from the work line's labour (labour norm × rate); a material can never be of kind Labour.
- A work line with no material lines has Materials and Machinery 0,00 €; one with labour norm 0 has Labour 0,00 €. Zero amounts are shown as 0,00 €.
- The split amounts are sums of already-rounded line costs, so Labour + Materials + Machinery = the work line's amount exactly, and the estimate's three totals add up to the direct costs exactly.
- The unit price is shown only; it is never used to calculate another amount. Unit price × quantity may differ from the amount by a rounding difference, which is expected.
- Estimate quantities are always greater than 0 (BR-007), so the unit price can always be calculated.
- The sample export has a "Code" column; the platform has no resource codes, so this column is not added.
- The waste % column ("Atliekos %") stays, as an extra column right after "Norma", on screen and in the export; it is filled only on material line rows (spec Q2).
- On a narrow screen (360px, NFR-008) the extra amounts are shown in the existing stacked line layout, without horizontal page scrolling.
- Copying an estimate or other features that copy lines keep each material line's kind.
- A Viewer's names view and permissions are unchanged; a Viewer still cannot export (BR-004).

## Requirements

### Functional Requirements
- **FR-017-1**: A catalog material has a kind, "Medžiaga" (Material) or "Mechanizmas" (Machinery), chosen in the material form; the default is "Medžiaga". The materials list shows each material's kind (FR-030).
- **FR-017-2**: An estimate material line copies its material's kind when the work is added (BR-010). Later catalog changes do not change it, except through replacing the material (FR-023) or refreshing the estimate (FR-024). Both take the catalog material's current kind, and the refresh preview lists a changed kind like any other changed value.
- **FR-017-3**: Existing materials and estimate material lines get the kind "Medžiaga"; every existing estimate keeps its amounts and totals.
- **FR-017-4**: Each work line has: Labour = its labour cost; Materials = the sum of its "Medžiaga" material line costs; Machinery = the sum of its "Mechanizmas" material line costs; amount = Labour + Materials + Machinery (BR-015). No additional rounding is applied: each part is a sum of already-rounded costs.
- **FR-017-5**: Each work line has a unit price = its amount ÷ its quantity, rounded to 2 decimal places with the existing money rounding (BR-017, NFR-006), e.g. 23,58 €/m² (spec Q3). It is shown only and never used in another calculation.
- **FR-017-6**: The estimate shows the total Labour, Materials and Machinery (sums of the work lines' parts); the direct costs equal their sum. Overhead, profit, VAT and the total are calculated as before (BR-016, unchanged).
- **FR-017-7**: The Estimate Editor shows, for each work line, its number (1, 2, … in the order of the lines), its Labour, Materials, Machinery, amount and unit price. Directly under it comes a labour row ("Darbas", val., labour norm, labour rate, hours = quantity × labour norm, labour cost), and then the material lines, each with its cost in its kind's column (spec Q1). The labour rate is edited on the labour row (FR-023). Hours are shown with up to 3 decimal places and are not rounded in the calculation. Below the lines it shows the three totals before the direct costs. All are recalculated together with the existing totals (FR-015, NFR-009).
- **FR-017-8**: The Excel export table has the columns "Nr.", "Pavadinimas", "Mato vnt.", "Norma", "Atliekos %", "Kaina", "Kiekis", "Suma", "Darbas", "Medžiagos", "Mechanizmai", as in US4, with the same values as the screen (FR-027). Amounts and unit prices are number cells; text cells stay protected (NFR-007).
- **FR-017-9**: A Viewer receives no kinds, split amounts or unit prices, on screen or in any response (BR-005, FR-026); this is covered by the existing role tests (NFR-005).
- **FR-017-10**: Only a Manager can set a material's kind (BR-004); another company's materials stay inaccessible (FR-006).

### Key Entities
- **Material kind** (new fixed list): Medžiaga (Material), Mechanizmas (Machinery). Not editable by users.
- **Material** (existing): gains a kind.
- **Estimate material line** (existing): gains a copy of its material's kind.
- **Work line calculation / estimate calculation** (calculated, not stored): gain Labour, Materials and Machinery amounts and, for work lines, the unit price.

## Success Criteria

### Measurable Outcomes
- **SC-017-1**: For 100% of estimates, Labour + Materials + Machinery equals each work line's amount and the direct costs exactly, to the cent (automated tests, including the worked example: 420,00 + 405,31 + 0,00 = 825,31 €).
- **SC-017-2**: 100% of existing estimates have the same direct costs and total before and after the change (automated test on the worked example: 1263,26 €).
- **SC-017-3**: The exported file's columns and every work line's Labour, Materials, Machinery, amount and unit price equal the screen for 100% of lines (automated test).
- **SC-017-4**: A client who knows the local estimate format can find a work's unit price and its labour, materials and machinery amounts in the export without explanation (review against sample-estimate-export.csv).
- **SC-017-5**: 0 kinds, split amounts or unit prices reach a Viewer (automated role tests); all existing tests keep passing.

## Assumptions
- docs/requirements.md gets FR-030–FR-032 and the BR-010, BR-015, BR-017 and NFR-006 wording from the Step 1 proposal before this feature is planned. That proposal is not approved yet.
- Two kinds are enough. Labour is always the work's own labour (one labour rate and norm per work); more labour resources per work stay out of scope (ANALYSIS.md §3).
- Overhead, profit and VAT stay single percentages on the direct costs. The sample's three separate 1,5 % surcharges per column give the same total, so no per-column surcharges are added.
- Sections, section subtotals and estimate header fields (gaps D, E) are separate features; the export has one table for the whole estimate.
- Filtering or searching materials by kind is not required (FR-029 categories are a separate feature).
- Feature 16 (precision) is independent; if it lands first, prices in the "Kaina" column follow its display rule.

## Clarifications

### Session 2026-11-17
- Q: Once a work line row shows the line's total amount and unit price, where should that work's labour (labour norm, labour rate, hours and labour cost) appear on screen and in the Excel export? → A: As a labour row directly under each work line ("Darbas", val., labour norm, labour rate, hours, cost in "Suma" and "Darbas"), on screen and in the export.
- Q: The current screen and export have a waste % column ("Atliekos %"), but the requested export column list does not include it; what should happen to it? → A: Keep it as an extra column "Atliekos %" right after "Norma", on screen and in the export (11 columns).
- Q: When a work line's unit price (amount ÷ quantity) is shown, how many decimal places should it be rounded to? → A: 2 decimal places with the existing money rounding; shown only, never used in another calculation.
