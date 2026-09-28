# Running Gift Planner with Tests

This guide explains how to run the Gift Planner application with Docker Compose and execute tests.

## Quick Start

### Run All Services (Development)

```bash
# Set the JWT signing key and start all services
JWT_SIGNING_KEY=$(openssl rand -hex 32) docker compose up --build

# On Windows (PowerShell):
$env:JWT_SIGNING_KEY = -join ((1..64) | % {[char][byte]((0..255) | Get-Random)}) | ConvertTo-Base64String
docker compose up --build
```

Then open http://localhost:8100 to access the application.

Services:
- **PostgreSQL**: localhost:5432 (user: gift, password: gift)
- **Redis**: localhost:6379
- **API**: http://localhost:5080/api/v1
- **Client**: http://localhost:8100

## Running Tests

### Option 1: Run Tests Locally (Recommended for Development)

#### Client Unit Tests

```bash
cd client
npm install
npm test                # Run once (CI mode)
npm run test:watch     # Run with file watching
```

#### Server Unit Tests

```bash
cd server
dotnet restore
dotnet test tests/GiftPlanner.Application.Tests/GiftPlanner.Application.Tests.csproj
```

#### E2E Tests

First, ensure services are running:
```bash
JWT_SIGNING_KEY=$(openssl rand -hex 32) docker compose up --build
```

Then in another terminal:
```bash
cd e2e
npm install
npm test                  # Run all e2e tests
npm run test:headed       # Run with visible browser
npm run test:debug        # Run in debug mode
npm run test:ui           # Run with interactive UI
```

### Option 2: Run Tests in Docker

#### Run Client Tests in Docker

```bash
# Build client image
docker compose build client

# Run tests
docker compose run --rm client npm test
```

#### Run Server Tests in Docker

```bash
# Build server image
docker compose build api

# Run server tests
docker compose run --rm api dotnet test tests/GiftPlanner.Application.Tests/GiftPlanner.Application.Tests.csproj
```

#### Run All Tests with Compose

Use the test compose file:

```bash
JWT_SIGNING_KEY=$(openssl rand -hex 32) docker compose -f docker-compose.yml -f docker-compose.test.yml up --build
```

This will:
1. Start PostgreSQL and Redis
2. Run server unit tests
3. Build and run client unit tests
4. Run E2E tests
5. Generate test reports

## Test Structure

### Client Unit Tests

Located in `client/src/app/**/*.spec.ts`

- **api.service.spec.ts** - HTTP client methods and endpoints
- **format.spec.ts** - Formatting utilities and date functions

Run with coverage:
```bash
cd client
npm test -- --code-coverage
# Report in client/coverage/
```

### Server Unit Tests

Located in `server/tests/GiftPlanner.Application.Tests/`

- **EngineTests.cs** - Comprehensive tests for:
  - Filter logic (budget, history, lead time, audience)
  - Scoring algorithms
  - Diversity reranking
  - Clarification strategies
  - LLM output parsing
  - Date/occasion calculations

Run with coverage:
```bash
cd server
dotnet test tests/GiftPlanner.Application.Tests/GiftPlanner.Application.Tests.csproj \
  /p:CollectCoverage=true \
  /p:CoverageFormat=lcov \
  /p:CoverageDirectory=coverage
```

### E2E Tests

Located in `e2e/tests/**/*.spec.ts`

- **auth.spec.ts** - Authentication and navigation flows
- **ui.spec.ts** - UI responsiveness and error handling

## Configuration

### Docker Compose Files

- **docker-compose.yml** - Development stack (default)
- **docker-compose.test.yml** - Test stack (override with `-f`)

### Environment Variables

Set before running services:

- `JWT_SIGNING_KEY` - Required, 32+ characters for JWT signing
- `POSTGRES_PASSWORD` - Optional, defaults to "gift"
- `ANTHROPIC_API_KEY` - Optional, for LLM features
- `CI` - Set to "true" for CI environments

### Service Ports

| Service | Port | Purpose |
|---------|------|---------|
| PostgreSQL | 5432 | Database |
| Redis | 6379 | Caching |
| API | 5080 | REST API |
| Client | 8100 | Web UI |

## Development Workflow

### Making Changes

1. **Client Changes**
   ```bash
   cd client
   npm run start        # Dev server with hot reload
   npm test             # Run tests
   ```

2. **Server Changes**
   ```bash
   cd server
   dotnet run --project src/GiftPlanner.Api/
   dotnet test tests/GiftPlanner.Application.Tests/
   ```

3. **E2E Tests**
   ```bash
   cd e2e
   npm run test:headed  # Visual test execution
   ```

### Adding Tests

**Client Test Example:**
```typescript
// src/app/services/my.service.spec.ts
import { TestBed } from '@angular/core/testing';
import { MyService } from './my.service';

describe('MyService', () => {
  let service: MyService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MyService);
  });

  it('should have a method', () => {
    expect(service.myMethod()).toBeDefined();
  });
});
```

**Server Test Example:**
```csharp
// tests/GiftPlanner.Application.Tests/MyTests.cs
public class MyTests
{
    [Fact]
    public void ShouldDoSomething()
    {
        var result = MyClass.DoWork();
        Assert.NotNull(result);
    }
}
```

**E2E Test Example:**
```typescript
// e2e/tests/my-feature.spec.ts
test('should complete user workflow', async ({ page }) => {
  await page.goto('/home');
  await page.click('button:has-text("Start")');
  await expect(page).toHaveTitle(/Results/);
});
```

## Troubleshooting

### "Port 8100 is already in use"

```bash
# Kill the process using the port
lsof -i :8100 | grep -v COMMAND | awk '{print $2}' | xargs kill -9

# Or change the port in docker-compose.yml
```

### "Database connection failed"

```bash
# Check PostgreSQL is running
docker compose logs postgres

# Verify database exists
docker compose exec postgres psql -U gift -d giftplanner -c "\dt"

# Reset database
docker compose down -v    # Remove volumes
docker compose up --build
```

### "npm ERR! ERESOLVE unable to resolve dependency tree"

```bash
cd client
rm -rf node_modules package-lock.json
npm install
```

### "dotnet: command not found"

Install .NET SDK:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)

## CI/CD Integration

For GitHub Actions or similar CI/CD:

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:17-alpine
        env:
          POSTGRES_DB: giftplanner_test
          POSTGRES_USER: gift
          POSTGRES_PASSWORD: gift
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Run client tests
        run: |
          cd client
          npm install
          npm test
      
      - name: Run server tests
        run: |
          cd server
          dotnet test tests/GiftPlanner.Application.Tests/
      
      - name: Run e2e tests
        run: |
          cd e2e
          npm install
          npm test
```

## Additional Resources

See [TESTING.md](./TESTING.md) for detailed test documentation.
