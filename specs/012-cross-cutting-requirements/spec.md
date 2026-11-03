# Feature 12: Cross-Cutting Requirements

**Feature Branch**: `012-cross-cutting-requirements`
**Created**: 2026-11-03
**Status**: Draft
**Input**: Finish the non-functional requirements that no single feature covers: NFR-001, NFR-008, NFR-010 and NFR-012 in docs/requirements.md, and the navigation and responsive design in docs/ux.md §7–8. This feature adds no new business features.

## Specification
Every logged-in user can move between the parts of the application they may use from any page, on a phone (from 360 px wide) as well as on a large screen (up to 1920 px), without the page scrolling sideways. Everything the user reads, including errors, is in Lithuanian and formatted the Lithuanian way, never showing technical details. Browsers are told to keep using a secure connection.

## User Scenarios & Testing

### User Story 1: Navigate from anywhere (Priority: P1)
As a logged-in user, I want a navigation menu on every page, so that I can reach Dashboard, Projects and (as a Manager) Materials, Works and Team, or log out, without going back step by step.

**Why this priority**: today the only way between areas is the Dashboard and "← back" links (docs/ux.md §7 requires a navigation bar).

**Independent Test**: on any logged-in page, the menu shows exactly the items of the user's role, and each item opens its page.

**Acceptance Scenarios**:
1. Given a Manager on any page, when the page loads, then the menu shows Pagrindinis, Projektai, Medžiagos, Darbai, Komanda and Atsijungti; the current page's item is marked.
2. Given a Viewer, then the menu shows only Pagrindinis, Projektai and Atsijungti.
3. Given a screen narrower than a desktop, when the user presses the menu button, then a full-height panel opens with the same items stacked, and choosing an item opens it and closes the panel.
4. Given the Landing, Login or Register page (nobody logged in), then no menu is shown.

### User Story 2: Use the app on a phone (Priority: P1)
As a user on a phone, I want every page to fit the screen, so that I can work on site without scrolling sideways.

**Why this priority**: NFR-008 is a Must Have, and Viewers use the app on site (docs/ux.md §8).

**Independent Test**: at 360 px wide every page has no horizontal page scroll, and the lists show one card per item.

**Acceptance Scenarios**:
1. Given a 360 px wide screen, when any page is opened (Dashboard, Projects, Project Detail, Materials, Works, Team, every form, the Estimate Editor, the Estimate View), then the page does not scroll sideways.
2. Given a 360 px wide screen, when the Projects, Materials, Works or Team list is shown, then each item is a card showing its one or two most important fields and its actions.
3. Given a 1920 px wide screen, then the content stays readable in its centred column.
4. Given a page heading long enough to wrap onto two lines, then the lines do not overlap.

### User Story 3: Clear errors in Lithuanian (Priority: P2)
As a user, I want every error or warning to be in Lithuanian and say what failed, so that I understand it and never see technical details.

**Why this priority**: NFR-010. Pages already map known errors to Lithuanian texts; this makes it a guarantee and covers unexpected failures.

**Independent Test**: for every request a page makes, an unexpected server failure (or a server text in English) results in a Lithuanian message on the page, never the server's own text or technical details.

**Acceptance Scenarios**:
1. Given any page, when a request fails unexpectedly (server error or no connection), then a Lithuanian message says what could not be done ("Nepavyko …"), and the rest of the page stays usable.
2. Given a server error response, then no English server text, error code, stack trace or field name is shown to the user.
3. Given an unexpected server failure in production, then the response itself contains no technical details.

### Edge Cases
- Pages without a logged-in user (Landing, Login, Register) have no menu; after logout the menu disappears.
- On the Estimate Editor, a wide table of lines may scroll sideways inside its own frame on a phone, but the page itself does not.
- The Viewer's Estimate View, opened from a project, links back to that project; opened directly (e.g. a bookmark), it links to the project list.
- A session that expires while the mobile menu is open closes the menu and shows Login with the existing "session expired" message.
- Numbers, money and dates use Lithuanian formatting everywhere (decimal comma, € after the amount, dates as yyyy-MM-dd), through the existing format helpers.
- Outside development, the API tells browsers to use only HTTPS from then on (NFR-001); in development nothing changes.

## Requirements

### Functional Requirements
- **FR-012-1**: Every logged-in page shows a navigation menu with the items the user's role may use (Manager: Pagrindinis, Projektai, Medžiagos, Darbai, Komanda, Atsijungti; Viewer: Pagrindinis, Projektai, Atsijungti), marking the current one, with the user's company name on its left. The Dashboard's own header (company name and logout) is replaced by this menu, so every page has exactly one logout action.
- **FR-012-2**: On screens narrower than 768 px the menu collapses behind a menu button that opens a full-height panel with the same items, operable by keyboard and closable with Escape.
- **FR-012-3**: No page scrolls horizontally from 360 px to 1920 px for normal content (NFR-008); the Projects, Materials, Works and Team lists become cards on screens narrower than 768 px and stay tables from 768 px up (docs/ux.md §8).
- **FR-012-4**: Headings that wrap never overlap.
- **FR-012-5**: The Viewer's Estimate View links back to the project it was opened from.
- **FR-012-6**: Every message shown to a user is Lithuanian, states what failed and contains no technical details or server text (NFR-010); unexpected failures show a generic Lithuanian message. The server's error texts stay as they are (English, for developers and API clients, pinned by existing tests and contracts); the client always shows its own Lithuanian text chosen by the response status and never displays the server's text.
- **FR-012-7**: Outside development the API sends a strict-transport-security instruction (NFR-001).
- **FR-012-8**: Money, numbers and dates are displayed with Lithuanian formatting everywhere (NFR-012), using one set of format helpers.

### Key Entities
None. No data, endpoints or database changes.

## Success Criteria

### Measurable Outcomes
- **SC-012-1**: 100% of logged-in pages show the role's menu items and no others (automated tests for Manager and Viewer).
- **SC-012-2**: 0 pages with a horizontal page scrollbar at 360 px and at 1920 px (checked page by page).
- **SC-012-3**: 0 occurrences of English server text or technical details in any message a page shows when a request fails (automated tests).
- **SC-012-4**: In production responses, the HTTPS-only instruction is present (automated test); in development it is absent.
- **SC-012-5**: All existing automated tests keep passing.

## Assumptions
- The menu labels are the Lithuanian forms of docs/ux.md §7 (Dashboard → Pagrindinis, Projects → Projektai, Materials → Medžiagos, Works → Darbai, Team → Komanda, Logout → Atsijungti).
- "Normal content" (NFR-008) allows a wide data table to scroll inside its own frame (the Estimate Editor's line table); the page itself never scrolls sideways.
- The existing per-page back links stay; the menu adds to them.
- NFR-013 (browsers) and NFR-009 (performance) need no new work: the app uses standard features of current browsers and, by design, calculates in memory (spec 007).

## Clarifications
- Existing tasks T111–T113 in tasks.md are the starting point; they will be rewritten by /speckit-tasks.

### Session 2026-11-03
- Q: At what width does the layout switch to mobile? → A: Below 768 px the menu is a hamburger and the lists are cards; from 768 px up, the menu bar and tables.
- Q: Do Lithuanian error texts come from the server or the client? → A: The client: it shows its own Lithuanian text by status, with a common "Nepavyko … Bandykite dar kartą." fallback; the server's English texts are unchanged and never displayed (tests prove it).
- Q: What happens to the Dashboard's own header once the menu exists? → A: The menu shows the company name on the left and the items, including Atsijungti, on the right; the Dashboard's own header is removed (one logout per page).
