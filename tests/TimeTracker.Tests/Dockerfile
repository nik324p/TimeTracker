# One-shot unit-test container: runs xUnit against TimeTracker.Core and exits non-zero on any failure.
# Core-only tests — no database or broker needed.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["tests/TimeTracker.Tests/TimeTracker.Tests.csproj", "tests/TimeTracker.Tests/"]
COPY ["src/TimeTracker.Core/TimeTracker.Core.csproj", "src/TimeTracker.Core/"]
RUN dotnet restore "tests/TimeTracker.Tests/TimeTracker.Tests.csproj"

COPY . .
ENTRYPOINT ["dotnet", "test", "tests/TimeTracker.Tests/TimeTracker.Tests.csproj", \
            "-c", "Release", "--no-restore"]
