# Phase 3 Authentication Decision

Phase 3 uses JWT Bearer authentication for the Angular SPA and ASP.NET Core Web API prototype.

## Strategy

- Registration and login are exposed through `/api/auth`.
- Normal registration always assigns the `Registered User` role.
- The client cannot submit `Role`, `RoleId`, `IsAdmin`, or officer/admin flags through the approved registration DTO.
- Passwords are hashed with ASP.NET Core `PasswordHasher<User>`.
- JWT claims are limited to user identifier, email, and role.
- Inactive users cannot log in, and token validation checks that the authenticated user is still active.

## Logout

JWTs are stateless in Phase 3. Logout is handled by removing the token on the client. Server-side token revocation and refresh-token storage are not implemented in this phase.

## Configuration

JWT settings are configuration-driven:

- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SigningKey`
- `Jwt:ExpirationMinutes`

Committed signing-key values are placeholders only. Developers should supply a local signing key with .NET user secrets or environment variables.

## Validation

Registration requires name, valid email, and a password of at least 8 characters. The password length is a technical security baseline, not an agricultural/domain rule.
