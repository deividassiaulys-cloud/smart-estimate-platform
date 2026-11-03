# Quickstart: Validating Cross-Cutting Requirements

## Checks after every task

```bash
cd server && dotnet build && dotnet test
cd client && npm run lint && npm run build && npm run test
```

## Manual walk-through (client running; mock API or real API)

1. As a Manager at 1280 px: the menu bar shows the company name, Pagrindinis, Projektai, Medžiagos, Darbai, Komanda and Atsijungti; the current item is marked; the Dashboard has no second logout.
2. Resize to 360 px: "Meniu" opens a full-height panel; Escape closes it; choosing "Darbai" opens Works and closes it.
3. At 360 px, open Projects, Materials, Works, Team and Project Detail → cards, no sideways page scroll. The Estimate Editor table scrolls only inside its frame.
4. The Material form with a long name heading ("Redaguoti medžiagą") on a narrow screen → the two lines don't overlap. Prefilled numbers use a comma (4,50; 0,8).
5. The editor's totals show "Pridėtinės išlaidos (12,5 %)" for 12.5 %.
6. As a Viewer: the menu has only Pagrindinis, Projektai and Atsijungti. Project → Atidaryti → "← Projektas" returns to that project.
7. Stop the API (or make it fail) and reload a page → a Lithuanian "Nepavyko …" message, never English or technical text.
8. At 1920 px every page stays in its centred column.

See [contracts/ui-navigation.md](contracts/ui-navigation.md).
