# UI Contract: Navigation and Layout

## Menu items by role

| Item | Path | Manager | Viewer |
|---|---|---|---|
| Pagrindinis | /dashboard | ✔ | ✔ |
| Projektai | /projects | ✔ | ✔ |
| Medžiagos | /materials | ✔ | — |
| Darbai | /works | ✔ | — |
| Komanda | /team | ✔ | — |
| Atsijungti | (logout → /login) | ✔ | ✔ |

The company name is shown at the start of the bar. The current page's item has `aria-current="page"`. There's no menu on `/`, `/login` or `/register`.

## Breakpoint (spec Q1)

| Width | Menu | Lists (Projects, Materials, Works, Team, estimates) |
|---|---|---|
| < 768 px | "Meniu" button → full-height panel (Escape / item / "Uždaryti" closes) | Cards: name as the title, other fields with labels, actions in a row |
| ≥ 768 px | Horizontal bar | Tables |

## HTTP

Outside Development every response over HTTPS (to a non-localhost host) carries `Strict-Transport-Security: max-age=2592000`.
