# Trial catalog loader (no code changes)

`materials.json` (200) and `works.json` (102) are the sample-estimate rate templates converted to the
current API (`MaterialRequest`, `WorkRequest`). With these works the sample estimate totals
135,464.33 EUR under the current BR-015 to BR-017 (PDF: 135,414.29 EUR, +0.04 %). See `conversion-report.txt`.

## Use

1. Start the API (`dotnet run` in `server/SmartEstimatePlatform.Api`) and the client.
2. Register a test company in the UI (e.g. `manager@test.lt`).
3. `python docs/reference/seed/seed.py --email manager@test.lt --password <password>`
   (default URL `https://localhost:7264`; add `--url http://localhost:5084` if needed).
4. Create a project and an estimate, set overhead 1.5 %, profit 6 %, VAT 21 %, add works.

Re-running is safe: existing items (409) are skipped. To regenerate: `python convert.py ../sample-estimates`.

## Compromises forced by the current rules

- Price is DECIMAL(18,2): sub-cent prices (0.001 EUR clips, 0.0616 EUR screws) are converted to packs, e.g. "Kabė (pack 10 vnt)".
- Norms have 3 decimals: small timber norms (m³ per m²) are rounded, which causes most of the +50 EUR; two norms below 0.0005 are dropped.
- Units missing from BR-008 (`pak., reisas, km, d.d., tub.`) are moved into the name.
- Machinery has no type of its own: loaded as materials marked `[mech.]`.
- BR-006 (name + unit unique): repeated names such as "Montavimo darbai" get the section name as a suffix.
- Waste % is 0 everywhere: the source norms already include waste.
