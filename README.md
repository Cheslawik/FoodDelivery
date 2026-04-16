# FoodDelivery Front-Office SaaS (.NET 10)

REST API for a food delivery front-office service built with `Clean Architecture`:
- `FoodDelivery.Domain`
- `FoodDelivery.Application`
- `FoodDelivery.Infrastructure`
- `FoodDelivery.WebApi`
- `FoodDelivery.Tests`

## Implemented
- Menu: product list with pagination/filtering + product details.
- Cart: add/update/remove items, total price calculation.
- Orders: checkout (ASAP or scheduled), order history, details.
- JWT auth: register/login, bearer token, refresh token rotation/revoke, role-based access (`Customer`, `Admin`).
- API versioning: `/api/v1/...`.
- Swagger (Swashbuckle).
- Global exception middleware.
- FluentValidation.
- Serilog logging.
- EF Core + SQL Server.
- Health checks (`/health`).
- Rate limiting.
- Demo seed data.
- Docker + docker-compose.

## Architecture and Approaches
- SOLID.
- Clean Architecture / Onion.
- CQRS (via separate Query/Command handlers).
- Repository/Unit of Work through `IApplicationDbContext` + `IUnitOfWork` abstractions.
- DTO + AutoMapper.
- Dependency Injection (built-in container).

## Run Locally
1. Ensure SQL Server is available (locally or in Docker).
2. Update the connection string in `FoodDelivery.WebApi/appsettings.Development.json`.
3. Run:
   ```bash
   dotnet restore FoodDelivery.slnx
   dotnet build FoodDelivery.slnx
   dotnet run --project FoodDelivery.WebApi/FoodDelivery.WebApi.csproj
   ```
4. Swagger: `http://localhost:5197/swagger` (based on launch settings).

### Auth flow
- `POST /api/v1/auth/register` -> register and issue `accessToken + refreshToken` (`Customer` role)
- `POST /api/v1/auth/login` -> login and issue `accessToken + refreshToken`
- `POST /api/v1/auth/refresh` -> rotate refresh token and issue a new token pair
- `POST /api/v1/auth/revoke` -> revoke refresh token
- `GET /api/v1/auth/me` -> current user info (requires access token)
- In Swagger click `Authorize` and paste: `Bearer <token>`

### Seed users
- `customer@fooddelivery.local / Password123!` (`Customer`)
- `admin@fooddelivery.local / Password123!` (`Admin`)

## Run with Docker
```bash
docker compose up --build
```
API: `http://localhost:8080/swagger`

## Main endpoints
- `GET /api/v1/menu`
- `GET /api/v1/menu/{id}`
- `GET /api/v1/cart`
- `POST /api/v1/cart/items`
- `PUT /api/v1/cart/items/{id}`
- `DELETE /api/v1/cart/items/{id}`
- `POST /api/v1/orders`
- `GET /api/v1/orders`
- `GET /api/v1/orders/{id}`
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/revoke`
- `GET /api/v1/auth/me`

## EF Core migrations workflow
1. Create a migration:
   ```bash
   dotnet ef migrations add AddRefreshTokensAndRoles --project FoodDelivery.Infrastructure/FoodDelivery.Infrastructure.csproj --startup-project FoodDelivery.WebApi/FoodDelivery.WebApi.csproj --output-dir Persistence/Migrations
   ```
2. Apply migrations:
   ```bash
   dotnet ef database update --project FoodDelivery.Infrastructure/FoodDelivery.Infrastructure.csproj --startup-project FoodDelivery.WebApi/FoodDelivery.WebApi.csproj --configuration Release
   ```

## Agile: user stories
- As a user, I want to browse and filter the menu to find food quickly.
- As a user, I want to add items to cart and adjust quantities.
- As a user, I want to place an order ASAP or for a scheduled slot.
- As a user, I want to review order history and details.

## Agile: sprints
- Sprint 1: Foundation + Domain + Infrastructure + base API.
- Sprint 2: Menu + Cart functionality.
- Sprint 3: Orders + history + observability + hardening.

## Backlog
- Redis cache for menu.
- Hangfire jobs (order statuses, notifications).
- Integration tests (Testcontainers).
- CI/CD pipeline + quality gates.

## Note
If `dotnet restore` fails, verify access to `https://api.nuget.org/v3/index.json` and environment network restrictions.
