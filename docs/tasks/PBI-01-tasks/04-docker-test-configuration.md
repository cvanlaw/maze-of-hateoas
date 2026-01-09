---
id: 4
title: Docker Test Configuration
depends_on: [3]
status: pending
---

# Task 4: Docker Test Configuration

## Description

Create the Docker configuration files that enable running all tests in containers per CLAUDE.md requirements. This establishes the TDD workflow foundation.

## Deliverables

- `Dockerfile.test` - Docker image for running tests
- `docker-compose.test.yml` - Orchestrates test execution
- `.gitignore` - Excludes build artifacts and test results

## Acceptance Criteria

- [ ] `Dockerfile.test` uses `mcr.microsoft.com/dotnet/sdk:8.0` as base image
- [ ] `Dockerfile.test` runs `dotnet test` with `--logger:trx` for test output
- [ ] `docker-compose.test.yml` mounts `./TestResults` volume for result extraction
- [ ] Running `docker compose -f docker-compose.test.yml up --build` executes tests and exits with code 0
- [ ] Test results are output to `./TestResults/` on host filesystem
- [ ] `.gitignore` excludes `bin/`, `obj/`, `TestResults/`

## Implementation Details

### Dockerfile.test

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY src/MazeOfHateoas.Domain/*.csproj src/MazeOfHateoas.Domain/
COPY src/MazeOfHateoas.Application/*.csproj src/MazeOfHateoas.Application/
COPY src/MazeOfHateoas.Infrastructure/*.csproj src/MazeOfHateoas.Infrastructure/
COPY src/MazeOfHateoas.Api/*.csproj src/MazeOfHateoas.Api/
COPY tests/MazeOfHateoas.UnitTests/*.csproj tests/MazeOfHateoas.UnitTests/
COPY tests/MazeOfHateoas.IntegrationTests/*.csproj tests/MazeOfHateoas.IntegrationTests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Run tests with trx logger
ENTRYPOINT ["dotnet", "test", "--no-restore", "--logger:trx", "--results-directory", "/TestResults"]
```

### docker-compose.test.yml

```yaml
version: '3.8'

services:
  test:
    build:
      context: .
      dockerfile: Dockerfile.test
    volumes:
      - ./TestResults:/TestResults
```

### .gitignore

```gitignore
# Build outputs
bin/
obj/

# Test results
TestResults/

# IDE
.vs/
.vscode/
*.user
```

## Testing Checklist

- [ ] Run `docker compose -f docker-compose.test.yml up --build`
- [ ] Verify exit code is 0: `echo $?`
- [ ] Verify `TestResults/` directory contains `.trx` files
- [ ] Verify `.gitignore` prevents committing build artifacts
