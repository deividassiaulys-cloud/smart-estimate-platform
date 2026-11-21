# Quickstart: Tables That Fit Without Sideways Scrolling

## Automated checks
```bash
cd server && dotnet test        # unchanged (no server change)
cd client && npm run test       # dialog, accessible names, tooltips; existing tests unchanged in expectations
```

## Manual check (SC-020-1, required before merging)
At **1280 px** and **1920 px** browser width, with at least one name of 80–200 characters in each table:
1. Estimate Editor, Draft, with sections: no horizontal scroll bar on the table or the page; names wrap onto up to 2 lines with "…"; hovering shows the full name; actions are small "Keisti" / "Šalinti".
2. Click "Keisti": the dialog opens with focus in the search field. Type part of a name; use the arrow keys and Enter; Escape cancels and focus returns to "Keisti".
3. The materials, works and catalog-sections lists, the work form, the catalog section form, the projects list, a project's estimates list, the Viewer's estimate names list and the team list: no sideways scrolling, names wrap.
4. At 360 px: the lists are cards as before; the editor table may scroll inside its frame, but the page doesn't (spec Q1).
5. On a touch device or in DevTools' touch emulation, the small buttons are at least 44 px high (spec Q2).
