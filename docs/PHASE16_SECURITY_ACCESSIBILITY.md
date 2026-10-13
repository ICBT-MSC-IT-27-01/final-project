# Phase 16 Security and Accessibility

Generated: 2026-10-13

## Security Testing

Verified by automated tests:

- Anonymous users are redirected from protected admin routes.
- Public read-only recommendation remains accessible without login.
- Registered users can access history through authenticated session state.
- Agricultural Officers can access validation workflow but do not receive admin navigation.
- Administrators can access admin dashboard.
- Public registration does not expose role selection.
- Invalid login returns a safe user-facing error.
- Recommendation history remains feature-flag controlled by backend tests.
- Browser E2E blocks operational forecast generation through a fail-closed mocked response.
- Staged changes were scanned for credential-like strings before Git closure.

Dependency security:

- `npm audit --audit-level=high` initially found Angular-tooling transitive advisories.
- `npm audit fix` was applied.
- Final `npm audit --audit-level=high` result: 0 vulnerabilities.

## Accessibility and Usability

Automated/browser coverage:

- Browser tests query headings, links, buttons, labels, alerts, and status regions by accessible roles/names.
- Public, registered, officer, and admin workflows are keyboard-addressable through native controls.
- Form inputs have visible labels.
- Error messages use `role="alert"` where implemented.
- Loading/success status messages use status regions where implemented.
- Desktop and mobile viewport browser tests passed.

Code improvement:

- Added explicit `:focus-visible` outline styling for links, buttons, inputs, selects, and textareas.

## Limitations

No formal WCAG certification was performed. No automated axe-core audit package was added. Manual assistive-technology testing and participant usability review remain future dissertation activities.
