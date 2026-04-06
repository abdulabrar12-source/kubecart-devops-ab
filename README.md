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

### 6. Secrets — Per-Service (Least Privilege)

Three separate Secrets are defined in `k8s/demo/secret.yaml`. Each service only mounts its own Secret:

| Secret | Mounted by | Keys |
|---|---|---|
| `identity-secret` | identity-service | `DB_USER` `DB_PASSWORD` `JWT_SIGNING_KEY` `APP_ENCRYPTION_KEY` |
| `catalog-secret` | catalog-service | `DB_USER` `DB_PASSWORD` `JWT_SIGNING_KEY` |
| `order-secret` | order-service | `DB_USER` `DB_PASSWORD` `JWT_SIGNING_KEY` |

> `JWT_SIGNING_KEY` must be **identical** across all three secrets — catalog and order use it to validate tokens issued by identity. `APP_ENCRYPTION_KEY` is exclusive to identity-service (PII encryption at rest).

```bash
# Verify a secret was created and has the expected keys
kubectl get secret identity-secret -n demo -o jsonpath='{.data}' | python3 -m json.tool

# Confirm a key decodes to a non-empty value
kubectl get secret catalog-secret -n demo \
  -o jsonpath='{.data.JWT_SIGNING_KEY}' | base64 -d

# Check which secrets a running pod actually sees
kubectl exec -n demo deployment/order-service -- printenv | grep -E 'DB_|JWT_'
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

---

## Debug Drills

Real failures encountered during development and deployment, with step-by-step diagnosis and fix.

---

### Drill 1 — Pod CrashLoopBackOff: Missing Secret Key (DevOps)

#### Symptom

```bash
kubectl get pods -n demo
# identity-service-xxx   0/1   CrashLoopBackOff   3   2m
```

```bash
kubectl logs -n demo deployment/identity-service
# Unhandled exception. System.Exception:
#   Required environment variable 'JWT_SIGNING_KEY' is not set.
```

#### Root Cause

`AppConfig.cs` performs a fail-fast check at startup — if any required environment variable is absent or empty it throws immediately. The most common trigger is a **key name mismatch** between `secret.yaml` and the env var name the service expects, e.g. the Secret had `JWT_KEY` but the service reads `JWT_SIGNING_KEY`.

#### Diagnosis

```bash
# 1. Confirm the pod is crash-looping and read the exact error
kubectl logs -n demo deployment/identity-service --previous

# 2. Check what keys are actually present in that service's Secret
#    Each service has its own Secret: identity-secret / catalog-secret / order-secret
kubectl get secret identity-secret -n demo -o jsonpath='{.data}' | python3 -m json.tool

# 3. Decode a specific key to verify the value is non-empty
kubectl get secret identity-secret -n demo \
  -o jsonpath='{.data.JWT_SIGNING_KEY}' | base64 -d
```

#### Fix

```bash
# 1. Edit the Secret to use the correct key name / value
kubectl edit secret kubecart-secrets -n demo
# OR re-apply your corrected secret.yaml
kubectl apply -f k8s/demo/secret.yaml

# 2. Force a rolling restart so the pod re-reads the Secret
kubectl rollout restart deployment/identity-service -n demo

# 3. Confirm the pod comes up healthy
kubectl rollout status deployment/identity-service -n demo
kubectl get pods -n demo
```

#### Prevention

- Keep `.env.example` in sync with `AppConfig.cs` — both are the source of truth for required variable names.
- Run `kubectl describe pod <name> -n demo` before debugging logs; the `Environment` block shows which variables were actually injected.

---

### Drill 2 — Checkout Returns 503: Wrong Catalog Service URL (Developer + DevOps)

#### Symptom

`POST /api/orders/checkout` returns **503 Service Unavailable**.

```bash
kubectl logs -n demo deployment/order-service
# CatalogServiceException: Failed to reach catalog-service.
# System.Net.Http.HttpRequestException: Connection refused
#   (http://catalog-service/api/catalog/products/<id>)
```

The cart loads fine but every checkout attempt fails.

#### Root Cause

`order-service` calls `catalog-service` over HTTP at checkout time to validate product prices (see `Services/CatalogClient.cs`). The base URL is read from `CATALOG_SERVICE_URL`. Inside Kubernetes the correct value is the **cluster-internal DNS name** including namespace:

```
http://catalog-service.demo.svc.cluster.local
```

A common mistake is setting it to `http://catalog-service` (missing namespace suffix) or using a localhost address left over from local development.

#### Diagnosis

```bash
# 1. Read the current value injected into the order-service pod
kubectl exec -n demo deployment/order-service \
  -- printenv CATALOG_SERVICE_URL

# 2. Test connectivity from inside the pod
kubectl exec -n demo deployment/order-service \
  -- wget -qO- http://catalog-service.demo.svc.cluster.local/health

# 3. Verify the catalog-service ClusterIP and port
kubectl get svc catalog-service -n demo
```

#### Fix

```bash
# 1. Update the order-service ConfigMap with the correct URL
kubectl edit configmap order-service-config -n demo
# Set: CATALOG_SERVICE_URL=http://catalog-service.demo.svc.cluster.local

# 2. Rolling restart to pick up the new env var
kubectl rollout restart deployment/order-service -n demo

# 3. Smoke-test checkout
curl -s -X POST http://localhost:8080/api/orders/checkout \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"shippingAddress":"123 Main St"}'
# Expect: 201 Created
```

#### Prevention

- In `k8s/demo/order/configmap.yaml` always use the full DNS form: `http://<service-name>.<namespace>.svc.cluster.local`.
- For local development set `CATALOG_SERVICE_URL=http://localhost:5002` in `order-service/.env`.
- Add a `/health` readiness probe that performs a HEAD request to `CATALOG_SERVICE_URL/health` so a misconfigured URL is caught at pod startup rather than at first checkout.
