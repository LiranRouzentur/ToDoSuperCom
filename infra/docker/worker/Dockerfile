FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY src/backend/TaskSystem.sln ./
COPY src/backend/TaskSystem.Worker/TaskSystem.Worker.csproj ./TaskSystem.Worker/
COPY src/backend/TaskSystem.Shared/TaskSystem.Shared.csproj ./TaskSystem.Shared/

# Restore dependencies
RUN dotnet restore TaskSystem.Worker/TaskSystem.Worker.csproj

# Copy source code
COPY src/backend/TaskSystem.Worker ./TaskSystem.Worker/
COPY src/backend/TaskSystem.Shared ./TaskSystem.Shared/

# Build and publish
WORKDIR /src/TaskSystem.Worker
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TaskSystem.Worker.dll"]

