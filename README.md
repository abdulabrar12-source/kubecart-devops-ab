# KubeCart

A production-style microservice e-commerce platform built with .NET 10, React 19, and Kubernetes.

---

## Quick Links

| Document | Description |
|---|---|
| [DESIGN.md](DESIGN.md) | Architecture, API reference, data model, tech decisions |
| [USER_MANUAL.md](USER_MANUAL.md) | End-user guide — shopping, orders, admin panel |

---

## Architecture

```
Browser
  └── Ingress (nginx)
        ├── /api/auth     → identity-service   (.NET 10, port 80)
        ├── /api/catalog  → catalog-service    (.NET 10, port 80, 2 replicas)
        ├── /api/orders   → order-service      (.NET 10, port 80)
        └── /             → frontend           (React 19 + Vite, port 80)

order-service ──HTTP──► catalog-service   (product validation at checkout)

Each service ──► SQL Server
                  ├── KubeCart_Identity
                  ├── KubeCart_Catalog
                  └── KubeCart_Orders
```

---

## Repo Structure

```
kubecart-devops-ab/
│
├── identity-service/          # JWT auth — register, login, token validation
│   ├── Config/AppConfig.cs    # Env var loader (fail-fast)
│   ├── Data/                  # IUserRepository + Dapper SQL impl
│   ├── Health/                # SQL health check
│   ├── Models/                # Request/response DTOs
│   ├── Program.cs             # Minimal API entrypoint
│   ├── Dockerfile
│   └── .env.example
│
├── catalog-service/           # Product catalogue — categories, products, images
│   ├── Config/AppConfig.cs
│   ├── Data/                  # ICatalogRepository + Dapper SQL impl
│   ├── Health/
│   ├── Models/
│   ├── Program.cs
│   ├── Dockerfile
│   └── .env.example
│
├── order-service/             # Cart, checkout, order history
│   ├── Config/AppConfig.cs
│   ├── Data/                  # IOrderRepository + Dapper SQL impl
│   ├── Health/
│   ├── Models/
│   ├── Services/              # CartService, CheckoutService, CatalogClient (HTTP)
│   ├── Program.cs
│   ├── Dockerfile
│   └── .env.example
│
├── ui/                        # React 19 + Vite 8 SPA
│   ├── src/
│   │   ├── api/               # authApi.js, catalogApi.js, ordersApi.js
│   │   ├── components/        # Navbar, CartDrawer, ProductCard, ProtectedRoute
│   │   ├── contexts/          # AuthContext (JWT state)
│   │   └── pages/             # Home, Login, Register, Cart, Checkout, Orders
│   │       └── admin/         # AdminProducts, AdminOrders
│   ├── nginx.conf             # Reverse-proxy template (envsubst at runtime)
│   └── Dockerfile
│
├── k8s/demo/                  # Kubernetes manifests (namespace: demo)
│   ├── namespace.yaml
│   ├── secret.yaml            # DB creds + JWT keys (base64)
│   ├── ingress.yaml           # nginx Ingress routing
│   ├── identity/              # deployment, service, configmap
│   ├── catalog/               # deployment (2 replicas), service, configmap
│   ├── order/                 # deployment, service, configmap
│   ├── frontend/              # deployment, service, configmap
│   └── monitoring/            # prometheus-values.yaml + grafana dashboard
│
├── start-local.sh             # One-shot local dev startup
├── deploy-monitoring.sh       # Helm install for Prometheus + Grafana
├── DESIGN.md                  # Architecture + API reference
└── USER_MANUAL.md             # End-user guide
```

---

## For Developers — Local Setup

### Prerequisites
- Docker Desktop
- .NET 10 SDK
- Node.js 20+

### 1. Start SQL Server

```bash
docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=YourStrong@Pass123 \
  -p 1433:1433 --name kubecart-sqlserver -d \
  mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Configure env files

```bash
cp identity-service/.env.example identity-service/.env
cp catalog-service/.env.example  catalog-service/.env
cp order-service/.env.example    order-service/.env
```

### 3. Start all services

```bash
bash start-local.sh
```

| Service | URL | Health |
|---|---|---|
| Identity | http://localhost:5001 | http://localhost:5001/health |
| Catalog | http://localhost:5002 | http://localhost:5002/health |
| Order | http://localhost:5003 | http://localhost:5003/health |
| UI | http://localhost:5173 | — |

---

## For DevOps — Kubernetes Deployment

### Prerequisites
- Minikube (`minikube start`)
- Helm (`brew install helm`)
- kubectl

### 1. Build images

```bash
docker build -t identity-service:latest ./identity-service
docker build -t catalog-service:latest  ./catalog-service
docker build -t order-service:latest    ./order-service
docker build -t frontend:latest         ./ui
```

### 2. Load into Minikube

```bash
minikube image load identity-service:latest
minikube image load catalog-service:latest
minikube image load order-service:latest
minikube image load frontend:latest
```

### 3. Deploy

```bash
kubectl apply -f k8s/demo/namespace.yaml
kubectl apply -f k8s/demo/secret.yaml
kubectl apply -f k8s/demo/identity/
kubectl apply -f k8s/demo/catalog/
kubectl apply -f k8s/demo/order/
kubectl apply -f k8s/demo/frontend/
kubectl apply -f k8s/demo/ingress.yaml
```

### 4. Access the app

```bash
# Option A — port-forward (no sudo)
kubectl port-forward -n ingress-nginx svc/ingress-nginx-controller 8080:80
# Open: http://localhost:8080

# Option B — Minikube tunnel
echo "$(minikube ip)  kubecart.local" | sudo tee -a /etc/hosts
sudo minikube tunnel
# Open: http://kubecart.local
```

### 5. Verify

```bash
kubectl get pods -n demo          # all pods Running
kubectl get ingress -n demo       # kubecart-ingress has ADDRESS
```

---

## Monitoring (Prometheus + Grafana)

```bash
bash deploy-monitoring.sh
```

```bash
kubectl port-forward -n monitoring svc/kubecart-monitoring-grafana 3000:80
# Open: http://localhost:3000  (admin / admin)
# Dashboard: KubeCart — Service Overview
```

---

## Environment Variables

All services load config exclusively from environment variables. See each service's `.env.example` for the full list. No values are hardcoded.

| Variable | Services | Description |
|---|---|---|
| `DB_HOST` | all | SQL Server host + port e.g. `localhost,1433` |
| `DB_NAME` | all | Database name |
| `DB_USER` | all | SQL login |
| `DB_PASSWORD` | all | SQL password |
| `JWT_SIGNING_KEY` | identity, order | HMAC-SHA256 key for JWT signing/validation |
| `APP_ENCRYPTION_KEY` | identity | AES-256 key for PII encryption |
| `CATALOG_SERVICE_URL` | order | Base URL of catalog-service |
| `ASPNETCORE_URLS` | all | Bind address e.g. `http://+:5001` |

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 10 Minimal API |
| ORM | Dapper + raw SQL |
| Database | SQL Server 2022 |
| Frontend | React 19, Vite 8, React Router v7 |
| Auth | JWT HS256 (7-day expiry) |
| Containers | Docker (multi-stage builds) |
| Orchestration | Kubernetes — Deployments, Services, ConfigMaps, Secrets, Ingress |
| Cluster | Minikube (docker driver) |
| Monitoring | prometheus-net.AspNetCore + kube-prometheus-stack (Helm) |
