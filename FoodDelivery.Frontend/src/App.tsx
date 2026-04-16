import { useCallback, useEffect, useMemo, useState } from 'react'
import type { Dispatch, FormEvent, SetStateAction } from 'react'
import { Link, Navigate, NavLink, Route, Routes, useNavigate } from 'react-router-dom'
import { api } from './api'
import './App.css'
import type { AuthResponse, Cart, Order, PaginatedResult, Product } from './types'

type AuthMode = 'login' | 'register'
type Notice = { type: 'success' | 'error'; text: string } | null

interface SessionProfile {
  email: string
  fullName: string
  phone: string
  address: string
}

interface MenuQueryState {
  pageNumber: number
  pageSize: number
  search: string
  category: string
}

interface CheckoutState {
  contactName: string
  contactPhone: string
  deliveryAddress: string
  isAsap: boolean
  scheduledLocal: string
}

interface MenuPageProps {
  categories: string[]
  menu: PaginatedResult<Product> | null
  menuLoading: boolean
  menuQuery: MenuQueryState
  pendingProductIds: string[]
  totalPages: number
  onAddToCart: (productId: string) => Promise<void>
  setMenuQuery: Dispatch<SetStateAction<MenuQueryState>>
}

interface CartPageProps {
  cart: Cart | null
  cartItemsCount: number
  cartLoading: boolean
  checkoutForm: CheckoutState
  isAuthenticated: boolean
  placingOrder: boolean
  useHomeAddress: boolean
  onOrderSubmit: (event: FormEvent<HTMLFormElement>) => Promise<void>
  onQuantityChange: (cartItemId: string, quantity: number) => Promise<void>
  onUseHomeAddressToggle: (checked: boolean) => void
  setCheckoutForm: Dispatch<SetStateAction<CheckoutState>>
}

interface OrdersPageProps {
  isAuthenticated: boolean
  orders: Order[]
  ordersLoading: boolean
  selectedOrder: Order | null
  selectedOrderLoadingId: string
  onSelectOrder: (orderId: string) => Promise<void>
}

interface AuthPanelProps {
  authMode: AuthMode
  authSubmitting: boolean
  isAuthenticated: boolean
  loginForm: {
    email: string
    password: string
  }
  profile: SessionProfile | null
  registerForm: {
    email: string
    fullName: string
    phone: string
    address: string
    password: string
  }
  clearSession: () => void
  onAuthSubmit: (event: FormEvent<HTMLFormElement>) => Promise<void>
  setAuthMode: Dispatch<SetStateAction<AuthMode>>
  setLoginForm: Dispatch<
    SetStateAction<{
      email: string
      password: string
    }>
  >
  setRegisterForm: Dispatch<
    SetStateAction<{
      email: string
      fullName: string
      phone: string
      address: string
      password: string
    }>
  >
}

const TOKEN_STORAGE_KEY = 'fd_access_token'
const PROFILE_STORAGE_KEY = 'fd_user_profile'
const IMAGE_FALLBACK_URL =
  'data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 640 380%22%3E%3Crect width=%22640%22 height=%22380%22 fill=%22%230f172a%22/%3E%3Ctext x=%2250%25%22 y=%2250%25%22 dominant-baseline=%22middle%22 text-anchor=%22middle%22 fill=%22%23e2e8f0%22 font-family=%22Arial%22 font-size=%2234%22%3EFresh%20Food%3C/text%3E%3C/svg%3E'

const initialMenuQuery: MenuQueryState = {
  pageNumber: 1,
  pageSize: 8,
  search: '',
  category: '',
}

const initialCheckout: CheckoutState = {
  contactName: '',
  contactPhone: '',
  deliveryAddress: '',
  isAsap: true,
  scheduledLocal: '',
}

function formatMoney(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 2,
  }).format(value)
}

function formatDate(value: string | null): string {
  if (!value) {
    return 'As soon as possible'
  }

  return new Date(value).toLocaleString('en-US', {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

function MenuPage({
  categories,
  menu,
  menuLoading,
  menuQuery,
  pendingProductIds,
  totalPages,
  onAddToCart,
  setMenuQuery,
}: MenuPageProps) {
  return (
    <section className="panel menu-panel">
      <div className="panel-header">
        <h2>Menu Catalog</h2>
        <p>{menu?.totalCount ?? 0} items</p>
      </div>

      <div className="filters">
        <input
          placeholder="Search by name or description"
          value={menuQuery.search}
          onChange={(event) =>
            setMenuQuery((prev) => ({
              ...prev,
              pageNumber: 1,
              search: event.target.value,
            }))
          }
        />
        <select
          value={menuQuery.category}
          onChange={(event) =>
            setMenuQuery((prev) => ({
              ...prev,
              pageNumber: 1,
              category: event.target.value,
            }))
          }
        >
          <option value="">All categories</option>
          {categories.map((category) => (
            <option key={category} value={category}>
              {category}
            </option>
          ))}
        </select>
      </div>

      {menuLoading ? (
        <p className="state-text">Loading menu...</p>
      ) : menu && menu.items.length > 0 ? (
        <div className="menu-grid">
          {menu.items.map((item) => (
            <article className="product-card" key={item.id}>
              <img
                alt={item.name}
                onError={(event) => {
                  event.currentTarget.onerror = null
                  event.currentTarget.src = IMAGE_FALLBACK_URL
                }}
                src={item.imageUrl || IMAGE_FALLBACK_URL}
              />
              <div className="product-content">
                <p className="chip">{item.categoryName || 'Uncategorized'}</p>
                <h3>{item.name}</h3>
                <p className="description">{item.description}</p>
                <div className="product-footer">
                  <strong>{formatMoney(item.price)}</strong>
                  <button
                    disabled={pendingProductIds.includes(item.id)}
                    onClick={() => void onAddToCart(item.id)}
                    type="button"
                  >
                    Add to cart
                  </button>
                </div>
              </div>
            </article>
          ))}
        </div>
      ) : (
        <p className="state-text">No items found for the current filters.</p>
      )}

      <div className="pager">
        <button
          disabled={menuQuery.pageNumber <= 1 || menuLoading}
          onClick={() =>
            setMenuQuery((prev) => ({
              ...prev,
              pageNumber: Math.max(1, prev.pageNumber - 1),
            }))
          }
          type="button"
        >
          Previous
        </button>
        <span>
          Page {menu?.pageNumber ?? 1} of {totalPages}
        </span>
        <button
          disabled={menuLoading || (menu?.pageNumber ?? 1) >= totalPages}
          onClick={() =>
            setMenuQuery((prev) => ({ ...prev, pageNumber: prev.pageNumber + 1 }))
          }
          type="button"
        >
          Next
        </button>
      </div>
    </section>
  )
}

function CartPage({
  cart,
  cartItemsCount,
  cartLoading,
  checkoutForm,
  isAuthenticated,
  placingOrder,
  useHomeAddress,
  onOrderSubmit,
  onQuantityChange,
  onUseHomeAddressToggle,
  setCheckoutForm,
}: CartPageProps) {
  return (
    <div className="page-stack">
      <section className="panel cart-panel">
        <div className="panel-header">
          <h2>Cart</h2>
          <p>{cartItemsCount} items</p>
        </div>

        {!isAuthenticated ? (
          <p className="state-text">Sign in to manage your cart.</p>
        ) : cartLoading ? (
          <p className="state-text">Loading cart...</p>
        ) : !cart || cart.items.length === 0 ? (
          <p className="state-text">Cart is empty.</p>
        ) : (
          <>
            <div className="cart-list">
              {cart.items.map((item) => (
                <div className="cart-row" key={item.cartItemId}>
                  <div>
                    <p>{item.productName}</p>
                    <small>{formatMoney(item.lineTotal)}</small>
                  </div>
                  <div className="qty">
                    <button
                      onClick={() =>
                        void onQuantityChange(item.cartItemId, item.quantity - 1)
                      }
                      type="button"
                    >
                      -
                    </button>
                    <span>{item.quantity}</span>
                    <button
                      onClick={() =>
                        void onQuantityChange(item.cartItemId, item.quantity + 1)
                      }
                      type="button"
                    >
                      +
                    </button>
                  </div>
                </div>
              ))}
            </div>
            <div className="summary-line">
              <span>Total:</span>
              <strong>{formatMoney(cart.totalAmount)}</strong>
            </div>
          </>
        )}
      </section>

      <section className="panel checkout-panel">
        <div className="panel-header">
          <h2>Place an Order</h2>
          <p>ASAP or scheduled</p>
        </div>

        <form className="checkout-form" onSubmit={(event) => void onOrderSubmit(event)}>
          <input
            placeholder="Recipient name"
            required
            value={checkoutForm.contactName}
            onChange={(event) =>
              setCheckoutForm((prev) => ({
                ...prev,
                contactName: event.target.value,
              }))
            }
          />
          <input
            placeholder="Phone"
            required
            value={checkoutForm.contactPhone}
            onChange={(event) =>
              setCheckoutForm((prev) => ({
                ...prev,
                contactPhone: event.target.value,
              }))
            }
          />
          <label className="checkbox">
            <input
              checked={useHomeAddress}
              disabled={!isAuthenticated}
              onChange={(event) => onUseHomeAddressToggle(event.target.checked)}
              type="checkbox"
            />
            Use home address
          </label>
          <input
            placeholder="Delivery address"
            required
            disabled={useHomeAddress}
            value={checkoutForm.deliveryAddress}
            onChange={(event) =>
              setCheckoutForm((prev) => ({
                ...prev,
                deliveryAddress: event.target.value,
              }))
            }
          />

          <label className="checkbox">
            <input
              checked={checkoutForm.isAsap}
              onChange={(event) =>
                setCheckoutForm((prev) => ({
                  ...prev,
                  isAsap: event.target.checked,
                }))
              }
              type="checkbox"
            />
            ASAP (as soon as possible)
          </label>

          {!checkoutForm.isAsap && (
            <input
              min={new Date().toISOString().slice(0, 16)}
              required
              type="datetime-local"
              value={checkoutForm.scheduledLocal}
              onChange={(event) =>
                setCheckoutForm((prev) => ({
                  ...prev,
                  scheduledLocal: event.target.value,
                }))
              }
            />
          )}

          <button
            disabled={!isAuthenticated || placingOrder || cartItemsCount === 0}
            type="submit"
          >
            {placingOrder ? 'Submitting...' : 'Confirm order'}
          </button>
        </form>
      </section>
    </div>
  )
}

function OrdersPage({
  isAuthenticated,
  orders,
  ordersLoading,
  selectedOrder,
  selectedOrderLoadingId,
  onSelectOrder,
}: OrdersPageProps) {
  return (
    <section className="panel orders-panel">
      <div className="panel-header">
        <h2>Order History</h2>
        <p>{orders.length} orders</p>
      </div>

      {!isAuthenticated ? (
        <p className="state-text">Order history is available after sign-in.</p>
      ) : ordersLoading ? (
        <p className="state-text">Loading orders...</p>
      ) : orders.length === 0 ? (
        <p className="state-text">No completed orders yet.</p>
      ) : (
        <div className="orders-list">
          {orders.map((order) => (
            <button
              className="order-row"
              key={order.orderId}
              onClick={() => void onSelectOrder(order.orderId)}
              type="button"
            >
              <span>#{order.orderId.slice(0, 8)}</span>
              <strong>{formatMoney(order.totalAmount)}</strong>
            </button>
          ))}
        </div>
      )}

      {selectedOrder && (
        <div className="order-details">
          <h3>Order #{selectedOrder.orderId.slice(0, 8)}</h3>
          <p>
            <strong>Contact:</strong> {selectedOrder.contactName}, {selectedOrder.contactPhone}
          </p>
          <p>
            <strong>Address:</strong> {selectedOrder.deliveryAddress}
          </p>
          <p>
            <strong>Delivery:</strong>{' '}
            {selectedOrder.deliveryType === 'Scheduled'
              ? formatDate(selectedOrder.scheduledDeliveryTimeUtc)
              : 'ASAP'}
          </p>
          <div className="details-items">
            {selectedOrder.items.map((item) => (
              <div className="details-row" key={item.productId}>
                <span>
                  {item.productName} x{item.quantity}
                </span>
                <strong>{formatMoney(item.lineTotal)}</strong>
              </div>
            ))}
          </div>
          <div className="summary-line">
            <span>Amount:</span>
            <strong>{formatMoney(selectedOrder.totalAmount)}</strong>
          </div>
        </div>
      )}

      {selectedOrderLoadingId && (
        <p className="state-text">Loading order {selectedOrderLoadingId}...</p>
      )}
    </section>
  )
}

function AuthPanel({
  authMode,
  authSubmitting,
  isAuthenticated,
  loginForm,
  profile,
  registerForm,
  clearSession,
  onAuthSubmit,
  setAuthMode,
  setLoginForm,
  setRegisterForm,
}: AuthPanelProps) {
  return (
    <section className="panel auth-panel">
      <div className="panel-header">
        <h2>{isAuthenticated ? 'Profile' : 'Authentication'}</h2>
        <p>{profile?.email ?? 'Access to cart and orders'}</p>
      </div>

      {isAuthenticated && profile ? (
        <div className="profile-card">
          <p className="welcome">{profile.fullName}</p>
          <p>{profile.email}</p>
          <p>{profile.phone || 'Phone not specified'}</p>
          <p>{profile.address || 'Home address not specified'}</p>
          <button onClick={clearSession} type="button">
            End session
          </button>
        </div>
      ) : (
        <form className="auth-form" onSubmit={(event) => void onAuthSubmit(event)}>
          <div className="auth-switcher">
            <button
              className={authMode === 'login' ? 'active' : ''}
              onClick={() => setAuthMode('login')}
              type="button"
            >
              Sign in
            </button>
            <button
              className={authMode === 'register' ? 'active' : ''}
              onClick={() => setAuthMode('register')}
              type="button"
            >
              Sign up
            </button>
          </div>

          <input
            autoComplete="email"
            placeholder="Email"
            required
            type="email"
            value={authMode === 'login' ? loginForm.email : registerForm.email}
            onChange={(event) =>
              authMode === 'login'
                ? setLoginForm((prev) => ({ ...prev, email: event.target.value }))
                : setRegisterForm((prev) => ({
                    ...prev,
                    email: event.target.value,
                  }))
            }
          />

          {authMode === 'register' && (
            <>
              <input
                placeholder="Full name"
                required
                value={registerForm.fullName}
                onChange={(event) =>
                  setRegisterForm((prev) => ({
                    ...prev,
                    fullName: event.target.value,
                  }))
                }
              />
              <input
                placeholder="Phone"
                required
                value={registerForm.phone}
                onChange={(event) =>
                  setRegisterForm((prev) => ({
                    ...prev,
                    phone: event.target.value,
                  }))
                }
              />
              <input
                placeholder="Address"
                required
                value={registerForm.address}
                onChange={(event) =>
                  setRegisterForm((prev) => ({
                    ...prev,
                    address: event.target.value,
                  }))
                }
              />
            </>
          )}

          <input
            autoComplete={authMode === 'login' ? 'current-password' : 'new-password'}
            placeholder="Password"
            required
            type="password"
            value={authMode === 'login' ? loginForm.password : registerForm.password}
            onChange={(event) =>
              authMode === 'login'
                ? setLoginForm((prev) => ({
                    ...prev,
                    password: event.target.value,
                  }))
                : setRegisterForm((prev) => ({
                    ...prev,
                    password: event.target.value,
                  }))
            }
          />

          <button disabled={authSubmitting} type="submit">
            {authSubmitting
              ? 'Please wait...'
              : authMode === 'login'
                ? 'Sign in'
                : 'Create account'}
          </button>
        </form>
      )}
    </section>
  )
}

function App() {
  const navigate = useNavigate()
  const [authMode, setAuthMode] = useState<AuthMode>('login')
  const [notice, setNotice] = useState<Notice>(null)

  const [token, setToken] = useState<string>(
    () => window.localStorage.getItem(TOKEN_STORAGE_KEY) ?? '',
  )

  const [profile, setProfile] = useState<SessionProfile | null>(() => {
    const raw = window.localStorage.getItem(PROFILE_STORAGE_KEY)
    if (!raw) {
      return null
    }

    try {
      const parsed = JSON.parse(raw) as Partial<SessionProfile>
      if (!parsed.email || !parsed.fullName) {
        return null
      }

      return {
        email: parsed.email,
        fullName: parsed.fullName,
        phone: parsed.phone ?? '',
        address: parsed.address ?? '',
      }
    } catch {
      return null
    }
  })

  const [menuQuery, setMenuQuery] = useState<MenuQueryState>(initialMenuQuery)
  const [menu, setMenu] = useState<PaginatedResult<Product> | null>(null)
  const [menuLoading, setMenuLoading] = useState(false)

  const [cart, setCart] = useState<Cart | null>(null)
  const [cartLoading, setCartLoading] = useState(false)

  const [orders, setOrders] = useState<Order[]>([])
  const [ordersLoading, setOrdersLoading] = useState(false)
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null)
  const [selectedOrderLoadingId, setSelectedOrderLoadingId] = useState('')

  const [loginForm, setLoginForm] = useState({ email: '', password: '' })
  const [registerForm, setRegisterForm] = useState({
    email: '',
    fullName: '',
    phone: '',
    address: '',
    password: '',
  })
  const [checkoutForm, setCheckoutForm] = useState<CheckoutState>(initialCheckout)

  const [authSubmitting, setAuthSubmitting] = useState(false)
  const [placingOrder, setPlacingOrder] = useState(false)
  const [pendingProductIds, setPendingProductIds] = useState<string[]>([])
  const [useHomeAddress, setUseHomeAddress] = useState(false)

  const isAuthenticated = token.length > 0

  const showError = useCallback(
    (message: string) => setNotice({ type: 'error', text: message }),
    [],
  )
  const showSuccess = useCallback(
    (message: string) => setNotice({ type: 'success', text: message }),
    [],
  )

  const setSession = useCallback((response: AuthResponse) => {
    const nextProfile: SessionProfile = {
      email: response.email,
      fullName: response.fullName,
      phone: response.phone?.trim() ?? '',
      address: response.address?.trim() ?? '',
    }

    window.localStorage.setItem(TOKEN_STORAGE_KEY, response.accessToken)
    window.localStorage.setItem(PROFILE_STORAGE_KEY, JSON.stringify(nextProfile))

    setToken(response.accessToken)
    setProfile(nextProfile)
    setCheckoutForm((prev) => ({
      ...prev,
      contactName: response.fullName,
      contactPhone: response.phone?.trim() ?? prev.contactPhone,
    }))
  }, [])

  const clearSession = useCallback(() => {
    window.localStorage.removeItem(TOKEN_STORAGE_KEY)
    window.localStorage.removeItem(PROFILE_STORAGE_KEY)
    setToken('')
    setProfile(null)
    setCart(null)
    setOrders([])
    setSelectedOrder(null)
    setCheckoutForm(initialCheckout)
    setUseHomeAddress(false)
    navigate('/menu')
  }, [navigate])

  const loadMenu = useCallback(async () => {
    setMenuLoading(true)
    try {
      const response = await api.getMenu({
        pageNumber: menuQuery.pageNumber,
        pageSize: menuQuery.pageSize,
        search: menuQuery.search.trim(),
        category: menuQuery.category.trim(),
      })
      setMenu(response)
    } catch (error) {
      showError(error instanceof Error ? error.message : 'Failed to load menu.')
    } finally {
      setMenuLoading(false)
    }
  }, [menuQuery, showError])

  const loadCart = useCallback(
    async (options?: { silent?: boolean }) => {
      if (!token) {
        setCart(null)
        return
      }

      const isSilent = options?.silent ?? false
      if (!isSilent) {
        setCartLoading(true)
      }

      try {
        setCart(await api.getCart(token))
      } catch (error) {
        showError(error instanceof Error ? error.message : 'Failed to load cart.')
      } finally {
        if (!isSilent) {
          setCartLoading(false)
        }
      }
    },
    [showError, token],
  )

  const loadOrders = useCallback(async () => {
    if (!token) {
      setOrders([])
      return
    }

    setOrdersLoading(true)
    try {
      setOrders(await api.getOrders(token))
    } catch (error) {
      showError(error instanceof Error ? error.message : 'Failed to load orders.')
    } finally {
      setOrdersLoading(false)
    }
  }, [showError, token])

  useEffect(() => {
    void loadMenu()
  }, [loadMenu])

  useEffect(() => {
    if (!token) {
      return
    }

    void loadCart()
    void loadOrders()
  }, [token, loadCart, loadOrders])

  useEffect(() => {
    const fullName = profile?.fullName?.trim()
    const phone = profile?.phone?.trim()
    if (!fullName && !phone) {
      return
    }

    setCheckoutForm((prev) => ({
      ...prev,
      contactName: prev.contactName.trim() ? prev.contactName : fullName || '',
      contactPhone: prev.contactPhone.trim() ? prev.contactPhone : phone || '',
    }))
  }, [profile?.fullName, profile?.phone])

  useEffect(() => {
    if (!useHomeAddress) {
      return
    }

    const homeAddress = profile?.address?.trim()
    if (!homeAddress) {
      return
    }

    setCheckoutForm((prev) => ({
      ...prev,
      deliveryAddress: homeAddress,
    }))
  }, [profile?.address, useHomeAddress])

  const categories = useMemo(() => {
    const names = new Set(
      menu?.items
        .map((item) => item.categoryName?.trim())
        .filter((value): value is string => Boolean(value)) ?? [],
    )
    if (menuQuery.category && !names.has(menuQuery.category)) {
      names.add(menuQuery.category)
    }
    return [...names].sort((a, b) => a.localeCompare(b))
  }, [menu, menuQuery.category])

  const totalPages = useMemo(() => {
    if (!menu || menu.totalCount === 0) {
      return 1
    }
    return Math.max(1, Math.ceil(menu.totalCount / menu.pageSize))
  }, [menu])

  const cartItemsCount = cart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0

  const handleAuthSubmit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault()
      setAuthSubmitting(true)

      try {
        const response =
          authMode === 'login'
            ? await api.login(loginForm)
            : await api.register(registerForm)

        setSession(response)
        showSuccess(
          authMode === 'login'
            ? 'You have successfully signed in.'
            : 'Registration completed, session is active.',
        )
        setAuthMode('login')
        setRegisterForm((prev) => ({ ...prev, password: '' }))
        setLoginForm((prev) => ({ ...prev, password: '' }))
        await Promise.all([loadCart(), loadOrders()])
        navigate('/menu')
      } catch (error) {
        showError(
          error instanceof Error ? error.message : 'Failed to complete authentication.',
        )
      } finally {
        setAuthSubmitting(false)
      }
    },
    [
      authMode,
      loadCart,
      loadOrders,
      loginForm,
      navigate,
      registerForm,
      setSession,
      showError,
      showSuccess,
    ],
  )

  const handleAddToCart = useCallback(
    async (productId: string) => {
      if (!token) {
        showError('Please sign in first to add items to your cart.')
        return
      }

      if (pendingProductIds.includes(productId)) {
        return
      }

      setPendingProductIds((prev) => [...prev, productId])
      try {
        try {
          await api.addCartItem(token, { productId, quantity: 1 })
        } catch (addError) {
          const actualCart = await api.getCart(token)
          const existingItem = actualCart.items.find((item) => item.productId === productId)

          if (!existingItem) {
            throw addError
          }

          if (existingItem.quantity >= 50) {
            throw new Error('Maximum 50 units of the same product in the cart.')
          }

          await api.updateCartItem(token, existingItem.cartItemId, {
            quantity: existingItem.quantity + 1,
          })
        }

        await loadCart({ silent: true })
        showSuccess('Item added to cart.')
      } catch (error) {
        const message =
          error instanceof Error ? error.message : 'Failed to add item to cart.'

        if (message === 'Session expired. Please sign in again.') {
          clearSession()
        }

        showError(message)
      } finally {
        setPendingProductIds((prev) => prev.filter((id) => id !== productId))
      }
    },
    [clearSession, loadCart, pendingProductIds, showError, showSuccess, token],
  )

  const handleQuantityChange = useCallback(
    async (cartItemId: string, quantity: number) => {
      if (!token) {
        return
      }

      try {
        if (quantity <= 0) {
          await api.deleteCartItem(token, cartItemId)
        } else {
          await api.updateCartItem(token, cartItemId, { quantity })
        }
        await loadCart({ silent: true })
      } catch (error) {
        showError(error instanceof Error ? error.message : 'Failed to update quantity.')
      }
    },
    [loadCart, showError, token],
  )

  const handleUseHomeAddressToggle = useCallback(
    (checked: boolean) => {
      if (!checked) {
        setUseHomeAddress(false)
        return
      }

      const homeAddress = profile?.address?.trim()
      if (!homeAddress) {
        showError('No home address in profile. Please enter the address manually.')
        return
      }

      setUseHomeAddress(true)
      setCheckoutForm((prev) => ({
        ...prev,
        deliveryAddress: homeAddress,
      }))
    },
    [profile?.address, showError],
  )

  const handleOrderSubmit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault()

      if (!token) {
        showError('Authentication is required to place an order.')
        return
      }

      if (!cart || cart.items.length === 0) {
        showError('Cart is empty.')
        return
      }

      let scheduledUtc: string | null = null
      if (!checkoutForm.isAsap) {
        if (!checkoutForm.scheduledLocal) {
          showError('Please select a delivery time.')
          return
        }

        const parsedDate = new Date(checkoutForm.scheduledLocal)
        if (Number.isNaN(parsedDate.getTime())) {
          showError('Invalid delivery date.')
          return
        }

        scheduledUtc = parsedDate.toISOString()
      }

      setPlacingOrder(true)
      try {
        const created = await api.createOrder(token, {
          contactName: checkoutForm.contactName.trim(),
          contactPhone: checkoutForm.contactPhone.trim(),
          deliveryAddress: checkoutForm.deliveryAddress.trim(),
          isAsap: checkoutForm.isAsap,
          scheduledDeliveryTimeUtc: scheduledUtc,
        })

        await Promise.all([loadCart(), loadOrders()])

        if (created?.orderId) {
          setSelectedOrder(await api.getOrderById(token, created.orderId))
        }

        setCheckoutForm((prev) => ({ ...prev, isAsap: true, scheduledLocal: '' }))
        navigate('/orders')
        showSuccess('Order placed successfully.')
      } catch (error) {
        showError(error instanceof Error ? error.message : 'Failed to place order.')
      } finally {
        setPlacingOrder(false)
      }
    },
    [cart, checkoutForm, loadCart, loadOrders, navigate, showError, showSuccess, token],
  )

  const handleSelectOrder = useCallback(
    async (orderId: string) => {
      if (!token) {
        return
      }

      setSelectedOrderLoadingId(orderId)
      try {
        setSelectedOrder(await api.getOrderById(token, orderId))
        navigate('/orders')
      } catch (error) {
        showError(
          error instanceof Error ? error.message : 'Failed to load order details.',
        )
      } finally {
        setSelectedOrderLoadingId('')
      }
    },
    [navigate, showError, token],
  )

  return (
    <div className="app">
      <div className="bg-layer" />
      <header className="topbar">
        <Link className="brand-link" to="/menu">
          <p className="eyebrow">Food Delivery Front Office</p>
          <h1>BiteBoard</h1>
        </Link>
        <div className="topbar-right">
          <nav className="views">
            <NavLink to="/menu">Menu</NavLink>
            <NavLink to="/cart">Cart ({cartItemsCount})</NavLink>
            <NavLink to="/orders">Orders</NavLink>
          </nav>
          {isAuthenticated ? (
            <button className="logout" onClick={clearSession} type="button">
              Log out
            </button>
          ) : (
            <span className="auth-pill">Not signed in</span>
          )}
        </div>
      </header>

      {notice && (
        <div className={`notice ${notice.type}`} role="status">
          {notice.text}
        </div>
      )}

      <main className="layout">
        <div className="page-stack">
          <Routes>
            <Route path="/" element={<Navigate replace to="/menu" />} />
            <Route
              path="/menu"
              element={
                <MenuPage
                  categories={categories}
                  menu={menu}
                  menuLoading={menuLoading}
                  menuQuery={menuQuery}
                  pendingProductIds={pendingProductIds}
                  totalPages={totalPages}
                  onAddToCart={handleAddToCart}
                  setMenuQuery={setMenuQuery}
                />
              }
            />
            <Route
              path="/cart"
              element={
                <CartPage
                  cart={cart}
                  cartItemsCount={cartItemsCount}
                  cartLoading={cartLoading}
                  checkoutForm={checkoutForm}
                  isAuthenticated={isAuthenticated}
                  placingOrder={placingOrder}
                  useHomeAddress={useHomeAddress}
                  onOrderSubmit={handleOrderSubmit}
                  onQuantityChange={handleQuantityChange}
                  onUseHomeAddressToggle={handleUseHomeAddressToggle}
                  setCheckoutForm={setCheckoutForm}
                />
              }
            />
            <Route
              path="/orders"
              element={
                <OrdersPage
                  isAuthenticated={isAuthenticated}
                  orders={orders}
                  ordersLoading={ordersLoading}
                  selectedOrder={selectedOrder}
                  selectedOrderLoadingId={selectedOrderLoadingId}
                  onSelectOrder={handleSelectOrder}
                />
              }
            />
            <Route path="*" element={<Navigate replace to="/menu" />} />
          </Routes>
        </div>

        <aside className="sidebar">
          <AuthPanel
            authMode={authMode}
            authSubmitting={authSubmitting}
            isAuthenticated={isAuthenticated}
            loginForm={loginForm}
            profile={profile}
            registerForm={registerForm}
            clearSession={clearSession}
            onAuthSubmit={handleAuthSubmit}
            setAuthMode={setAuthMode}
            setLoginForm={setLoginForm}
            setRegisterForm={setRegisterForm}
          />
        </aside>
      </main>
    </div>
  )
}

export default App


