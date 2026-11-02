# API Contract: Code Structure Refactor

All routes, methods, authorization rules, success status codes and JSON field names stay exactly as in features 001–004.

## Unchanged

| Endpoint | Success | Errors (status, detail) |
|---|---|---|
| POST /api/auth/register | 201 AuthResponse | 400 invalid request; 409 "An account with this email already exists." |
| POST /api/auth/login | 200 AuthResponse | 401 "Invalid email or password."; 423 lock message + `minutesRemaining` |
| GET /api/auth/me | 200 CurrentUserResponse | 401 |
| GET /api/team | 200 TeamMemberResponse[] | 401, 403 |
| POST /api/team/viewers | 201 TeamMemberResponse | 400; 409 "An account with this email already exists." |
| DELETE /api/team/viewers/{id} | 204 | 404 "Viewer not found." |
| PATCH /api/team/viewers/{id}/password | 204 | 400; 404 "Viewer not found." |
| GET /api/materials?search= | 200 MaterialResponse[] | 401, 403 |
| GET /api/materials/{id} | 200 MaterialResponse | 404 "Material not found." |
| POST /api/materials | 201 MaterialResponse | 400; 409 "A material with this name and unit already exists." |
| PUT /api/materials/{id} | 200 MaterialResponse | 400; 404; 409 |
| DELETE /api/materials/{id} | 204 | 404 |
| GET /api/units | 200 string[] | 401 |

## Allowed differences (FR-014)

1. **Schema name**: `RegisterResponse` and `LoginResponse` are described as one `AuthResponse` schema. The JSON body is identical.
2. **400 body for invalid requests**: the standard validation-error format.

   ```json
   { "title": "One or more validation errors occurred.", "status": 400,
     "errors": { "Password": ["Password must be at least 8 characters long."] } }
   ```

   Before, the body was a single `detail` message. The status and the set of rejected inputs are unchanged.

   **Exception**: a password reset that equals the Viewer's stored email is still rejected by the service with the old single-`detail` 400.

   **Order**: an invalid body is now rejected before the service runs. So a request that has both an invalid body and an unknown ID (e.g. PUT /api/materials/999 with price −1, or a too-short reset password for an unknown Viewer) gets 400 where it used to get 404. Each request is still rejected, and nothing is saved.
3. **API description only**: the success types are declared explicitly, and 400 (all endpoints) and 401/403 (Manager-only endpoints) are now documented. Previously some of these responses were real but not listed.
