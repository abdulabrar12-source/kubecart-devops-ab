# KubeCart — Design Document

---

## Table of Contents

1. [System Architecture](#1-system-architecture)
2. [Service Responsibilities](#2-service-responsibilities)
3. [API Reference](#3-api-reference)
4. [Data Model](#4-data-model)
5. [Inter-Service Communication](#5-inter-service-communication)
6. [Authentication & Authorization Flow](#6-authentication--authorization-flow)
7. [Kubernetes Topology](#7-kubernetes-topology)
8. [Monitoring](#8-monitoring)
9. [Key Design Decisions](#9-key-design-decisions)

---

## 1. System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  Browser                                                        │
└────────────────────────┬────────────────────────────────────────┘
                         │  HTTP
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│  nginx Ingress Controller  (namespace: ingress-nginx)           │
│                                                                 │
│   /api/auth/*    ──►  identity-service:80                       │
│   /api/catalog/* ──►  catalog-service:80  (2 replicas)          │
│   /api/orders/*  ──►  order-service:80                          │
│   /*             ──►  frontend:80                               │
└───────┬──────────────────┬─────────────────┬────────────────────┘
        │                  │                 │
        ▼                  ▼                 ▼
┌───────────────┐  ┌───────────────┐  ┌─────────────────────────┐
│identity-svc   │  │catalog-svc    │  │order-svc                │
│.NET 10 Minimal│  │.NET 10 Minimal│  │.NET 10 Minimal API      │
│API            │  │API            │  │                         │
│  JWT sign/    │  │  Categories   │  │  Cart CRUD              │
│  validate     │  │  Products     │  │  Checkout               │
│  User mgmt    │  │  Image URLs   │  │  Order history ◄──HTTP──┼─► catalog-svc
└──────┬────────┘  └──────┬────────┘  └─────────┬───────────────┘
       │                  │                      │
       ▼                  ▼                      ▼
┌──────────────┐  ┌───────────────┐  ┌───────────────────────────┐
│KubeCart_     │  │KubeCart_      │  │KubeCart_Orders            │
│Identity (SQL)│  │Catalog (SQL)  │  │(SQL Server)               │
└──────────────┘  └───────────────┘  └───────────────────────────┘

Monitoring namespace:
  Prometheus → scrapes /metrics from all 3 services
  Grafana    → "KubeCart Service Overview" dashboard
```

---

## 2. Service Responsibilities

### identity-service
- **Register** — hash + AES-encrypt user PII, insert into DB, return JWT
- **Login** — validate credentials, return JWT (HS256, 7-day TTL)
- **Token validation** — `/api/auth/me` verifies `Authorization: Bearer <token>`, returns user claims
- No dependency on other services at runtime

### catalog-service
- **Categories** — CRUD (public reads, admin writes)
- **Products** — CRUD with category FK, image URL management
- **Access control** — admin endpoints check JWT claim `role=admin`
- No dependency on other services at runtime

### order-service
- **Cart** — per-user cart items stored in DB (not session), keyed by `userId + productId`
- **Checkout** — calls catalog-service to validate products exist and resolve prices, creates an `Order` + `OrderItems`, clears cart
- **Order history** — users see own orders; admin can see all orders and update status
- Depends on catalog-service at checkout time

### frontend (ui)
- Single Page Application built with React 19 + Vite 8
- nginx serves static assets and reverse-proxies `/api/*` to backend services at runtime via envsubst template
- Auth state managed in `AuthContext` (localStorage JWT)
- React Router v7 for client-side routing

---

## 3. API Reference

All endpoints except `/api/auth/register` and `/api/auth/login` require:

```
Authorization: Bearer <jwt_token>
```

---

### 3.1 Identity Service — `/api/auth`

#### `POST /api/auth/register`

Register a new user account.

**Request body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass1!",
  "firstName": "Jane",
  "lastName": "Doe"
}
```

**Response `201`:**
```json
{
  "token": "<jwt>",
  "userId": "uuid",
  "email": "user@example.com",
  "firstName": "Jane",
  "role": "customer"
}
```

---

#### `POST /api/auth/login`

Authenticate and receive a JWT.

**Request body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass1!"
}
```

**Response `200`:**
```json
{
  "token": "<jwt>",
  "userId": "uuid",
  "email": "user@example.com",
  "firstName": "Jane",
  "role": "customer"
}
```

---

#### `GET /api/auth/me`

Returns the authenticated user's claims decoded from the JWT.

**Response `200`:**
```json
{
  "userId": "uuid",
  "email": "user@example.com",
  "firstName": "Jane",
  "role": "customer"
}
```

---

### 3.2 Catalog Service — `/api/catalog`

#### `GET /api/catalog/categories`

List all categories (public).

**Response `200`:**
```json
[
  { "id": 1, "name": "Electronics" },
  { "id": 2, "name": "Clothing" }
]
```

---

#### `POST /api/catalog/admin/categories`

Create a new category. **Requires `role=admin`.**

**Request body:**
```json
{ "name": "Furniture" }
```

**Response `201`:**
```json
{ "id": 3, "name": "Furniture" }
```

---

#### `GET /api/catalog/products`

List products. Supports optional query param `?categoryId=1`.

**Response `200`:**
```json
[
  {
    "id": "uuid",
    "name": "Laptop Pro",
    "description": "...",
    "price": 1299.99,
    "imageUrl": "https://...",
    "categoryId": 1,
    "categoryName": "Electronics",
    "stock": 42
  }
]
```

---

#### `GET /api/catalog/products/{id}`

Get a single product by ID.

**Response `200`:** Same shape as list item above.

---

#### `POST /api/catalog/admin/products`

Create a product. **Requires `role=admin`.**

**Request body:**
```json
{
  "name": "Wireless Keyboard",
  "description": "...",
  "price": 79.99,
  "imageUrl": "https://...",
  "categoryId": 1,
  "stock": 100
}
```

**Response `201`:** Full product object.

---

#### `PUT /api/catalog/admin/products/{id}`

Update a product. **Requires `role=admin`.**

**Request body:** Same as create (all fields).

**Response `200`:** Updated product object.

---

#### `DELETE /api/catalog/admin/products/{id}`

Delete a product. **Requires `role=admin`.**

**Response `204`:** No content.

---

#### `POST /api/catalog/admin/products/{id}/images`

Update the image URL of a product. **Requires `role=admin`.**

**Request body:**
```json
{ "imageUrl": "https://example.com/new-image.jpg" }
```

**Response `200`:** Updated product object.

---

### 3.3 Order Service — `/api/orders`

All endpoints require authentication.

#### `GET /api/orders/cart`

Get the current user's cart.

**Response `200`:**
```json
[
  {
    "productId": "uuid",
    "productName": "Laptop Pro",
    "price": 1299.99,
    "quantity": 2,
    "imageUrl": "https://..."
  }
]
```

---

#### `POST /api/orders/cart/items`

Add a product to the cart (or increment quantity if already present).

**Request body:**
```json
{
  "productId": "uuid",
  "quantity": 1
}
```

**Response `200`:** Updated cart array.

---

#### `PUT /api/orders/cart/items/{productId}`

Update the quantity of a cart item. Send `quantity: 0` to remove.

**Request body:**
```json
{ "quantity": 3 }
```

**Response `200`:** Updated cart array.

---

#### `DELETE /api/orders/cart/items/{productId}`

Remove an item from the cart entirely.

**Response `200`:** Updated cart array.

---

#### `POST /api/orders/checkout`

Checkout the current cart — validates products with catalog-service, creates an order, clears the cart.

**Request body:**
```json
{
  "shippingAddress": "123 Main St, Springfield, IL 62701"
}
```

**Response `201`:**
```json
{
  "orderId": "uuid",
  "status": "Pending",
  "total": 2599.98,
  "createdAt": "2025-07-01T14:22:00Z"
}
```

---

#### `GET /api/orders`

List the authenticated user's orders.

**Response `200`:**
```json
[
  {
    "orderId": "uuid",
    "status": "Shipped",
    "total": 2599.98,
    "itemCount": 2,
    "createdAt": "2025-07-01T14:22:00Z"
  }
]
```

---

#### `GET /api/orders/{id}`

Get full order details including line items.

**Response `200`:**
```json
{
  "orderId": "uuid",
  "status": "Pending",
  "shippingAddress": "123 Main St...",
  "total": 2599.98,
  "createdAt": "2025-07-01T14:22:00Z",
  "items": [
    { "productId": "uuid", "productName": "Laptop Pro", "price": 1299.99, "quantity": 2 }
  ]
}
```

---

#### `PUT /api/orders/admin/{orderId}/status`

Update order status. **Requires `role=admin`.**

**Request body:**
```json
{ "status": "Shipped" }
```

Valid statuses: `Pending`, `Processing`, `Shipped`, `Delivered`, `Cancelled`

**Response `200`:** Updated order summary.

---

## 4. Data Model

### KubeCart_Identity

```
Users
  Id            UNIQUEIDENTIFIER  PK default NEWID()
  Email         NVARCHAR(256)     UNIQUE NOT NULL (encrypted at rest)
  PasswordHash  NVARCHAR(512)     NOT NULL (BCrypt)
  FirstName     NVARCHAR(128)     (encrypted at rest)
  LastName      NVARCHAR(128)     (encrypted at rest)
  Role          NVARCHAR(32)      NOT NULL default 'customer'
  CreatedAt     DATETIME2         NOT NULL default GETUTCDATE()
```

### KubeCart_Catalog

```
Categories
  Id    INT           PK IDENTITY
  Name  NVARCHAR(128) NOT NULL

Products
  Id          UNIQUEIDENTIFIER  PK default NEWID()
  Name        NVARCHAR(256)     NOT NULL
  Description NVARCHAR(MAX)
  Price       DECIMAL(18,2)     NOT NULL
  ImageUrl    NVARCHAR(2048)
  CategoryId  INT               FK → Categories.Id
  Stock       INT               NOT NULL default 0
  CreatedAt   DATETIME2         NOT NULL default GETUTCDATE()
```

### KubeCart_Orders

```
CartItems
  Id        INT              PK IDENTITY
  UserId    UNIQUEIDENTIFIER NOT NULL
  ProductId UNIQUEIDENTIFIER NOT NULL
  Quantity  INT              NOT NULL
  AddedAt   DATETIME2        NOT NULL default GETUTCDATE()
  UNIQUE (UserId, ProductId)

Orders
  Id              UNIQUEIDENTIFIER  PK default NEWID()
  UserId          UNIQUEIDENTIFIER  NOT NULL
  Status          NVARCHAR(32)      NOT NULL default 'Pending'
  ShippingAddress NVARCHAR(512)
  Total           DECIMAL(18,2)     NOT NULL
  CreatedAt       DATETIME2         NOT NULL default GETUTCDATE()

OrderItems
  Id          INT              PK IDENTITY
  OrderId     UNIQUEIDENTIFIER FK → Orders.Id
  ProductId   UNIQUEIDENTIFIER NOT NULL
  ProductName NVARCHAR(256)    NOT NULL  ← denormalized at checkout time
  Price       DECIMAL(18,2)    NOT NULL  ← price locked at checkout time
  Quantity    INT              NOT NULL
```

**Note:** `ProductName` and `Price` are intentionally denormalized in `OrderItems`. This ensures order history is not affected by future catalog updates.

---

## 5. Inter-Service Communication

The only runtime inter-service call is **order-service → catalog-service** at checkout.

```
order-service/Services/CatalogClient.cs
  └── GET {CATALOG_SERVICE_URL}/api/catalog/products/{productId}
        for each item in the cart
```

- Uses `HttpClient` injected via DI (`AddHttpClient<ICatalogClient>`)
- `CatalogServiceException` wraps HTTP errors for clean propagation
- If catalog-service is unreachable, checkout fails with `503 Service Unavailable`
- The JWT from the incoming user request is forwarded so catalog-service can validate auth

All other service interactions go through the nginx Ingress (browser calls `/api/*`). Services do **not** call each other directly except for the checkout path above.

---

## 6. Authentication & Authorization Flow

```
1. User POSTs /api/auth/login → identity-service issues JWT
       Payload: { sub: userId, email, firstName, role, exp: +7days }
       Algorithm: HS256  Key: JWT_SIGNING_KEY env var

2. Browser stores token in localStorage via AuthContext

3. Every API call includes:
       Authorization: Bearer <token>

4. catalog-service and order-service validate the token locally
       using the same JWT_SIGNING_KEY (injected via K8s Secret)
       No call to identity-service per request

5. Admin routes check the `role` claim:
       if (role != "admin") → 403 Forbidden
```

**Token expiry:** 7 days. No refresh token mechanism — user must re-login.

**PII encryption:** `firstName`, `lastName`, `email` stored AES-256 encrypted in SQL using `APP_ENCRYPTION_KEY`. Decrypted only in application layer.

---

## 7. Kubernetes Topology

```
Namespace: demo
  Workloads:
    identity-deployment    1 replica   image: identity-service:latest
    catalog-deployment     2 replicas  image: catalog-service:latest
    order-deployment       1 replica   image: order-service:latest
    frontend-deployment    1 replica   image: frontend:latest

  Services (ClusterIP):
    identity-service  → port 80
    catalog-service   → port 80
    order-service     → port 80
    frontend-service  → port 80

  Config:
    Secrets (kubecart-secrets):
      DB_HOST, DB_USER, DB_PASSWORD,
      JWT_SIGNING_KEY, APP_ENCRYPTION_KEY
    ConfigMaps (per service):
      DB_NAME, ASPNETCORE_URLS,
      CATALOG_SERVICE_URL (order-service only)

  Ingress (kubecart-ingress):
    nginx Ingress Controller (namespace: ingress-nginx)
    Rules:
      /api/auth(/|$)(.*)    → identity-service:80
      /api/catalog(/|$)(.*) → catalog-service:80
      /api/orders(/|$)(.*)  → order-service:80
      /                     → frontend-service:80

Namespace: monitoring
  prometheus-stack (kube-prometheus-stack Helm chart)
  grafana-dashboard-configmap.yaml  → pre-loads KubeCart dashboard
```

### Secret management
All sensitive values in `k8s/demo/secret.yaml` are base64-encoded. In a real production environment, these would be managed with Vault, AWS Secrets Manager, or Sealed Secrets.

---

## 8. Monitoring

### Metrics collection

Each .NET service uses `prometheus-net.AspNetCore`:

```csharp
app.UseHttpMetrics();        // request rate, latency, status codes
app.MapMetrics("/metrics");  // Prometheus scrape endpoint
```

Prometheus is configured to scrape `/metrics` from each service pod via the `ServiceMonitor` CRD or static scrape config in `prometheus-values.yaml`.

### Grafana Dashboard — "KubeCart Service Overview"

Located at `k8s/demo/monitoring/grafana-dashboard-configmap.yaml`

Panels:
| Panel | Query |
|---|---|
| Request Rate (all services) | `rate(http_requests_received_total[1m])` |
| Error Rate | `rate(http_requests_received_total{code=~"5.."}[1m])` |
| P99 Latency | `histogram_quantile(0.99, rate(http_request_duration_seconds_bucket[5m]))` |
| Active Requests | `http_requests_in_progress` |

---

## 9. Key Design Decisions

### Minimal API over MVC/Controllers
All three services use ASP.NET Core Minimal API (`app.MapGet/Post/Put/Delete`). This reduces boilerplate, keeps each service file focused, and aligns with modern .NET patterns for small, purpose-built microservices.

### Dapper over Entity Framework Core
Dapper is used for all SQL access. The rationale:
- **Explicit SQL** — no surprise N+1 queries or auto-generated migrations
- **Lightweight** — no DbContext, model tracking, or change detection overhead
- **Educational clarity** — the data access layer is transparent and readable

### Stateless JWT (no refresh tokens)
Keeps auth simple and horizontally scalable. Each service validates JWTs locally without a round-trip to identity-service. The 7-day TTL is acceptable for a shopping app.

### Denormalized order items
`ProductName` and `Price` are copied into `OrderItems` at checkout time. This is intentional — it preserves order history integrity when products are later updated or deleted.

### Cart in database (not session)
Cart items are persisted in `KubeCart_Orders.CartItems`. This allows carts to survive browser refreshes, tab changes, and logout/login cycles without session affinity.

### Two catalog-service replicas
The catalog-service is the most-read service (every product page, every checkout). It is set to 2 replicas in Kubernetes to demonstrate horizontal scaling. Since it is stateless (all state in SQL), no sticky sessions are needed.

### Multi-stage Docker builds
All Dockerfiles use two stages: `build` (full SDK image) and `runtime` (ASP.NET runtime image). Final images are ~200 MB instead of ~600 MB.

### Environment-variable-only configuration
`AppConfig.cs` in each service reads all config from environment variables and throws a descriptive `Exception` at startup if any required variable is missing. There is no fallback to defaults for secrets or connection strings. This fail-fast approach catches misconfiguration immediately rather than at runtime.
