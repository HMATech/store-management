# Builds the Web.Api image. Run from the repo root: docker build -t store-management-api .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, from the project files alone, so the layer is cached until a dependency changes.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Shared/Shared.csproj src/Shared/
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Web.Api/Web.Api.csproj src/Web.Api/
RUN dotnet restore src/Web.Api/Web.Api.csproj

COPY src/ src/
RUN dotnet publish src/Web.Api/Web.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

# The base image listens on 8080 and provides a non-root user.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Web.Api.dll"]
