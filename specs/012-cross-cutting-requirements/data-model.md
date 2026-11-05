# Data Model: Cross-Cutting Requirements

**No data, entity, DTO or database change.** This feature changes only the presentation (navigation, layout, number display, error texts) and one HTTP response header (HSTS).

Client-only state:

| State | Where | Notes |
|---|---|---|
| Company name for the menu | `AppLayout` (from `GET /api/auth/me`, loaded once per session of the layout) | Same endpoint the Dashboard already uses |
| Mobile menu open/closed | `NavBar` | Closed on route change and Escape |
| Project to return to | React Router location state on `/estimates/:id/view` | Only for the back link; not sent to the server |
