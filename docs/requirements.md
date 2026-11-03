1. Project Definition

Project title: Smart Estimate Management Platform

Project description: The Smart Estimate Management Platform is an application for construction service providers, independent contractors, and small construction companies who want to prepare construction estimates quickly and without errors. Estimates are currently prepared mostly in Excel spreadsheets or in programs installed on a single computer, making it difficult to keep prices and norms up to date and allowing only one person to work on a project at a time. The platform lets a company build its own catalog of materials and works, where each work has a material composition (e.g. “Wall A++” with boards, membrane, screws, and drywall). Building an estimate is as simple as selecting a work and entering a quantity — materials, their quantities, and totals with overhead costs are calculated automatically. An estimate has a status (Draft or Confirmed), making it clear which one is still being prepared and which one is final. Estimates can be viewed by company employees from anywhere, while a site worker can be shown only the list of works and materials, without prices. An estimate can be exported to Excel.

2. Problem Statement

What difficulties users face

Construction service providers, contractors, and small construction companies find it difficult to prepare a construction estimate quickly and without errors, and to update it when project scope, material prices, or labour norms change. Estimators spend a lot of time manually entering quantities, multiplying them by rates and norms, and recalculating final sums. Material quantities are often miscalculated — for example, losses or waste are not accounted for. Even a small change requires recalculating many lines. In addition, it is unclear which estimate is still being prepared and which has already been given to the client as the final version — this sometimes leads to an already-submitted price being accidentally changed.

How the problem is solved today

Two approaches are mainly used: Excel or similar spreadsheets, which each person creates and maintains themselves, or specialized estimating programs installed on a computer, where prices and norms are stored in a local database. In both cases, prices and norms are updated manually, based on supplier price lists or store prices. Estimates are passed to colleagues as files via email or messages.

Limitations of the current approach

Work limited to one place. Data is accessible only on the computer where the program is installed or the file is stored. Only one person can typically work on a project at a time.
Multiple file copies and versions. Passing files around creates multiple versions, and it’s unclear which one is current.
Manual price and norm updates. When a material’s price changes, it must be found and corrected in every estimate.
Risk of errors. In Excel spreadsheets, formulas can be accidentally overwritten or broken, and there is almost no data validation.
No clear separation between a draft and a final version, making it easy to accidentally change an already-agreed price.

Why it matters

Incorrect estimation of materials and labour leads to direct financial losses: a contractor may work at a loss or overprice and lose a client. A slow estimate recalculation delays submitting a quote to the client. When data is kept on a single computer, losing it means losing a lot of work, and the team cannot effectively share projects.

3. Target Users and User Roles

Visitor (unregistered user)

Goal: learn about the platform and register.
Can: view public information, register, log in.
Data: public information only.
Cannot: see any company data.

Company Manager

Goal: build estimates from the company’s own catalog and control who among the staff sees what.
Can:
create, edit, and delete materials and works (with material composition and norms);
create, edit, and delete projects and estimates;
add works to an estimate, change quantities, set overhead, profit, and VAT percentages;
confirm an estimate or return it to Draft status;
export estimates to Excel.
Data: all of their own company’s data.
Cannot: see or change other companies’ data. The last Manager of a company cannot be removed or demoted.

Viewer

Goal: see which works and materials are part of a project.
Can: view the list of projects and the names of works and materials in estimates.
Data: only the names of works and materials.
Cannot: see quantities, norms, prices, percentages, totals, or estimate status; change data; export.

4. Project Goals
G-1. Let a company describe work compositions (materials and their norms) once and quickly build estimates from them.
G-2. Shorten the time needed to prepare estimates and reduce the risk of calculation errors: totals are automatically recalculated when quantities or percentages change.
G-3. Let several company employees access the same projects from anywhere, without sending files.
G-4. Let the Manager control who sees prices and who sees only the list of works and materials.
G-5. Protect an estimate from accidental changes once it has been confirmed and handed to the client.
G-6. Protect each company’s data and preserve its ownership: one company’s data is not visible to another, and an estimate can be exported to Excel at any time.

5. Project Scope

In Scope (will be implemented in this version)

Registration with company creation, login, logout.
The Manager creates, removes, and manages Viewer accounts.
Company data isolation and permission checks.
A material catalog (price, unit of measurement) and a work catalog with material composition, norms, and waste.
Projects and estimates: selecting a work, quantity, automatic material generation and recalculation.
Overhead, profit, and VAT percentages.
Estimate status (Draft / Confirmed) with editing restrictions.
Role-based viewing (Manager sees everything, Viewer sees only names).
Estimate export to Excel.
Works in a browser on desktop and mobile.
If time allows: estimate copying, editing estimate lines, and PDF export.

Out of Scope (will NOT be implemented in this version)

Excel import (a possible future addition).
An administrator interface and company approval (registration works immediately).
Invitations, emails, email-based password recovery, two-factor authentication.
Multiple Manager roles per company, a Viewer with prices, a user belonging to multiple companies.
Concurrent-editing conflict warnings and real-time editing.
Estimate version history, templates, sections, discounts.
Commercial offers, a client role, invoices, payments, integrations.
Price history, multiple prices for the same material, automatic price updates.
Activity log, backup management, a mobile app, multiple languages.

6. MVP (Minimum Viable Product)

MVP definition

A user registers, a company is created, and they become the Manager.
The Manager creates materials and a work “Wall A++” with its composition and norms.
The Manager creates a project and an estimate (in draft), adds “Wall A++” (e.g. 35 m²). Material lines and totals are generated automatically.
When a quantity or percentage changes, totals are recalculated immediately.
The Manager confirms the estimate — it becomes read-only until explicitly returned to Draft.
The Manager creates a Viewer account; once logged in, the Viewer sees only the names of works and materials, without status or prices.
The Manager exports the estimate to Excel.
A user from another company cannot see this data.

Must Have

Registration with company creation, login, logout.
The Manager creates and removes Viewer accounts.
Company data isolation and permission checks.
Creating, editing, deleting, and searching materials and works; work composition with norms and waste.
Projects and estimates: adding a work, generating materials, changing quantities, copying prices into the estimate.
Automatic recalculation, overhead, profit, VAT.
Estimate status: Draft / Confirmed.
Viewing without prices for the Viewer.
Export to Excel.
Works in a browser on desktop and mobile.

Should Have

Estimate copying.
Editing an estimate line: changing the price, removing or replacing a material only within that estimate.
Updating an estimate’s prices and norms from the current catalog.
PDF export.

Could Have

Categories and filtering.
Reordering estimate lines.

7. Functional Requirements
ID	Requirement	Priority
FR-001	The system MUST allow a visitor to register by providing a name, surname, email, password, and company name. Registration MUST create a company and its Manager account.	Must have
FR-002	The system MUST NOT allow registration if the email is already associated with another account.	Must have
FR-003	The system MUST allow a user to log in with email and password, and to log out.	Must have
FR-004	The system MUST allow a Manager to create a Viewer account (name, surname, email, temporary password) and remove it. A removed Viewer MUST immediately lose access.	Must have
FR-005	The system MUST allow a Manager to set a new password for a Viewer.	Should have
FR-006	The system MUST show and return to a user only their own company’s data.	Must have
FR-007	The system MUST reject any action not permitted by the user’s role.	Must have
FR-008	The system MUST allow a Manager to create, edit, and delete materials (name, unit of measurement, price).	Must have
FR-009	The system MUST allow a Manager to create, edit, and delete works (name, unit of measurement, labour rate €/h, labour norm h/unit) and to link materials to a work with a consumption norm per work unit and a waste/loss percentage.	Must have
FR-010	The system MUST allow a Manager to search materials and works by part of the name, case-insensitively.	Must have
FR-011	The system MUST allow a Manager to create, edit, and delete projects.	Must have
FR-012	The system MUST allow a Manager to create an estimate within a project, add a work from the catalog to it, and specify a quantity. Once a work is added, the system MUST automatically create its material lines based on the work’s composition.	Must have
FR-013	The system MUST calculate each material line’s quantity using the formula: work quantity × consumption norm × (1 + waste % / 100).	Must have
FR-014	When adding a work to an estimate, the system MUST copy its and its materials’ prices and norms from the catalog. Later catalog changes or deletions MUST NOT change estimates already created.	Must have
FR-015	The system MUST automatically recalculate line and final totals when a quantity or percentage changes.	Must have
FR-016	The system MUST allow a Manager to set each estimate’s overhead, profit, and VAT percentages (0 to 100) and include them in the final total.	Must have
FR-017	The system MUST allow a Manager to change a work line’s quantity or remove a work line together with its material lines, while the estimate is in Draft status.	Must have
FR-018	A new estimate MUST be created with the status “Draft.”	Must have
FR-019	The system MUST allow a Manager to change an estimate’s status from “Draft” to “Confirmed.”	Must have
FR-020	While an estimate’s status is “Confirmed,” the system MUST NOT allow its works, materials, quantities, or percentages to be changed, until the Manager explicitly returns it to “Draft” status.	Must have
FR-021	The system MUST allow a Manager to return a confirmed estimate to “Draft” status.	Must have
FR-022	The system MUST show each estimate’s status in the estimates list.	Must have
FR-023	The system MUST allow a Manager to change a line’s price, or remove or replace a work’s material line within an estimate, without changing the company catalog (Draft status only).	Should have
FR-024	The system MUST allow a Manager to update an estimate’s prices and norms based on the current catalog (Draft status only), showing which lines will change beforehand.	Should have
FR-025	The system MUST allow a Manager to create a copy of an estimate (a new copy is always created with Draft status) in the same or another company project.	Should have
FR-026	The system MUST show a Viewer only the project list and the names of works and materials in estimates, without status.	Must have
FR-027	The system MUST allow a Manager to export an estimate to an Excel file containing the same data they see in the system, including status.	Must have
FR-028	The system MUST allow a Manager to export an estimate to a PDF file.	Should have
FR-029	The system MUST allow a Manager to assign a category to materials and works and to filter lists by it.	Could have

8. Non-Functional Requirements
ID	Requirement	Priority
NFR-001	All communication between the browser and the system MUST occur over an encrypted connection (HTTPS).	Must have
NFR-002	Passwords MUST be stored only as a password hash. It MUST NOT be possible to recover a password from the database.	Must have
NFR-003	The system MUST require a password of at least 8 characters.	Must have
NFR-004	After 5 unsuccessful login attempts within 10 minutes, the system MUST block further attempts on that account for 15 minutes.	Should have
NFR-005	Automated tests MUST verify that a user of each role does not receive unauthorized data or actions (FR-006, FR-007, FR-026), including requests for another company’s data by identifier.	Must have
NFR-006	Monetary amounts MUST be calculated and stored using a decimal type, and displayed with 2 decimal places. The same data MUST always produce the same result.	Must have
NFR-007	In the exported Excel file, text cells beginning with the characters =, +, -, or @ MUST be written as text.	Must have
NFR-008	The system MUST have an appropriate layout from 360px to 1920px screen widths, without horizontal page scrolling for normal content.	Must have
NFR-009	An estimate page (up to 200 lines) MUST load within 3 seconds, and totals MUST be recalculated within 1 second of a change.	Must have
NFR-010	Error and warning messages MUST be in Lithuanian, state what failed, and MUST NOT show technical details.	Must have
NFR-011	Irreversible actions (deletion) MUST require explicit user confirmation. Confirming an estimate (FR-019) MUST also require confirmation, since it restricts further editing.	Must have
NFR-012	The interface MUST be in Lithuanian, amounts shown in euros (€), and numbers and dates formatted per Lithuanian conventions.	Must have
NFR-013	The system MUST work in the two latest versions of Chrome, Edge, Firefox, and Safari.	Should have
NFR-014	The system MUST collect only necessary personal data (name, surname, email).	Should have

9. Main Business Rules
BR-001. A user belongs to one company. Registration creates a company and its sole Manager.
BR-002. Email is unique; addresses are compared case-insensitively.
BR-003. Only a Manager creates and removes Viewer accounts. A Manager cannot be removed.
BR-004. Only a Manager can create, modify, delete, and export data.
BR-005. A Viewer never receives quantities, norms, prices, percentages, totals, or estimate status, either on screen or in exports.
BR-006. A material or work is identified within a company by its name and unit of measurement, and cannot be duplicated.
BR-007. Price and norm cannot be negative; waste and estimate percentages must be between 0 and 100; estimate quantity must be greater than 0.
BR-008. Units of measurement come from a fixed system list (pcs, m, m², m³, kg, t, l, h, set).
BR-009. A work may have zero or more materials; the same material may be used in multiple works.
BR-010. An estimate line stores a copy of its price and norms. Catalog changes or deletions do not affect estimates.
BR-011. A project belongs to one company, an estimate to one project. Deleting a project deletes its estimates. Deletion is irreversible.
BR-012. A new estimate is always created with “Draft” status.
BR-013. An estimate’s works, materials, quantities, and percentages cannot be changed while its status is “Confirmed.” To edit, the estimate must be returned to “Draft.”
BR-014. Only a Manager can change an estimate’s status.
BR-015. Line calculation:
labour cost = quantity × labour norm (h/unit) × labour rate (€/h);
material quantity = work quantity × consumption norm × (1 + waste % / 100);
material cost = material quantity × unit price;
direct costs = sum of all labour and material costs.
BR-016. Percentages are calculated sequentially, and the final total is their sum:
overhead = direct costs × p₁ %;
profit = (direct costs + overhead) × p₂ %;
VAT = (direct costs + overhead + profit) × p₃ %.
BR-017. Only monetary amounts are rounded, to 2 decimal places (0.005 → 0.01). The final total is the sum of the already-rounded parts. Quantities and norms are not rounded, shown to 3 decimal places.

10. Main Data Entities
Company: a construction company or contractor (name).
User: an account (name, surname, email, password), role (Manager / Viewer), belongs to one company.
Material: a company catalog material (name, unit, price).
Work: a company catalog work (name, unit, labour rate, labour norm per hour per unit).
Work Material Norm: the relationship between a work and a material (consumption norm, waste %). Forms the work’s “composition.”
Unit of Measurement: a fixed system list.
Project: an order or site (name, description).
Estimate: a project’s calculation (name, status: Draft / Confirmed, overhead, profit, and VAT percentages).
Estimate Work Line: a work with a quantity and a copy of its price and norms.
Estimate Material Line: generated from the work’s composition, belongs to a work line, stores a copy of its own price and norms.
Category: groups materials and works.
Relationships: a company has many users, materials, works, and projects. A project has many estimates; an estimate has work lines; a work line has material lines. Estimate lines are only derived from the catalog but do not depend on it. Everything except the unit of measurement belongs to one company. An estimate’s status controls whether its lines can be changed (BR-013).

11. Assumptions, Constraints, and Dependencies

Assumptions

A user belongs to one company and, in one session, works only with that company.
Each company has exactly one Manager — the one who registered it.
A real Excel file sample (column structure) can be obtained later, when an import addition is built; until then, the catalog is filled in manually within the system.
Users have at least basic digital literacy skills (browser, filling in forms).
One interface language (Lithuanian) and one currency (euro) are sufficient.

Constraints

The backend MUST be ASP.NET Core (.NET). A REST API serving data to the React interface.
The frontend MUST be React. The user interface communicates with the backend only via an API (JSON), separate from server-side rendering.
Authentication between React and ASP.NET Core MUST occur via tokens (e.g. JWT), not server-side session cookies, since the interface is a separate application (SPA).
The system must work in a browser, without a separate installation on the user’s device.
The interface and all messages shown to the user must be in Lithuanian.
Monetary calculations must use a decimal type (C# decimal), not floating-point numbers (NFR-006), both in backend logic and in database column types.
Excel export is generated on the backend (ASP.NET Core), not in the browser, so the result does not depend on the user’s device capabilities.

Dependencies

Backend: ASP.NET Core Web API, Entity Framework Core (for data access), a relational database.
Excel export requires a .NET library that generates .xlsx files server-side.
Password storage requires a slow hashing algorithm library available in the .NET ecosystem.
Since there is no email sending (Out of Scope), a Manager’s password recovery is possible only manually through the database, or is left as a known system limitation.

12. Initial README (updated)
markdown
# Smart Estimate Management Platform

## About the Project
A web application for construction service providers and contractors that
enables fast, error-free preparation of construction estimates from a
defined catalog of materials, works, prices, and norms, with automatic
recalculation of totals.

## Problem
Estimates are currently prepared in Excel spreadsheets or in programs
installed on a single computer, which limits work to one location, makes
price updates manual, and carries a high risk of errors.

## Target User Groups
- **Manager** – the company owner / estimator, manages the catalog and estimates.
- **Viewer** – an employee (e.g. on a construction site), sees only the
  lists of works and materials, without prices.

## Core Functionality (MVP)
- Registration with company creation
- Material and work catalog with work compositions (material norms)
- Project and estimate creation with automatic recalculation
- Estimate status: Draft / Confirmed
- Role-based viewing
- Export to Excel

## Project Status
Requirements and design phase.

## Technologies
- **Backend:** ASP.NET Core Web API (.NET)
- **ORM / Data Access:** Entity Framework Core (Microting.EntityFrameworkCore.MySql provider)
- **Database:** MySQL (managed via MySQL Workbench)
- **Frontend:** React (Vite, TypeScript)
- **Authentication:** ASP.NET Core Identity (password hashing) + JWT

## Documentation
- docs/requirements.md – full project requirements specification
- docs/ux.md – user experience, user flows, and wireframes
- specs/ – Spec Kit feature specifications
