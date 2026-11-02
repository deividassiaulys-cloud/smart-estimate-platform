# Data Model: Project Management

## Entity

| Entity | Fields | Constraints |
|---|---|---|
| **Project** (`ICompanyScoped`) | Id, Name, Description, CompanyId, Company | Name: required, trimmed, varchar(200). Description: nullable, trimmed, varchar(2000). No unique index (names may repeat). FK Company |

Construction (X.6, private setters): `Project(name, description)` and `Update(name, description)`, plus a private parameterless constructor for EF Core. Mutators hold no rules.

Feature 007 adds Estimate → Project (cascade). Nothing in 006 references estimates.

## DTOs (`Models/DTOs/Projects/`, sealed records, init properties)

| Record | Fields | Validation (X.10) |
|---|---|---|
| ProjectRequest | Name, Description? | `[Required]` + `[StringLength(ProjectRules.NameMaxLength)]` Name; `[StringLength(ProjectRules.DescriptionMaxLength)]` Description |
| ProjectResponse | Id, Name, Description? | — (used for both the list and the detail) |

## Rules

`ProjectRules`: `NameMaxLength = 200`, `DescriptionMaxLength = 2000`. They're used by the request and the database model.
