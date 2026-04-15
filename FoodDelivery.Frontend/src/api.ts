import type {
  AddCartItemPayload,
  ApiErrorShape,
  AuthResponse,
  Cart,
  CreateOrderPayload,
  CreateOrderResponse,
  LoginPayload,
  Order,
  PaginatedResult,
  Product,
  RegisterPayload,
  UpdateCartItemPayload,
} from './types'

const API_ROOT = import.meta.env.VITE_API_BASE ?? '/api/v1'

async function parseError(response: Response): Promise<string> {
  try {
    const payload = (await response.json()) as ApiErrorShape
    if (Array.isArray(payload.errors) && payload.errors.length > 0) {
      return payload.errors.join(' ')
    }

    if (payload.message && payload.message.trim().length > 0) {
      return payload.message
    }
  } catch {
    // Ignore JSON parse errors and use status text fallback.
  }

  return response.statusText || 'Request failed.'
}

async function request<T>(
  path: string,
  options: RequestInit = {},
  token?: string,
): Promise<T> {
  const headers = new Headers(options.headers)
  if (options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  let response: Response
  try {
    response = await fetch(`${API_ROOT}${path}`, {
      ...options,
      headers,
    })
  } catch {
    throw new Error(
      'Не удалось подключиться к API. Проверьте, что бэкенд запущен и доступен.',
    )
  }

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error('Сессия истекла. Войдите снова.')
    }
    throw new Error(await parseError(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  const text = await response.text()
  return (text ? (JSON.parse(text) as T) : undefined) as T
}

export const api = {
  getMenu: (query: {
    pageNumber: number
    pageSize: number
    category?: string
    search?: string
  }) => {
    const params = new URLSearchParams({
      pageNumber: String(query.pageNumber),
      pageSize: String(query.pageSize),
    })

    if (query.category) {
      params.set('category', query.category)
    }

    if (query.search) {
      params.set('search', query.search)
    }

    return request<PaginatedResult<Product>>(`/menu?${params.toString()}`)
  },

  register: (payload: RegisterPayload) =>
    request<AuthResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  login: (payload: LoginPayload) =>
    request<AuthResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  getCart: (token: string) => request<Cart>('/cart', {}, token),

  addCartItem: (token: string, payload: AddCartItemPayload) =>
    request<void>(
      '/cart/items',
      {
        method: 'POST',
        body: JSON.stringify(payload),
      },
      token,
    ),

  updateCartItem: (
    token: string,
    cartItemId: string,
    payload: UpdateCartItemPayload,
  ) =>
    request<void>(
      `/cart/items/${cartItemId}`,
      {
        method: 'PUT',
        body: JSON.stringify(payload),
      },
      token,
    ),

  deleteCartItem: (token: string, cartItemId: string) =>
    request<void>(
      `/cart/items/${cartItemId}`,
      {
        method: 'DELETE',
      },
      token,
    ),

  createOrder: (token: string, payload: CreateOrderPayload) =>
    request<CreateOrderResponse>(
      '/orders',
      {
        method: 'POST',
        body: JSON.stringify(payload),
      },
      token,
    ),

  getOrders: (token: string) => request<Order[]>('/orders', {}, token),
  getOrderById: (token: string, orderId: string) =>
    request<Order>(`/orders/${orderId}`, {}, token),
}
