# FoodDelivery

[![CI](https://github.com/Cheslawik/FoodDelivery/actions/workflows/ci.yml/badge.svg)](https://github.com/Cheslawik/FoodDelivery/actions/workflows/ci.yml)

REST API for the customer side of a food delivery service: browse the menu, fill a cart and place an
order for delivery as soon as possible or for a chosen time. A small React client in
`FoodDelivery.Frontend` uses the API.

Built with **.NET 10**, **ASP.NET Core**, **EF Core** and **SQL Server**, following Clean Architecture.

## Features

- **Menu:** public product list with paging (up to 100 per page), category filter and text search,
  plus product details. Products switched off by `IsAvailable` are hidden.
- **Cart:** add, change and remove items (1-50 per product). Adding a product that is already in the
  cart increases its quantity. The response includes line totals and the cart total.
- **Orders:** checkout turns the cart into an order and empties the cart. Delivery is either
  ASAP or scheduled; a scheduled time must be in the future. Order lines keep the product
  name and price at the time of ordering. Checkout fails if a product has become unavailable.
  Users see only their own order history and details.
- **Accounts:** registration and login with a JWT access token and a refresh token. Refresh tokens
  are stored hashed, rotated on every refresh and can be revoked. Email and phone number are unique.
- **Ownership checks:** a user can only read or change their own cart items and orders; anything else
  returns 404.
- Consistent error responses ([RFC 9457 problem details](https://www.rfc-editor.org/rfc/rfc9457)):
  400 for validation and business rules, 401, 404, 409 for duplicates, 429 when rate-limited.
- Validation with FluentValidation, URL API versioning (`/api/v1/...`), Swagger UI with JWT support
  (Development only), Serilog request logging, a `/health` endpoint, rate limiting
  (100 requests per minute per user or IP).
- Demo seed data: 15 products in 6 categories and two users.
- Docker + docker-compose, GitHub Actions CI, 57 unit and integration tests.

Tokens carry a `Customer` or `Admin` role. All current endpoints accept both roles; there are no
admin-only endpoints yet.

## Architecture

```
FoodDelivery.Domain          Entities (User, Product, Category, Cart, Order, RefreshToken), enums. No dependencies.
FoodDelivery.Application     One handler per command or query, DTOs and projections, validators,
                             application exceptions. Talks to the database through IApplicationDbContext.
FoodDelivery.Infrastructure  EF Core DbContext, SQL Server migrations, seed data, JWT, refresh tokens,
                             password hashing, current user.
FoodDelivery.WebApi          Controllers, problem-details exception handler, validation filter,
                             auth, versioning, Swagger, rate limiting.
FoodDelivery.Tests           xUnit: handler tests on in-memory SQLite and API tests with WebApplicationFactory.
FoodDelivery.Frontend        React + TypeScript + Vite client.
```

Dependencies point inwards: WebApi → Application → Domain, and Infrastructure implements the
interfaces declared in Application. Commands and queries are separate handler classes registered in
DI, without MediatR. Mapping is plain LINQ projections that EF Core translates to SQL, without
AutoMapper.

## Run with Docker

```bash
docker compose up --build
```

Swagger UI: http://localhost:8080/swagger. The API waits for SQL Server to become healthy, applies
migrations and seeds the demo data on startup.

## Run locally

Requires the .NET 10 SDK and a SQL Server instance. The simplest way is to start only the database
from compose:

```bash
docker compose up -d fooddelivery.sqlserver
dotnet run --project FoodDelivery.WebApi
```

Swagger UI: http://localhost:5197/swagger. `appsettings.Development.json` holds the local connection
string and a development-only JWT key.

Frontend (proxies `/api` to `http://localhost:5197`):

```bash
cd FoodDelivery.Frontend
npm install
npm run dev
```

Open http://localhost:5173.

### Demo users

| Email | Password | Role |
|---|---|---|
| `customer@fooddelivery.local` | `Password123!` | Customer |
| `admin@fooddelivery.local` | `Password123!` | Admin |

### Configuration

Outside Development, set these as environment variables (or user-secrets):

| Setting | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string (required) |
| `Jwt__Key` | HMAC-SHA256 signing key, at least 32 bytes (checked at startup) |
| `Jwt__Issuer`, `Jwt__Audience` | Token issuer and audience (defaults in `appsettings.json`) |
| `Database__ApplyMigrationsOnStartup` | `true` by default; set `false` to run migrations separately |

## Tests

```bash
dotnet test FoodDelivery.slnx
```

- **Handler tests** run against in-memory SQLite, which enforces unique indexes and foreign keys,
  unlike the EF Core InMemory provider. They cover cart rules, checkout (totals, price snapshot,
  unavailable products), menu paging and filters, registration conflicts, login and refresh-token
  rotation and revocation.
- **API tests** host the real application with `WebApplicationFactory` (SQL Server swapped for SQLite)
  and check status codes, problem-details bodies, the full cart-to-order flow, access to another
  user's cart and orders, and rate limiting.

CI (`.github/workflows/ci.yml`) runs the tests, fails on vulnerable NuGet packages, checks that the
EF model has no changes without a migration, applies all migrations to a real SQL Server container,
lints and builds the frontend, and builds the Docker image.

## API

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/v1/menu?pageNumber=&pageSize=&category=&search=` | - | Paged product list |
| `GET` | `/api/v1/menu/{id}` | - | Product details |
| `GET` | `/api/v1/cart` | JWT | Current cart with totals |
| `POST` | `/api/v1/cart/items` | JWT | Add a product `{ productId, quantity }` |
| `PUT` | `/api/v1/cart/items/{id}` | JWT | Change quantity `{ quantity }` |
| `DELETE` | `/api/v1/cart/items/{id}` | JWT | Remove an item |
| `POST` | `/api/v1/orders` | JWT | Check out the cart |
| `GET` | `/api/v1/orders` | JWT | Order history, newest first |
| `GET` | `/api/v1/orders/{id}` | JWT | Order details |
| `POST` | `/api/v1/auth/register` | - | Register, returns access and refresh tokens |
| `POST` | `/api/v1/auth/login` | - | Log in, returns access and refresh tokens |
| `POST` | `/api/v1/auth/refresh` | - | Exchange a refresh token for a new pair |
| `POST` | `/api/v1/auth/revoke` | - | Revoke a refresh token |
| `GET` | `/api/v1/auth/me` | JWT | Current user from the token |
| `GET` | `/health` | - | Liveness check |

Checkout request:

```json
POST /api/v1/orders
{
  "contactName": "Jan Kowalski",
  "contactPhone": "+48123456789",
  "deliveryAddress": "ul. Marszałkowska 1, Warszawa",
  "isAsap": false,
  "scheduledDeliveryTimeUtc": "2026-12-01T18:00:00Z"
}
```

Error example (checkout with an empty cart):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Request cannot be completed",
  "status": 400,
  "detail": "Cart is empty.",
  "traceId": "00-d6396cf1b341c8c8ae5efb1a23cbb1e0-050e11fb42c74e9b-00"
}
```

## EF Core migrations

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project FoodDelivery.Infrastructure --startup-project FoodDelivery.WebApi --output-dir Persistence/Migrations
dotnet ef database update --project FoodDelivery.Infrastructure --startup-project FoodDelivery.WebApi
```

## Possible next steps

- Admin endpoints for managing the menu and order statuses.
- Integration tests against SQL Server with Testcontainers (today the tests use SQLite).
- Caching the menu.
