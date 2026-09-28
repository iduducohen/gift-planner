# Gift Planner API

ASP.NET Core API for planning gifts: recipients, occasions, idea recommendations, saved ideas, and surprise plans. The first launch is anonymous. Google and Apple sign-in merge that anonymous data into a real account.

Requires the .NET 10 SDK. PostgreSQL is required at runtime. Redis is optional.

## Layout

| Project | Role |
| --- | --- |
| `GiftPlanner.Domain` | Entities and invariants. No infrastructure dependencies. |
| `GiftPlanner.Application` | Use cases and the recommendation engine. Depends only on Domain. |
| `GiftPlanner.Infrastructure` | EF Core, PostgreSQL, Redis, JWT, and the Anthropic client. |
| `GiftPlanner.Api` | HTTP endpoints, auth, rate limits, and health checks. |
| `GiftPlanner.Application.Tests` | xUnit tests for scoring, filters, refine, and clarifications. |

## Run locally

PostgreSQL must be reachable at the development connection string (`gift` / `gift` on `localhost:5432`, database `giftplanner`). On startup the API creates the schema and seeds the demo catalog when the catalog is empty.

```bash
dotnet run --project src/GiftPlanner.Api
```

`appsettings.Development.json` supplies a dev-only JWT signing key and enables the `dev` identity provider. The API listens on `http://localhost:5000` unless `ASPNETCORE_URLS` is set.

- Health: `GET /health`
- OpenAPI (Development only): `GET /openapi/v1.json`

Anonymous session:

```bash
curl -s -X POST http://localhost:5000/api/v1/auth/anonymous
```

Dev sign-in (Development only). The token format is `dev:<id>|<display name>`:

```bash
curl -s -X POST http://localhost:5000/api/v1/auth/sign-in \
  -H "Content-Type: application/json" \
  -d "{\"provider\":\"dev\",\"idToken\":\"dev:local-user|Dudu\"}"
```

Send the returned token as `Authorization: Bearer <token>` on every other route except taxonomy.

## Configuration

Set secrets with environment variables (`Jwt__SigningKey`) or user secrets (`UserSecretsId` is `gift-planner-api`). The process refuses to start when `Jwt:SigningKey` is shorter than 32 bytes. Do not put a production signing key in source.

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:Postgres` | PostgreSQL connection string. |
| `ConnectionStrings:Redis` | Optional. Empty uses an in-memory distributed cache. |
| `Jwt:SigningKey` | HMAC key, at least 32 bytes. |
| `Jwt:AnonymousDays` / `Jwt:SignedInDays` | Token lifetime. Defaults: 180 and 30. |
| `Llm:ApiKey` | Anthropic key. Idea generation falls back to the catalog when this is empty. |
| `Llm:FastModel` / `Llm:SmartModel` | Model ids. Defaults: `claude-haiku-4-5` and `claude-sonnet-4-5`. |
| `Auth:GoogleClientId` / `Auth:AppleClientId` | OIDC audiences. A provider is skipped when its client id is empty. |
| `Auth:EnableDevProvider` | Accepts `dev:` tokens. On in Development only. |
| `Cors:Origins` | Allowed browser origins. |
| `OpenTelemetry:OtlpEndpoint` | OTLP export URL. Tracing and metrics stay off when empty. |
| `Clock:TimeZone` | Occasion dates. Default: `Asia/Jerusalem`. |

## HTTP API

All routes are under `/api/v1`. Authorized routes are scoped to the caller, so an id in the URL cannot read another user's data.

| Method | Path | Auth |
| --- | --- | --- |
| `POST` | `/auth/anonymous` | No |
| `POST` | `/auth/sign-in` | No. Body: `{ "provider", "idToken" }`. An anonymous bearer token merges that user's data into the signed-in account. |
| `GET` | `/taxonomy/relationships`, `/occasions`, `/age-ranges`, `/interests` | No. Cached for one hour. |
| `POST` | `/gift-sessions` | Yes |
| `POST` | `/gift-sessions/{id}/clarifications` | Yes |
| `POST` | `/gift-sessions/{id}/recipient` | Yes |
| `POST` | `/gift-sessions/{id}/idea-sets` | Yes. Rate limited. |
| `GET` | `/idea-sets/{id}`, `/idea-sets/{id}/ideas` | Yes. Ideas accept `cursor`, `limit`, and `filter`. |
| `GET` | `/refine-options`, `/ideas/{id}` | Yes |
| `PUT` / `DELETE` | `/ideas/{id}/saved` | Yes |
| `POST` | `/ideas/{id}/plan`, `/ideas/{id}/purchase-intents` | Yes |
| `GET` / `POST` | `/recipients` | Yes |
| `GET` / `PUT` / `DELETE` | `/recipients/{id}` | Yes |
| `POST` | `/recipients/{id}/occasions`, `/recipients/{id}/gift-history` | Yes |
| `DELETE` | `/recipients/{id}/occasions/{occasionId}` | Yes |
| `GET` | `/saved` | Yes. Optional `recipientId`. |
| `GET` | `/plans`, `/plans/{id}` | Yes |
| `PATCH` | `/plans/{id}/tasks/{taskId}` | Yes |
| `GET` | `/me/export` | Yes |
| `DELETE` | `/me` | Yes |

Auth routes are limited to 20 requests per minute per IP. Idea generation is limited to 10 requests per 10 minutes for anonymous users and 30 for signed-in users, keyed by user id.

## Build and test

```bash
dotnet build GiftPlanner.slnx
dotnet test GiftPlanner.slnx
```

## Docker

The image publishes `GiftPlanner.Api` and listens on port 8080 as a non-root user. Pass `Jwt__SigningKey` and `ConnectionStrings__Postgres` at run time.

```bash
docker build -t gift-planner-api .
docker run --rm -p 8080:8080 \
  -e Jwt__SigningKey="<32+ byte secret>" \
  -e ConnectionStrings__Postgres="Host=host.docker.internal;Port=5432;Database=giftplanner;Username=gift;Password=gift" \
  gift-planner-api
```
