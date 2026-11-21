# Research: Tables That Fit Without Sideways Scrolling

## R1. Why the tables are too wide
- `ui.cell` sets `whitespace-nowrap` on every cell, so a 200-character name is one 200-character line.
- In the editor, every material line also has a full-width replacement `<select>` whose options repeat long names.
- The work line's "Darbas: norm val. × [rate] €/val." cell cannot wrap.
- The actions are full-size buttons.

## R2. Wrapping names
- **Decision**: a shared `ui.nameCell` that wraps (`whitespace-normal`) with a maximum width, and a one-purpose component `ClampedText` that renders `<span class="line-clamp-2 break-words" title={text}>{text}</span>`.
  - `line-clamp-2` clips visually after 2 lines with "…".
  - The full text stays in the DOM, so screen readers read it, and `title` shows it on hover.
  - For keyboard focus (spec US1-2), the cell's row actions already name the item, so no extra focusable element is added (YAGNI).
- **Alternatives**: truncate in JavaScript. Rejected because it loses the full text for screen readers.

## R3. Small buttons with a 44 px touch target (spec Q2)
- **Decision**: `ui.smallButton` and `ui.smallDangerButton`: `px-2 py-1 text-sm` (~28–32 px), plus `pointer-coarse:min-h-11 pointer-coarse:min-w-11`. Tailwind 4's `pointer-coarse` variant applies only on touch screens, giving them the 44 px target from docs/ux.md.
- Row-action links ("Redaguoti") use the same small style.

## R4. Replacement dialog
- **Decision**: a new `ReplaceMaterialDialog` (role="dialog", aria-modal, labelled by its title). The same patterns as `ConfirmDialog`: fixed overlay, Escape closes, Tab stays inside, focus returns to the opener. Inside:
  - a search input that has the focus when the dialog opens;
  - a listbox of buttons (one per material: name and price), filtered with case-insensitive "contains" using `toLocaleLowerCase('lt-LT')`, sorted by name with `localeCompare('lt')`, scrolling inside (`max-h-[50vh] overflow-y-auto`);
  - an "Atšaukti" button.

  Arrow keys move between the search field and the options; Enter or a click chooses. The existing `onReplace(materialId)` is reused, so the server call and messages don't change (FR-023).
- **Alternatives**:
  - Extend `ConfirmDialog`. Rejected: different purpose and roles (alertdialog with two buttons), and mixing them would add options (X.1).
  - A combobox library. Rejected: a new dependency for one list (Article III).

## R5. Fitting the editor at 1280 px
- **Decision**:
  - The name column wraps with a max width of about 22rem.
  - The work line's norm-and-rate cell may wrap (two short lines).
  - Inline number fields narrow from `w-28` to `w-24`; the longest allowed values ("999999,999", "0,0616") still fit.
  - The actions become small buttons in one cell.
  - The table keeps `ui.tableWrapper` (`overflow-x-auto`), which is now only used below 768 px (spec Q1).
- **Verification**: jsdom has no layout, so SC-020-1 is a manual check at 1280 px and 1920 px with 200-character names, recorded in tasks.md. Automated tests cover the dialog, the accessible names and the tooltip text.
