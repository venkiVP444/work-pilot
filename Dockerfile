# WorkPilot AI - Google Cloud Run Multi-Stage Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV PORT=8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy NuGet configuration & project files for layer caching
COPY NuGet.Config ./
COPY WorkPilot.sln ./
COPY src/WorkPilot.Domain/WorkPilot.Domain.csproj src/WorkPilot.Domain/
COPY src/WorkPilot.Application/WorkPilot.Application.csproj src/WorkPilot.Application/
COPY src/WorkPilot.Infrastructure/WorkPilot.Infrastructure.csproj src/WorkPilot.Infrastructure/
COPY src/WorkPilot.Api/WorkPilot.Api.csproj src/WorkPilot.Api/

RUN dotnet restore src/WorkPilot.Api/WorkPilot.Api.csproj --source https://api.nuget.org/v3/index.json

# Copy full source and publish
COPY src/ ./src/
WORKDIR /src/src/WorkPilot.Api
RUN dotnet publish WorkPilot.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:${PORT}/api/health || exit 1

ENTRYPOINT ["dotnet", "WorkPilot.Api.dll"]
