# Account and role management

CropCompass has two sign-in URLs: `/login` for Registered Users and `/staff/login` for Agricultural Officers and Administrators. Both forms authenticate against the account stored in SQL Server; users do not select a role. The app checks that the returned role matches the chosen sign-in page before saving the session.

| Access | How the account is obtained | After sign-in |
| --- | --- | --- |
| Public User | No account required | Public dashboard, seven-day forecast, recommendations and seasonal planning |
| Registered User | Self-register at `/register`; sign in at `/login` | Recommendations and saved recommendation history |
| Agricultural Officer | An existing Administrator assigns the role to a registered account; sign in at `/staff/login` | Officer validation workspace at `/officer` |
| Administrator | Provisioned for the first time by the database owner; subsequent Administrators can be assigned by an existing Administrator; sign in at `/staff/login` | Administration workspace at `/admin` |

## Create and manage accounts

1. A person registers at `/register` with a name, email address and password. The API always creates a **Registered User** account. Registration cannot assign Agricultural Officer or Administrator access.
2. An Administrator signs in at `/staff/login`, opens **Admin → Users**, and selects the appropriate role for an existing account. The available roles are Registered User, Agricultural Officer and Administrator. The same screen can mark an account active or inactive.
3. After a role change, the affected person signs out and signs in again so the new role is included in a new access token. Existing tokens can retain the old role until they expire. An inactive account cannot sign in or use an existing token on protected API requests.

The Administrator UI manages existing accounts; it does not create an Officer or Administrator account directly. A fresh database contains the approved role records but no initial user. For the **first Administrator**, the database owner registers the intended account normally, verifies its identity in the chosen SQL Server environment, and assigns that account the seeded Administrator role in an authorized database administration session. The person then signs in again. This first-account step applies only when no Administrator exists; subsequent role changes use Admin → Users. There is no public Administrator signup or default Administrator password.

## Sign-in and access boundaries

- Both sign-in pages use `POST /api/auth/login`. The Angular app accepts only Registered User accounts at `/login` and only Agricultural Officer or Administrator accounts at `/staff/login`, then sends them to `/recommendations`, `/officer`, or `/admin` according to the returned role. These page checks are for the sign-in flow; server-side authorization remains the security boundary.
- The backend enforces authorization for Administrator and Agricultural Officer APIs. The Angular route guards provide navigation behavior but are not the authorization boundary.
- Public forecast and recommendation viewing does not require registration. Saved recommendation history belongs to the authenticated Registered User.
- Agricultural Officers review and validate recommendations; they cannot manage crops, suitability configuration, users, or operational forecast generation.
- Administrators can manage users and roles through `GET /api/admin/users`, `PUT /api/admin/users/{id}/role`, and `PATCH /api/admin/users/{id}/active-status` or the Admin → Users screen.

Passwords, access tokens, signing keys and real account credentials must not be placed in this repository or shared in project documentation. The local development Administrator created for a particular machine is not part of the GitHub checkout. No self-service password reset is implemented in the current prototype.
