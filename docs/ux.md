1.	Screen Inventory
	Screen	Purpose	Accessible to
1	Landing Page	Public page explaining the product; entry point for visitors.	Visitor
2	Register	Create a new account; registration also creates the company and makes the user its Manager.	Visitor
3	Login	Authenticate with email and password.	Visitor
4	Dashboard	Landing screen after login; shows a summary (recent projects/estimates) and role-appropriate shortcuts.	Manager, Viewer
5	Materials List	Browse, search and manage the company's materials (name, unit, price).	Manager (full), Viewer (not accessible — no pricing data)
6	Material Form (create / edit)	Add or edit a single material.	Manager
7	Works List	Browse, search and manage the company's works (name, unit, labour rate, labour norm).	Manager
8	Work Form (create / edit)	Create or edit a work, including its material composition (linked materials, consumption norm, waste %).	Manager
9	Team / Viewers List	List of Viewer accounts in the company; create, remove, reset password.	Manager
10	Viewer Account Form (create / reset password)	Create a new Viewer account or set a new password for an existing one.	Manager
11	Projects List	List of the company's projects.	Manager, Viewer
12	Project Form (create / edit)	Create or edit a project (name, description).	Manager
13	Project Detail	Shows one project and the list of its estimates.	Manager, Viewer
14	Estimate Editor	Main working screen: "+ Pridėti skyrių" and "+ Pridėti darbą" open dialogs with a search field, the list, the quantity (and for a work its section) and "+ Kurti naują…", which opens the catalog form on top of the editor and returns with the new item chosen (feature 020); add a catalog section with one quantity (all its works and materials are added), add works to a section or to none, set quantities (changing a section's quantity recalculates its works after a warning; removing a section asks first), view auto-generated material lines, edit a line's labour rate or material price, replace a material line ("Keisti" opens a dialog with a search field and the same-unit catalog materials) or remove it (Draft only, this estimate only), update prices and norms from the catalog after a preview of every change (Draft only), set overhead/profit/VAT %, see section subtotals (only when the estimate has sections) and totals, change status (Draft/Confirmed).	Manager
15	Estimate View (read-only)	Read-only view of an estimate. Manager sees full data (quantities, prices, sums, status); Viewer sees only section, work and material names.	Manager, Viewer
16	Export Confirmation	Confirms and triggers the Excel (and later PDF) export of an estimate.	Manager
17	Not Found (404)	Shown when a route or resource does not exist or does not belong to the user's company.	All authenticated roles
18	Unauthorized / Forbidden	Shown when a user tries to access a screen or action their role does not permit.	All authenticated roles
19	Catalog Sections List ("Skyriai")	Browse, search and delete (with confirmation) the company's section templates (name, unit, number of works); a work used in a section cannot be deleted (feature 019).	Manager
20	Catalog Section Form (create / edit)	Create or edit a section template, e.g. "A++ siena": name, unit, and catalog works each with a coefficient (work quantity per 1 unit of the section).	Manager


2.	Main User Journeys
Journey 1: Manager sets up the company and creates a first estimate
Starting point: Visitor on the Landing Page, no account yet.
Goal: Go from no account to a finished, confirmed estimate ready to export.
1.	Visitor opens the Landing Page and clicks "Register".
2.	Visitor fills in the Register form (name, surname, email, password, company name) and submits.
3.	System creates the company and the user's Manager account, then logs the user in and redirects to the Dashboard.
4.	Manager navigates to Materials List and adds the materials needed (e.g. boards, membrane, screws, drywall) via the Material Form.
5.	Manager navigates to Works List and creates a work (e.g. "Wall A++") via the Work Form, setting its labour rate, labour norm, and linking the materials with consumption norms and waste %.
6.	Manager navigates to Projects List, creates a new project via the Project Form.
7.	From the Project Detail screen, Manager creates a new estimate, which opens in the Estimate Editor (status: Draft).
8.	Manager adds the work "Wall A++" with a quantity (e.g. 35 m²); material lines are generated automatically.
9.	Manager sets overhead, profit, and VAT percentages; totals recalculate automatically.
10.	Manager changes the estimate status to "Confirmed".
Expected final state: A Confirmed estimate exists in the project, with correct totals, visible in Estimate View.

Journey 2: Manager invites a site worker and the worker checks what's needed
Starting point: Manager is logged in and already has at least one project with an estimate.
Goal: A Viewer (site worker) can see what materials and works are in a project without seeing prices.
1.	Manager navigates to Team / Viewers List and clicks "Add Viewer".
2.	Manager fills in the Viewer Account Form (name, surname, email, temporary password) and submits.
3.	System creates the Viewer account, scoped to the Manager's company.
4.	Manager shares the login credentials with the worker (outside the system, e.g. verbally or by message).
5.	Viewer opens Login, signs in with the given credentials, and lands on the Dashboard.
6.	Viewer navigates to Projects List, selects a project, and opens Project Detail.
7.	Viewer opens an estimate, which displays in Estimate View showing only work and material names (no quantities, prices, or totals).
Expected final state: Viewer can see which works and materials are part of the project, with no access to any commercial data.

Journey 3: Manager updates an existing estimate and exports it
Starting point: Manager is logged in and has a Draft estimate from a previous session.
Goal: Adjust the estimate to reflect a changed quantity and export the final result to Excel.
1.	Manager navigates to Projects List, opens a project, then opens an existing Draft estimate in the Estimate Editor.
2.	Manager changes the quantity of a work line (e.g. from 35 m² to 40 m²).
3.	System automatically recalculates material quantities and all totals.
4.	Manager reviews the updated totals and sets the estimate status to "Confirmed".
5.	Manager clicks "Export to Excel" from the estimate screen, triggering the Export Confirmation.
6.	Manager confirms, and the system generates and downloads the Excel file.
Expected final state: The estimate reflects the updated quantity, is Confirmed, and an Excel file matching the on-screen data has been downloaded.


 
 3.	User Flow Diagrams
Registration & Login
 


 Create a Work
 

Build an Estimate (Add Work, Auto-generate Materials, Recalculate)
 


 Viewer Accesses an Estimate (Role-Restricted View)
 


 4.	Low-Fidelity Wireframes
The wireframes are image files in docs/wireframes:
Wireframe	Screen (section 1)
docs/wireframes/wf07_materials_list.png	5 Materials List
docs/wireframes/wf08_material_form.png	6 Material Form
docs/wireframes/wf09_works_list.png	7 Works List
docs/wireframes/wf10_projects_list.png	11 Projects List
docs/wireframes/wf11_project_form.png	12 Project Form
docs/wireframes/wf12_project_detail.png	13 Project Detail
docs/wireframes/wf13_team_list.png	9 Team / Viewers List
docs/wireframes/wf14_viewer_form.png	10 Viewer Account Form
docs/wireframes/wf15_dialogs.png	Confirmation dialogs (section 6), incl. 16 Export Confirmation

Note: the wireframe labels are in English for this documentation only. The application interface itself is in Lithuanian (NFR-012), e.g. "Delete" is "Ištrinti" and "Cancel" is "Atšaukti".

 5.	Form Behaviour
Register Form
Field	Required	Input restrictions	Validation message (on error)
First name	Yes	1–100 characters, not blank	"First name is required."
Last name	Yes	1–100 characters, not blank	"Last name is required."
Email	Yes	Valid email format, max 255 characters	"Please enter a valid email address." / "This email is already registered."
Password	Yes	Min. 8 characters, cannot equal email	"Password must be at least 8 characters."
Company name	Yes	1–200 characters, not blank	"Company name is required."

Login Form
Field	Required	Input restrictions	Validation message (on error)
Email	Yes	Valid email format	"Please enter a valid email address."
Password	Yes	Not blank	"Please enter your password."

Material Form (create / edit)
Field	Required	Input restrictions	Validation message (on error)
Name	Yes	1–200 characters	"Material name is required."
Unit	Yes	One of the fixed unit list	"Please select a unit."
Price (€)	Yes	Number ≥ 0, iki 4 skaitmenų po kablelio (up to 4 decimal places, e.g. 0,0616 €)	"Price must be 0 or more, with at most 4 decimals."

Work Form (create / edit)
Field	Required	Input restrictions	Validation message (on error)
Name	Yes	1–200 characters	"Work name is required."
Unit	Yes	One of the fixed unit list	"Please select a unit."
Labour rate (€/h)	Yes	Number ≥ 0, iki 4 skaitmenų po kablelio (up to 4 decimal places, e.g. 0,0616 €/h)	"Labour rate cannot be negative."
Labour norm (h/unit)	Yes	Number ≥ 0, iki 6 skaitmenų po kablelio (up to 6 decimal places, e.g. 0,000884)	"Labour norm cannot be negative."
Material composition line — Material	No (0+ lines allowed)	Must exist in company's material base	"Please select a material."
Material composition line — Consumption norm	Yes, if a line exists	Number > 0, iki 6 skaitmenų po kablelio (up to 6 decimal places)	"Consumption norm cannot be negative."
Material composition line — Waste %	Yes, if a line exists	Number between 0 and 100	"Waste % must be between 0 and 100."

Project Form (create / edit)
Field	Required	Input restrictions	Validation message (on error)
Name	Yes	1–200 characters	"Project name is required."
Description	No	Max 2000 characters	—

Estimate Line — Quantity Edit (inline, within Estimate Editor)
Field	Required	Input restrictions	Validation message (on error)
Quantity (per work line)	Yes	Number > 0, up to 3 decimals	"Quantity must be greater than 0."
Labour rate (per work line)	Yes	Number ≥ 0, iki 4 skaitmenų po kablelio (up to 4 decimal places)	"Price must be 0 or more, with at most 4 decimals."
Price (per material line)	Yes	Number ≥ 0, iki 4 skaitmenų po kablelio (up to 4 decimal places)	"Price must be 0 or more, with at most 4 decimals."
Replace material (per material line)	No	A catalog material with the same unit	"Choose a catalog material with the same unit."
Overhead %	Yes	Number between 0 and 100	"Overhead % must be between 0 and 100."
Profit %	Yes	Number between 0 and 100	"Profit % must be between 0 and 100."
VAT %	Yes	Number between 0 and 100	"VAT % must be between 0 and 100."

Viewer Account Form (create / reset password)
Field	Required	Input restrictions	Validation message (on error)
First name	Yes	1–100 characters	"First name is required."
Last name	Yes	1–100 characters	"Last name is required."
Email	Yes	Valid email format, unique across the system	"This email is already registered."
Temporary password	Yes	Min. 8 characters	"Password must be at least 8 characters."


6.	Application States
•	Loading: placeholders shown instead of a blank screen while data loads.
•	Empty: a clear message ("No projects yet.", "No works yet.", etc.) + an action button to create the first item (only if the role allows it). Viewer never sees that button.
•	Server error: a short message ("Could not load [data].") + a "Try again" button; the rest of the interface (navigation) stays usable.
•	Success (after an action): returns to the list/screen, the new or updated item is briefly highlighted.
•	Validation error: the invalid field is outlined in red with an inline message below it; other fields keep their values.
•	Server error (on submit): a banner at the top of the form; entered values are preserved, not cleared.
•	Success: redirect to the relevant list screen.
•	403 Unauthorized: shown when a role doesn't permit the screen or action.
•	404 Not Found: shown when an ID doesn't exist or belongs to another company — 404 is used deliberately instead of 403, so a user cannot infer that the record exists elsewhere.
•	Session expired: redirect to Login with the message "Your session has expired."
•	Confirmation before destructive actions (NFR-011): every destructive action opens the shared confirmation dialog (wireframe wf15) before anything is sent: deleting a material, a work, a project, an estimate work line or a Viewer, and confirming an estimate. The dialog names the item and explains the consequence (e.g. “Delete 'Boards 45x45'? This action cannot be undone.”). Focus starts on "Cancel", so pressing Enter never confirms by accident; Escape or "Cancel" closes it and nothing changes.
•	Blocked material deletion (BR-018): when a material is used in a work composition, the server refuses the deletion (409) and the list shows the message "Medžiagos negalima ištrinti, nes ji naudojama darbuose: <work names>." ("The material cannot be deleted because it is used in works: …"); the material stays in the list. A material used only in estimates can be deleted, because estimates keep their own copies (BR-010).

 7.	Navigation
Nav item	Manager	Viewer
Dashboard	✔	✔
Projects	✔	✔
Materials	✔	✘
Works	✔	✘
Team	✔	✘
Logout	✔	✔


8.	Responsive Design
Top navigation
•	Desktop: full horizontal nav bar (Dashboard, Projects, Materials, Works, Team, Logout).
•	Mobile: collapses into a hamburger menu; tapping it opens a full-height slide-in panel with the same items, stacked vertically.
Lists (Projects, Materials, Works, Team)
•	Desktop: table layout as in the wireframes (multiple columns per row).
•	Mobile: table becomes stacked cards — one card per item, showing the 1–2 most important fields prominently.
•	Laptop and desktop (from 1280 px): every table fits the page without sideways scrolling. Names and descriptions wrap onto at most 2 lines (longer ones end with "…" and show the full text as a tooltip); numbers, units and amounts do not wrap. Row actions are small buttons ("Keisti", "Šalinti", "Redaguoti", "Ištrinti") that keep a 44 px target on touch screens. Below 768 px the Estimate Editor table may scroll inside its own frame (feature 020).
Forms (Work Form, Project Form, Register, etc.)
•	Desktop: fields arranged in two columns where related (e.g. Labour rate / Labour norm side by side).
•	Mobile: all fields stack into a single column, full width.

9.	Basic Accessibility Requirements
•	All interactive elements (buttons, links, inputs) are reachable and operable by keyboard alone, in a logical tab order.
•	Every form field has a visible, associated label — no icon-only or placeholder-only inputs.
•	Validation errors are communicated through text, not color alone.
•	Page structure uses semantic HTML (headings, lists, tables) so screen readers can convey hierarchy, not just visual layout.
•	Status pills (Draft/Confirmed) include text, not just a color difference.
•	Minimum tap/click target size of ~44px for buttons and actions, especially on mobile.

10.	Wireframes to Requirements
No new functionality was introduced by the wireframes beyond what's already in the 1st assignment's requirements.
