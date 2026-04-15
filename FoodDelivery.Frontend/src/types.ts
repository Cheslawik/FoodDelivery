export interface PaginatedResult<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
}

export interface Product {
  id: string
  name: string
  description: string
  price: number
  imageUrl: string
  categoryName: string
}

export interface CartItem {
  cartItemId: string
  productId: string
  productName: string
  unitPrice: number
  quantity: number
  lineTotal: number
}

export interface Cart {
  cartId: string
  items: CartItem[]
  totalAmount: number
}

export interface OrderItem {
  productId: string
  productName: string
  unitPrice: number
  quantity: number
  lineTotal: number
}

export interface Order {
  orderId: string
  contactName: string
  contactPhone: string
  deliveryAddress: string
  deliveryType: string
  scheduledDeliveryTimeUtc: string | null
  totalAmount: number
  items: OrderItem[]
}

export interface AuthResponse {
  userId: string
  email: string
  fullName: string
  phone?: string
  address?: string
  accessToken: string
}

export interface LoginPayload {
  email: string
  password: string
}

export interface RegisterPayload {
  email: string
  fullName: string
  phone: string
  address: string
  password: string
}

export interface AddCartItemPayload {
  productId: string
  quantity: number
}

export interface UpdateCartItemPayload {
  quantity: number
}

export interface CreateOrderPayload {
  contactName: string
  contactPhone: string
  deliveryAddress: string
  isAsap: boolean
  scheduledDeliveryTimeUtc: string | null
}

export interface CreateOrderResponse {
  orderId: string
}

export interface ApiErrorShape {
  statusCode?: number
  message?: string
  errors?: string[]
}
