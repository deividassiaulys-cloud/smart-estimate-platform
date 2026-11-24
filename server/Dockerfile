# API image (feature 022, T362). Build context: server/
#   docker build --build-arg APP_VERSION=<commit sha> -t smart-estimate-api:<tag> server/
# The final image holds only the published API on the ASP.NET Core runtime: no SDK, source code, tests or secrets.
# Settings and secrets come from the container's environment (docs/architecture.md section 4).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Restore first, so the package layer is reused while only the code changes.
COPY SmartEstimatePlatform.Api/SmartEstimatePlatform.Api.csproj SmartEstimatePlatform.Api/
RUN dotnet restore SmartEstimatePlatform.Api/SmartEstimatePlatform.Api.csproj
COPY SmartEstimatePlatform.Api/ SmartEstimatePlatform.Api/
RUN dotnet publish SmartEstimatePlatform.Api/SmartEstimatePlatform.Api.csproj \
    --configuration Release --no-restore --output /out/app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /out/app .
# The commit the image was built from, reported by GET /health.
ARG APP_VERSION=dev
ENV APP_VERSION=$APP_VERSION \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# The non-root "app" user that the official .NET images provide.
USER $APP_UID
ENTRYPOINT ["dotnet", "SmartEstimatePlatform.Api.dll"]
