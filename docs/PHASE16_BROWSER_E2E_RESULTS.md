# Phase 16 Browser E2E Results

Generated: 2026-10-13

## Framework

Tooling:

- `@playwright/test`
- Project-local config: `src/frontend/playwright.config.ts`
- Browser channel: installed Google Chrome
- No browser binaries, screenshots, videos, or traces are committed.

Command:

```powershell
cd src/frontend
npm run e2e
```

## Results

Final result:

- 14 passed
- 0 failed
- Desktop Chrome project: PASS
- Mobile Chrome project: PASS
- Duration: 43.9 seconds in the final closure rerun

## Covered Scenarios

Browser scenarios:

- Public dashboard displays latest seven-day forecast evidence.
- Public recommendation page performs read-only evaluation.
- Six approved crops render.
- Missing soil and insufficient-evidence states render.
- API failure message is user-safe.
- Registered user registration, login, history, and logout are functional under mocked API.
- Public registration does not expose privileged role selection.
- Protected admin route redirects anonymous users to login.
- Invalid login displays safe error.
- Agricultural Officer can access validation workflow.
- Agricultural Officer does not see admin navigation.
- Administrator can access admin dashboard.
- Administrator forecast generation fails closed in E2E and does not trigger live weather generation.
- Public recommendation content remains reachable in mobile viewport.

## Side Effects

No live Open-Meteo request, LSTM inference, SQL Server write, or administrator operational generation occurred during browser E2E.

All API responses were deterministic Phase 16 browser fixtures.

## Limitations

The E2E tests validate browser behavior and role navigation. They do not prove real participant usability, expert officer validation, or agronomic correctness.
