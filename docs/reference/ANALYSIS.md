# Gap analysis: Smart Estimate Platform vs. real-world sample estimates

Reviewed: `main` @ bed5271 (docs/requirements.md, constitution 1.2.0, specs 001–015, entities, `EstimateCalculator`, `EstimateExportService`).
Reference data: `docs/reference/sample-estimates/`.

## 1. What already matches

**The calculation needs no change.** BR-016 (overhead → profit → VAT, applied in sequence) with overhead 1.5 %, profit 6 % and VAT 21 % gives exactly the sample estimate's grand total:

| | Sample estimate (PDF) | Platform BR-016 |
|---|---|---|
| Direct costs | 104,017.69 | 104,017.69 |
| Surcharges | 893.18 + 80.13 + 586.96 = 1,560.27 | 104,017.69 × 1.5 % = 1,560.27 |
| Profit 6 % | 6,334.68 | 105,577.96 × 6 % = 6,334.68 |
| VAT 21 % | 23,501.65 | 111,912.64 × 21 % = 23,501.65 |
| **Grand total** | **135,414.29** | **135,414.29** |

Other points that already match:
- Copying prices and norms into the estimate (BR-010) matches the source estimates.
- A catalog work (labour norm × labour rate + material composition, FR-009) is the same concept as a sample-estimate line with resources, limited to one labour resource. The sample estimates already split factory labour (25 EUR/h) and site labour (35 EUR/h) into separate lines, so the one-labour-line model is enough.
- Waste %: the sample norms already include waste (e.g. membrane 1.3 m² per m²), so waste is 0 in the trial data.

## 2. Trial data without code changes

`docs/reference/seed/` loads 200 materials and 102 works through the existing API. With them the sample estimate totals **135,464.33 EUR** (PDF 135,414.29, +0.04 %). The compromises that cause this difference are listed in `seed/README.md`.

## 3. Gaps worth closing (priority order)

| # | Gap | Value | Requirement impact |
|---|---|---|---|
| A | **Precision:** material price and labour rate DECIMAL(18,2) → (18,4); consumption and labour norms DECIMAL(18,3) → (18,6); display norms with 3 decimals | Cent-accurate estimates; no "pack" workarounds | shared-technical-rules.md, NFR-006 / BR-017 wording, FR-008/FR-009 validation. Small |
| B | **Resource type** (Material.Kind: Material / Machinery) and **Labour / Materials / Machinery** columns per work line, per estimate and in the Excel export | The defining layout of LT local estimates | New FR (Should); extends FR-027 |
| C | **Unit price per work line** (amount / quantity) on screen and in the export; export columns as in the sample | Familiar format for clients | FR-027 (export = screen). Small |
| D | **Sections**: group work lines (e.g. "Lauko siena 28+9,5+195+45 mm") with section subtotals | Largest structural difference | **"sections" is currently Out of Scope** – needs instructor approval |
| E | Estimate header: number, price level ("2026 m. 10 mėn. kainomis"), **Notes**, **Not included** | Complete offer document | New FR (Could) |
| F | Extra units (pak., reisas, km, d.d.) | Cleaner names | BR-008 |
| G | Price history and price dates | – | **Out of Scope**, keep it that way |

**Do not change:**
- Multiple labour resources per work. The sample estimates almost never need it, because factory and site labour are already separate lines.
- The overhead logic (see section 1).

## 4. Process (AGENTS.md / constitution)

1. Update `docs/requirements.md` (new FRs, scope changes). For D, get instructor approval first and record it in `docs/review/change-log.md`.
2. Run each gap as its own feature branch and Spec Kit cycle: specify → clarify → plan → tasks → analyze → implement.
3. Suggested numbering: 016 precision (A + F), 017 resource types and estimate layout (B + C), 018 sections (D, only if approved), 019 estimate header (E).

## 5. Prompts for Claude Code

**Step 0 – review only, no changes:**
```
Read AGENTS.md, .specify/memory/constitution.md, docs/requirements.md and
docs/reference/ANALYSIS.md. Verify every claim in section 3 against the current code
(EstimateCalculator, Material, Work, EstimateExportService, AppDbContext precision,
MeasurementUnit, NumberRules). List anything you disagree with and, for each gap A–F,
which FR/NFR/BR would change. Do not modify any files.
```

**Step 1 – requirement changes (before any feature):**
```
Propose changes to docs/requirements.md and specs/shared-technical-rules.md for gaps
A, B and C in docs/reference/ANALYSIS.md section 3: new FR numbers with priorities,
updated wording of BR-008, BR-017 and NFR-006, and an entry for
docs/review/change-log.md. Do not change the Out of Scope list. Show the diff and
wait for my approval before writing. Do not commit.
```

**Step 2 – feature 016 (precision):**
```
/speckit.specify Feature 16: Higher price and norm precision. Material price and
labour rate are stored as DECIMAL(18,4); consumption norm and labour norm as
DECIMAL(18,6), in the catalog and in estimate lines. Validation accepts up to 4
and 6 decimals respectively and rejects more. The UI shows norms with up to 3
decimals and prices with up to 4. Money amounts are still rounded only by
MoneyRounding to 2 decimals (BR-017). Existing estimates keep their values.
Acceptance test: the lines of docs/reference/sample-estimates/sample-estimate.json,
created as catalog works with waste 0 and added to one estimate with overhead
1.5 %, profit 6 %, VAT 21 %, total 135,414.29 EUR ± 0.50 EUR.
```

**Step 3 – feature 017 (resource types and estimate layout):**
```
/speckit.specify Feature 17: Resource types and estimate columns. A catalog
material has a kind: Material or Machinery (default Material). Every work line
and the whole estimate show amounts split into Labour / Materials / Machinery,
and every work line shows its unit price (amount / quantity). The Excel export
uses the columns No., Name, Unit, Norm, Price, Quantity, Amount, Labour,
Materials, Machinery (see docs/reference/sample-estimates/sample-estimate-export.csv).
Viewers see nothing new (BR-005). The BR-016 calculation does not change.
```

Run `/speckit.clarify` after every `/speckit.specify`. Then go through plan → tasks → analyze, and only then implement. After implementing, run `dotnet test` and `npm run test`.
