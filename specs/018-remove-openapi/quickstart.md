# Quickstart: Remove the OpenAPI Document

## Automated checks
```bash
cd server && dotnet test      # same count as the baseline in tasks.md
cd client && npm run test     # same count as the baseline in tasks.md
```

## Manual check (SC-018-3)
1. `cd server/SmartEstimatePlatform.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile --urls http://localhost:5086`
2. `curl -s -o /dev/null -w '%{http_code}' http://localhost:5086/openapi/v1.json` → `404`
3. `curl -s -o /dev/null -w '%{http_code}' http://localhost:5086/api/units` → `401` (the API itself still answers)
4. `grep -rn "ProducesResponseType\|OpenApi" server/SmartEstimatePlatform.Api --include=*.cs --include=*.csproj` → no output (excluding bin/ and obj/)
