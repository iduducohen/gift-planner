# Testing Guide

This document describes how to run tests for the Gift Planner application.

## Overview

The project includes three levels of testing:

1. **Unit Tests** - Test individual services and utilities
   - Client: Angular unit tests using Jasmine/Karma
   - Server: C# xUnit tests

2. **End-to-End Tests** - Test complete user workflows
   - Playwright tests that simulate real user interactions

## Prerequisites

- Docker and Docker Compose installed
- For local testing: Node.js 24+ and .NET SDK 10.0+

## Running Tests Locally

### Client Unit Tests

```bash
cd client
npm install
npm test                    # Run once in CI mode
npm run test:watch         # Run with watch mode
```

### Server Unit Tests

```bash
cd server
dotnet restore
dotnet test tests/GiftPlanner.Application.Tests/GiftPlanner.Application.Tests.csproj
```

### E2E Tests (requires services running)

```bash
# Terminal 1: Start services
JWT_SIGNING_KEY=$(openssl rand -hex 32) docker compose up --build

# Terminal 2: Run e2e tests
cd e2e
npm install
npm test
```

## Running Tests with Docker Compose

### Run All Services

```bash
JWT_SIGNING_KEY=$(openssl rand -hex 32) docker compose up --build
```

Then open http://localhost:8100 to see the application.

### Run Unit Tests Only

**Client tests:**
```bash
docker compose build client
docker compose run --rm client npm test
```

**Server tests:**
```bash
docker compose build api
docker compose run --rm api dotnet test tests/GiftPlanner.Application.Tests/GiftPlanner.Application.Tests.csproj
```

### Run All Tests (Unit + E2E)

```bash
JWT_SIGNING_KEY=$(openssl rand -hex 32) docker compose -f docker-compose.yml -f docker-compose.test.yml up --build
```

## Test Coverage

### Client Unit Tests

Coverage includes:

- **API Service** (`api.service.spec.ts`) - HTTP client methods
- **Format Utilities** (`format.spec.ts`) - Formatting and conversion functions

To generate a coverage report:

```bash
cd client
npm test -- --code-coverage
# Report will be in client/coverage/
```

### Server Unit Tests

Coverage includes:

- **Filter Tests** - Budget, history, lead time, and audience filters
- **Scoring Tests** - Interest and emotion matching
- **Diversity Tests** - Gift diversity reranking
- **Refine Tests** - Gift refinement strategies
- **Clarification Tests** - User feedback interpretation
- **Parsing Tests** - LLM output parsing and validation
- **Date Utilities** - Occasion and date calculations

Run with coverage:

```bash
cd server
dotnet test tests/GiftPlanner.Application.Tests/GiftPlanner.Application.Tests.csproj \
  /p:CollectCoverage=true \
  /p:CoverageFormat=lcov \
  /p:CoverageDirectory=coverage
```

### E2E Tests

Coverage includes:

- **Authentication** - Login flow and auth state
- **UI Responsiveness** - Mobile, tablet, desktop viewports
- **Navigation** - Page transitions and routing
- **Error Handling** - Console errors and uncaught exceptions

View test results:

```bash
cd e2e
npm run test:report
```

## CI/CD Integration

Tests are run automatically in CI/CD pipelines. Configure your CI/CD to:

1. Run client unit tests: `npm test`
2. Run server unit tests: `dotnet test`
3. Run e2e tests after building: `npm test`

## Debugging Tests

### Client Tests

```bash
cd client
npm run test:watch
```

Then open the Karma runner in your browser at http://localhost:9876

### E2E Tests

Run tests with headed browser to see what's happening:

```bash
cd e2e
npm run test:headed
```

Or use the debug mode:

```bash
npm run test:debug
```

Or use the interactive UI mode:

```bash
npm run test:ui
```

## Writing New Tests

### Client Unit Tests

Create a `.spec.ts` file alongside your service:

```typescript
import { TestBed } from '@angular/core/testing';
import { MyService } from './my.service';

describe('MyService', () => {
  let service: MyService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MyService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should do something', () => {
    expect(service.doSomething()).toEqual(expected);
  });
});
```

### Server Unit Tests

Add tests to the test project:

```csharp
public class MyTests
{
    [Fact]
    public void ShouldDoSomething()
    {
        // Arrange
        var input = new Input();
        
        // Act
        var result = MyClass.DoSomething(input);
        
        // Assert
        Assert.NotNull(result);
    }
}
```

### E2E Tests

Add Playwright tests:

```typescript
import { test, expect } from '@playwright/test';

test.describe('My Feature', () => {
  test('should work correctly', async ({ page }) => {
    await page.goto('/my-page');
    
    const element = page.locator('[data-testid="my-element"]');
    await expect(element).toBeVisible();
  });
});
```

## Troubleshooting

### Docker Build Fails

Ensure you have the latest base images:

```bash
docker compose build --pull
```

### Tests Timeout

Increase the timeout in the respective config:

- Client: `karma.conf.js` - `browserNoActivityTimeout`
- E2E: `playwright.config.ts` - `timeout`

### Port Conflicts

If ports are already in use:

```bash
# Check what's using the port
lsof -i :8100
# Change ports in docker-compose.yml
```

### Database Connection Issues

Ensure PostgreSQL is running and initialized:

```bash
docker compose logs postgres
docker compose exec postgres psql -U gift -d giftplanner -c "\dt"
```

## Resources

- [Angular Testing Guide](https://angular.io/guide/testing)
- [Jasmine Documentation](https://jasmine.github.io/)
- [Karma Test Runner](https://karma-runner.github.io/)
- [xUnit.net](https://xunit.net/)
- [Playwright Documentation](https://playwright.dev/)
