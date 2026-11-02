# Data Model: Code Structure Refactor (Article X)

The database schema does not change (no migration). Only how values are set and how data crosses the API boundary changes.

## Entities (Models/Entities)

| Entity | Construction | Mutators (no business rules) | Notes |
|---|---|---|---|
| Company | `Company(name)` + private EF constructor | none | `Users` collection: private setter |
| User | `User(firstName, lastName, email, role, companyId)` and `User(firstName, lastName, email, role, company)` for registration + private EF constructor | `SetPasswordHash(hash)`, `SetLoginState(failedCount, firstFailedAt, lockoutEnd)`, `ResetLoginFailures()`, `InvalidateTokens()` | Lockout calculation stays in `AuthService` |
| Material | `Material(name, unit, price)` + private EF constructor | `Update(name, unit, price)` | `NameMaxLength` moves to `MaterialRules` |
| ICompanyScoped | `int CompanyId { get; }` | none | `AppDbContext` assigns it through the change tracker |

`UserRole` and `MeasurementUnit` stay in Models/Entities (owner decision; fixed lists live there per AGENTS.md).

## DTOs (sealed records, init properties)

| Folder | Record | Fields | Validation (X.10) |
|---|---|---|---|
| DTOs/Auth | RegisterRequest | CompanyName, FirstName, LastName, Email, Password | `[Required]`, `[EmailAddress]`; `Validate` → `PasswordRules` with Email |
| DTOs/Auth | LoginRequest | Email, Password | `[Required]` |
| DTOs/Auth | AuthResponse | Token, UserId, CompanyId, Role | — (replaces Register/LoginResponse) |
| DTOs/Auth | CurrentUserResponse | FirstName, LastName, CompanyName, Role | — |
| DTOs/Team | CreateViewerRequest | FirstName, LastName, Email, Password | `[Required]`, `[EmailAddress]`; `Validate` → `PasswordRules` with Email |
| DTOs/Team | ResetPasswordRequest | NewPassword | `[Required]`; `Validate` → `PasswordRules` (length; the email check is in the service) |
| DTOs/Team | TeamMemberResponse | Id, FirstName, LastName, Email, Role | — |
| DTOs/Materials | MaterialRequest | Name, Unit, Price | `[Required]`, `[StringLength(MaterialRules.NameMaxLength)]`; `Validate` → `MaterialRules` (unit, price) |
| DTOs/Materials | MaterialResponse | Id, Name, Unit, Price | — |

Field names, types and JSON names are unchanged. Mapping entity → DTO happens only in `Services/Mapping/DtoMapper.cs`.

## Options

`JwtOptions`: sealed record with init properties (Key, Issuer, Audience, ExpiresMinutes = 60), bound from section `Jwt` with the existing start-up validation.
