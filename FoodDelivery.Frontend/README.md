# FoodDelivery Frontend (React)

Frontend-приложение для API `FoodDelivery.WebApi` на React + TypeScript + Vite.

## Функции
- Регистрация и логин (`/api/v1/auth/register`, `/api/v1/auth/login`)
- Хранение JWT в `localStorage`
- Меню: поиск, фильтр, пагинация, добавление в корзину
- Корзина: изменение количества и пересчёт суммы
- Оформление заказа: `ASAP` или `scheduled`
- История заказов и просмотр деталей

## Запуск
1. Убедитесь, что `FoodDelivery.WebApi` запущен на `http://localhost:5197`.
2. В этом каталоге выполните:

```bash
npm install
npm run dev
```

Открыть: `http://localhost:5173`

## API base URL
По умолчанию frontend ходит на `/api/v1` (через Vite proxy на `http://localhost:5197`).

При необходимости можно задать переменную окружения:

```bash
VITE_API_BASE=/api/v1
```
