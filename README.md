# FoodDelivery Front-Office SaaS (.NET 10)

REST API для фронт-офиса сервиса доставки еды с архитектурой `Clean Architecture`:
- `FoodDelivery.Domain`
- `FoodDelivery.Application`
- `FoodDelivery.Infrastructure`
- `FoodDelivery.WebApi`
- `FoodDelivery.Tests`

## Реализовано
- Меню: список блюд с пагинацией/фильтрацией + карточка блюда.
- Корзина: добавление/обновление/удаление позиций, расчёт стоимости.
- Заказы: оформление (ASAP или scheduled), история заказов, детализация.
- JWT auth: регистрация/логин, bearer-токен, refresh token rotation/revoke, role-based доступ (`Customer`, `Admin`).
- API versioning: `/api/v1/...`.
- Swagger (Swashbuckle).
- Global exception middleware.
- FluentValidation.
- Serilog logging.
- EF Core + SQL Server.
- Health checks (`/health`).
- Rate limiting.
- Seed demo данных.
- Docker + docker-compose.

## Архитектура и подходы
- SOLID.
- Clean Architecture / Onion.
- CQRS (через отдельные Query/Command handlers).
- Repository/Unit of Work через абстракцию `IApplicationDbContext` + `IUnitOfWork`.
- DTO + AutoMapper.
- Dependency Injection (built-in container).

## Запуск локально
1. Убедиться, что доступен SQL Server (локально или в Docker).
2. Обновить строку подключения в `FoodDelivery.WebApi/appsettings.Development.json`.
3. Выполнить:
   ```bash
   dotnet restore FoodDelivery.slnx
   dotnet build FoodDelivery.slnx
   dotnet run --project FoodDelivery.WebApi/FoodDelivery.WebApi.csproj
   ```
4. Swagger: `http://localhost:5000/swagger` (порт зависит от launch settings).

### Auth flow
- `POST /api/v1/auth/register` -> регистрация и выдача `accessToken + refreshToken` (роль `Customer`)
- `POST /api/v1/auth/login` -> логин и выдача `accessToken + refreshToken`
- `POST /api/v1/auth/refresh` -> ротация refresh-токена и выпуск новой пары токенов
- `POST /api/v1/auth/revoke` -> отзыв refresh-токена
- `GET /api/v1/auth/me` -> информация о текущем пользователе (требует access token)
- В Swagger нажмите `Authorize` и вставьте: `Bearer <token>`

### Seed users
- `customer@fooddelivery.local / Password123!` (`Customer`)
- `admin@fooddelivery.local / Password123!` (`Admin`)

## Запуск через Docker
```bash
docker compose up --build
```
API: `http://localhost:8080/swagger`

## Основные endpoint'ы
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
1. Создать миграцию:
   ```bash
   dotnet ef migrations add AddRefreshTokensAndRoles --project FoodDelivery.Infrastructure/FoodDelivery.Infrastructure.csproj --startup-project FoodDelivery.WebApi/FoodDelivery.WebApi.csproj --output-dir Persistence/Migrations
   ```
2. Применить миграции:
   ```bash
   dotnet ef database update --project FoodDelivery.Infrastructure/FoodDelivery.Infrastructure.csproj --startup-project FoodDelivery.WebApi/FoodDelivery.WebApi.csproj --configuration Release
   ```

## Agile: user stories
- Как пользователь, я хочу видеть меню и фильтровать блюда, чтобы быстро выбрать еду.
- Как пользователь, я хочу добавлять блюда в корзину и менять количество.
- Как пользователь, я хочу оформить заказ на ближайшее время или конкретный слот.
- Как пользователь, я хочу просматривать историю заказов и детали каждого заказа.

## Agile: спринты
- Sprint 1: Foundation + Domain + Infrastructure + базовый API.
- Sprint 2: Menu + Cart функционал.
- Sprint 3: Orders + history + observability + hardening.

## Backlog
- Redis cache для меню.
- Hangfire jobs (статусы заказа, уведомления).
- Интеграционные тесты (Testcontainers).
- CI/CD pipeline + quality gates.

## Примечание
Если `dotnet restore` не проходит, проверьте доступ к `https://api.nuget.org/v3/index.json` и сетевые ограничения среды.
