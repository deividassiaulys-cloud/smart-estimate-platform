# API contract changes: Remove the OpenAPI Document

| Address | Before | After |
|---|---|---|
| `GET /openapi/v1.json` (Development only) | 200, the generated OpenAPI document | 404, like any unknown address |

Every other endpoint is unchanged: route, method, authorization, request validation, status codes and response bodies.
