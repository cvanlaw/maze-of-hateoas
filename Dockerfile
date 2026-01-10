# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["src/MazeOfHateoas.Api/MazeOfHateoas.Api.csproj", "MazeOfHateoas.Api/"]
COPY ["src/MazeOfHateoas.Application/MazeOfHateoas.Application.csproj", "MazeOfHateoas.Application/"]
COPY ["src/MazeOfHateoas.Domain/MazeOfHateoas.Domain.csproj", "MazeOfHateoas.Domain/"]
COPY ["src/MazeOfHateoas.Infrastructure/MazeOfHateoas.Infrastructure.csproj", "MazeOfHateoas.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "MazeOfHateoas.Api/MazeOfHateoas.Api.csproj"

# Copy source code
COPY ["src/MazeOfHateoas.Api/", "MazeOfHateoas.Api/"]
COPY ["src/MazeOfHateoas.Application/", "MazeOfHateoas.Application/"]
COPY ["src/MazeOfHateoas.Domain/", "MazeOfHateoas.Domain/"]
COPY ["src/MazeOfHateoas.Infrastructure/", "MazeOfHateoas.Infrastructure/"]

# Build and publish
RUN dotnet publish "MazeOfHateoas.Api/MazeOfHateoas.Api.csproj" -c Release -o /app/publish

# Runtime stage - using Alpine for smaller image size
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apk add --no-cache curl

COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MazeOfHateoas.Api.dll"]
