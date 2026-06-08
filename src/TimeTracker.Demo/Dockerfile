# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the csproj first for layer caching (Demo has no project references).
COPY ["src/TimeTracker.Demo/TimeTracker.Demo.csproj", "src/TimeTracker.Demo/"]
RUN dotnet restore "src/TimeTracker.Demo/TimeTracker.Demo.csproj"

COPY . .
RUN dotnet publish "src/TimeTracker.Demo/TimeTracker.Demo.csproj" -c Release -o /app/publish

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TimeTracker.Demo.dll"]
