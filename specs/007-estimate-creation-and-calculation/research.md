# Research: Estimate Creation and Calculation

**Decision:** Implement the calculation logic as a plain, dependency-free service that uses C# `decimal` and one `MoneyRounding` helper (`decimal.Round(value, 2, MidpointRounding.AwayFromZero)`).
**Rationale:** The formulas are fixed and simple, so a rules engine would add complexity without benefit. BR-017 requires 0.005 to become 0.01, while .NET's default banker's rounding would give 0.00.
**Alternatives considered:** `double` arithmetic and default rounding (rejected: inexact money); a third-party pricing/rules library (rejected as unjustified for this scope).
