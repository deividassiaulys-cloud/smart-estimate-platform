# Research: Remove the OpenAPI Document

## R1. The attributes have no runtime effect
- **Decision**: remove every `[ProducesResponseType]`.
- **Rationale**: `ProducesResponseTypeAttribute` only implements API-explorer metadata (`IApiResponseMetadataProvider`). It does not filter output, choose a status code or validate anything. Status codes come from the action code (`StatusCode(201, …)`, `NoContent()`), the exception handler (404/409/423) and `[ApiController]` (automatic 400). With the OpenAPI document gone, nothing reads the metadata.
- **Alternatives**: keep the attributes for readability. Rejected: they duplicate what the action code and the exception handler already define and must be kept in sync by hand (X.1, X.8). Dropping only the 30 redundant 200 responses was rejected by the student in favour of the full removal.

## R2. `[ApiController]` stays
- **Decision**: `ApiControllerBase` keeps `[ApiController]`, which gives attribute routing and the automatic 400 `ValidationProblemDetails` response (X.10). Only its `[ProducesResponseType<ValidationProblemDetails>(400)]` goes.

## R3. Endpoint documentation after the removal
- **Decision**: the hand-written contracts in specs/*/contracts are the endpoint documentation (spec Q1). docs/architecture.md says so.
- **Alternatives**: a hand-written docs/api.md. Rejected (YAGNI; the contracts already exist per feature).

## R4. Verification
- **Decision**: the unchanged test suites, with equal counts to the baseline, prove behaviour is unchanged. They cover 400/401/403/404/409/423 and 201/204 per endpoint. A manual Development request to `/openapi/v1.json` proves the document is gone, because tests run in the "Testing" environment, where it was never mapped.
