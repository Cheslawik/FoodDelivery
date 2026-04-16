# FoodDelivery Frontend (React)

Frontend application for `FoodDelivery.WebApi`, built with React + TypeScript + Vite.

## Features
- Registration and login (`/api/v1/auth/register`, `/api/v1/auth/login`)
- JWT storage in `localStorage`
- Menu: search, filtering, pagination, add to cart
- Cart: quantity updates and total recalculation
- Checkout: `ASAP` or `scheduled`
- Order history and order details

## Run
1. Make sure `FoodDelivery.WebApi` is running at `http://localhost:5197`.
2. In this directory run:

```bash
npm install
npm run dev
```

Open: `http://localhost:5173`

## API base URL
By default the frontend calls `/api/v1` (through Vite proxy to `http://localhost:5197`).

You can override it with an environment variable:

```bash
VITE_API_BASE=/api/v1
```
