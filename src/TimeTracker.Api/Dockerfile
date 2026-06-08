# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the csproj graph first for layer caching.
COPY ["src/TimeTracker.Api/TimeTracker.Api.csproj", "src/TimeTracker.Api/"]
COPY ["src/TimeTracker.Core/TimeTracker.Core.csproj", "src/TimeTracker.Core/"]
COPY ["src/TimeTracker.Infrastructure/TimeTracker.Infrastructure.csproj", "src/TimeTracker.Infrastructure/"]
RUN dotnet restore "src/TimeTracker.Api/TimeTracker.Api.csproj"

COPY . .
RUN dotnet publish "src/TimeTracker.Api/TimeTracker.Api.csproj" -c Release -o /app/publish --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is used by the docker-compose healthcheck.
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TimeTracker.Api.dll"]
