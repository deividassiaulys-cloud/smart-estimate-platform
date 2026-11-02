# API Contract: Projects

Every endpoint requires a logged-in user: 401 without a valid token. Another company's project is reported as 404, exactly like a missing one (feature 011).

| Endpoint | Who | Body | Success | Errors |
|---|---|---|---|---|
| GET /api/projects | Manager, Viewer | — | 200 `ProjectResponse[]`, A–Z by name, then by ID | — |
| GET /api/projects/{id} | Manager, Viewer | — | 200 `ProjectResponse` | 404 "Project not found." |
| POST /api/projects | Manager | `ProjectRequest` | 201 `ProjectResponse` | 400 validation; 403 for a Viewer |
| PUT /api/projects/{id} | Manager | `ProjectRequest` | 200 `ProjectResponse` | 400; 403; 404 |
| DELETE /api/projects/{id} | Manager | — | 204 | 403; 404 |

```json
// ProjectRequest
{ "name": "Buto remontas, Vilnius", "description": "Klientas: J. Jonaitis, Žirmūnų g. 1" }

// ProjectResponse
{ "id": 4, "name": "Buto remontas, Vilnius", "description": "Klientas: J. Jonaitis, Žirmūnų g. 1" }
```

A Viewer sees only the name and description, which contain nothing commercial (009).
