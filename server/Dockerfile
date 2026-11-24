# API image (feature 022, T362, T368). Build context: server/
#   docker build --build-arg APP_VERSION=<commit sha> -t smart-estimate-api:<tag> server/
# The final image holds only the published API and the EF Core migrations bundle (/app/efbundle) on the
# ASP.NET Core runtime: no SDK, source code, tests or secrets. Settings and secrets come from the container's
# environment (docs/architecture.md section 4). The bundle runs before a new API starts (deploy/deploy.sh):
#   docker compose --profile migrate run --rm migrate

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Restore first, so the package layer is reused while only the code changes.
COPY SmartEstimatePlatform.Api/SmartEstimatePlatform.Api.csproj SmartEstimatePlatform.Api/
RUN dotnet restore SmartEstimatePlatform.Api/SmartEstimatePlatform.Api.csproj
COPY SmartEstimatePlatform.Api/ SmartEstimatePlatform.Api/
RUN dotnet publish SmartEstimatePlatform.Api/SmartEstimatePlatform.Api.csproj \
    --configuration Release --no-restore --output /out/app /p:UseAppHost=false

# The migrations bundle: one executable that applies every pending migration in Migrations/ and exits non-zero
# if one fails. dotnet-ef is a build tool only, pinned to the project's EF Core version, and is not in the
# final image. Building the bundle creates the DbContext once without connecting, so the build runs in Development
# (Program.cs's Production checks do not apply) with a placeholder connection string, which is not a secret and is
# not kept. When the bundle runs, it uses the container's environment and real connection string.
FROM build AS bundle
RUN dotnet tool install --global dotnet-ef --version 10.0.12
ENV PATH="$PATH:/root/.dotnet/tools"
RUN ASPNETCORE_ENVIRONMENT=Development \
    ConnectionStrings__DefaultConnection="Server=build-placeholder;Database=none" \
    dotnet ef migrations bundle \
    --project SmartEstimatePlatform.Api/SmartEstimatePlatform.Api.csproj \
    --configuration Release --output /out/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /out/app .
COPY --from=bundle /out/efbundle ./efbundle
# The commit the image was built from, reported by GET /health.
ARG APP_VERSION=dev
ENV APP_VERSION=$APP_VERSION \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# The non-root "app" user that the official .NET images provide.
USER $APP_UID
ENTRYPOINT ["dotnet", "SmartEstimatePlatform.Api.dll"]
