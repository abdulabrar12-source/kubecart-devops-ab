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

Two intentionally broken states introduced, diagnosed, and fixed during deployment.

---

### Drill 1 — localhost DB Bug (Developer → DevOps)

#### The Broken State

A developer copied their local `.env` file directly into `k8s/demo/identity/configmap.yaml`, leaving:

```yaml
# k8s/demo/identity/configmap.yaml  ← BROKEN
data:
  DB_HOST: "localhost,1433"   # ← works on laptop, fatal inside a pod
  DB_NAME: "KubeCart_Identity"
```

#### Symptom

```bash
kubectl get pods -n demo
# identity-service-xxx   0/1   CrashLoopBackOff   4   3m

kubectl logs -n demo deployment/identity-service --previous
# Unhandled exception. Microsoft.Data.SqlClient.SqlException:
#   A network-related or instance-specific error occurred while
#   establishing a connection to SQL Server.
#   (provider: TCP Provider, error: 40 - Could not open connection)
#   Server: localhost,1433
```

#### Why It Breaks

Inside a Kubernetes pod `localhost` resolves to the **pod's own loopback interface** — not the host machine. SQL Server is running on the host (Mac), not inside the pod. The correct address for reaching the host from a Minikube pod is `host.minikube.internal,1433`.

#### Diagnosis

```bash
# 1. Read what DB_HOST the running pod actually sees
kubectl exec -n demo deployment/identity-service -- printenv DB_HOST
# Output: localhost,1433   ← the bug

# 2. Confirm SQL Server is unreachable at that address from inside the pod
kubectl exec -n demo deployment/identity-service \
  -- sh -c "nc -zv localhost 1433 2>&1 || echo 'UNREACHABLE'"

# 3. Verify the correct host is reachable
kubectl exec -n demo deployment/identity-service \
  -- sh -c "nc -zv host.minikube.internal 1433 2>&1 || echo 'UNREACHABLE'"
```

#### Fix

```bash
# 1. Patch the configmap with the correct host
kubectl patch configmap identity-configmap -n demo \
  --type merge -p '{"data":{"DB_HOST":"host.minikube.internal,1433"}}'

# Apply the same fix to catalog and order configmaps
kubectl patch configmap catalog-configmap -n demo \
  --type merge -p '{"data":{"DB_HOST":"host.minikube.internal,1433"}}'
kubectl patch configmap order-configmap -n demo \
  --type merge -p '{"data":{"DB_HOST":"host.minikube.internal,1433"}}'

# 2. Rolling restart to pick up the new value
kubectl rollout restart deployment/identity-service \
  deployment/catalog-service deployment/order-service -n demo

# 3. Confirm pods are Running and healthy
kubectl get pods -n demo
kubectl logs -n demo deployment/identity-service | tail -5
# Should see: "Application started. Press Ctrl+C to shut down."
```

#### What the configmap looks like after the fix

```yaml
# k8s/demo/identity/configmap.yaml  ← FIXED
data:
  DB_HOST: "host.minikube.internal,1433"
  DB_NAME: "KubeCart_Identity"
```

#### Prevention

- Never copy a local `.env` file into a K8s ConfigMap without reviewing every value.
- `localhost` inside a pod always means the pod itself. Use `host.minikube.internal` for the Docker host (Minikube), or the service's ClusterIP DNS name for another in-cluster service.
- Add `kubectl exec -- printenv` as a post-deploy smoke test in your runbook.

---

### Drill 2 — Wrong Password Secret Bug (DevOps)

#### The Broken State

The `DB_PASSWORD` in `k8s/demo/secret.yaml` was base64-encoded with a trailing newline (a common `echo` mistake), producing a password value with a `\n` character that SQL Server rejects:

```bash
# WRONG — echo adds \n before base64 encoding
echo 'YourStrong@Pass123' | base64
# WW91clN0cm9uZ0BQYXNzMTIzCg==   ← note the trailing 'Cg==' (the \n)

# CORRECT — echo -n suppresses the newline
echo -n 'YourStrong@Pass123' | base64
# WW91clN0cm9uZ0BQYXNzMTIz
```

The secret was stored with `WW91clN0cm9uZ0BQYXNzMTIzCg==` instead of `WW91clN0cm9uZ0BQYXNzMTIz`.

#### Symptom

```bash
kubectl get pods -n demo
# identity-service-xxx   0/1   CrashLoopBackOff   3   2m

kubectl logs -n demo deployment/identity-service --previous
# Unhandled exception. Microsoft.Data.SqlClient.SqlException:
#   Login failed for user 'sa'.
#   (Microsoft SQL Server, Error: 18456)
```

The pod starts, reaches SQL Server successfully (no TCP error), but authentication fails.

#### Diagnosis

```bash
# 1. Decode the stored password and pipe through cat -A to reveal hidden characters
kubectl get secret identity-secret -n demo \
  -o jsonpath='{.data.DB_PASSWORD}' | base64 -d | cat -A
# Output: YourStrong@Pass123^M$   ← ^M or $ means trailing whitespace/newline

# 2. Check the length — correct password is 18 chars, broken one is 19
kubectl get secret identity-secret -n demo \
  -o jsonpath='{.data.DB_PASSWORD}' | base64 -d | wc -c
# 19   ← should be 18

# 3. Confirm SQL Server itself accepts the correct password from the host
sqlcmd -S localhost,1433 -U sa -P 'YourStrong@Pass123' -Q "SELECT 1"
# If this succeeds, the problem is purely in the Secret value
```

#### Fix

```bash
# 1. Re-encode the password correctly (no trailing newline)
CORRECT_B64=$(echo -n 'YourStrong@Pass123' | base64)
echo $CORRECT_B64
# WW91clN0cm9uZ0BQYXNzMTIz

# 2. Patch all three per-service secrets
for secret in identity-secret catalog-secret order-secret; do
  kubectl patch secret $secret -n demo \
    --type merge -p "{\"data\":{\"DB_PASSWORD\":\"${CORRECT_B64}\"}}"
done

# 3. Rolling restart
kubectl rollout restart deployment/identity-service \
  deployment/catalog-service deployment/order-service -n demo

# 4. Verify login now succeeds
kubectl logs -n demo deployment/identity-service | grep -i "started\|error"
# Expect: "Application started. Press Ctrl+C to shut down."
```

#### What the secret value looks like after the fix

```yaml
# k8s/demo/secret.yaml  ← FIXED
data:
  DB_PASSWORD: WW91clN0cm9uZ0BQYXNzMTIz   # echo -n 'YourStrong@Pass123' | base64
```

#### Prevention

- **Always use `echo -n`** (no newline) when base64-encoding secret values.
- After applying a Secret, decode and length-check every sensitive value: `kubectl get secret <name> -n demo -o jsonpath='{.data.<KEY>}' | base64 -d | wc -c`
- Add a comment next to every base64 value in `secret.yaml` showing the exact `echo -n '...' | base64` command used to generate it — this repo already follows that convention.
