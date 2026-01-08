FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY src/backend/TaskSystem.sln ./
COPY src/backend/TaskSystem.Api/TaskSystem.Api.csproj ./TaskSystem.Api/
COPY src/backend/TaskSystem.Shared/TaskSystem.Shared.csproj ./TaskSystem.Shared/

# Restore dependencies
RUN dotnet restore TaskSystem.Api/TaskSystem.Api.csproj

# Copy source code
COPY src/backend/TaskSystem.Api ./TaskSystem.Api/
COPY src/backend/TaskSystem.Shared ./TaskSystem.Shared/

# Build and publish
WORKDIR /src/TaskSystem.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "TaskSystem.Api.dll"]

