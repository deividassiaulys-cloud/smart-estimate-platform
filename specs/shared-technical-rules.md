# Shared technical rules

These rules apply to all features (see the Technical Plan in Independent Work 3, Step 11).

- **Money and quantities.** All monetary values use C# decimal and are stored as DECIMAL(18,2); quantities and norms use DECIMAL(18,3); percentages use DECIMAL(5,2). double and float are never used for money (NFR-006).
- **Rounding.** One helper (MoneyRounding) rounds money with decimal.Round(value, 2, MidpointRounding.AwayFromZero), so 0.005 becomes 0.01 (BR-017). .NET’s default rounding is banker’s rounding and is not used. Each line cost, overhead, profit and VAT is rounded; the grand total is the sum of the rounded parts; quantities and norms are not rounded in calculations.
- **Percentages.** Overhead, profit, VAT and waste percentages are validated on the server to be between 0 and 100 inclusive (BR-007), independently of the frontend, and each has its own automated test.
- **Fixed measurement units.** The list (vnt., m, m², m³, kg, t, l, val., kompl.) is defined once in server/SmartEstimatePlatform.Api/Models/Entities/MeasurementUnit.cs (BR-008). Services validate against it and the frontend loads it from GET /api/units instead of duplicating it.
- **Destructive actions.** Every delete or remove action (material, work, project, work line, Viewer) and the estimate confirmation use the shared ConfirmDialog component (NFR-011).
- **Authentication model.** JWT Bearer authentication is the model chosen for this project; the user behind a token is checked on every request.
