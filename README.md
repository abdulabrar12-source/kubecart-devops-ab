# KubeCart — Microservice E-Commerce Platform

A production-style e-commerce platform built with 3 .NET 10 microservices, a React 19 frontend, and deployed on Kubernetes (Minikube).

---

## Architecture

```
Browser
  └── Ingress (nginx)
        ├── /api/auth     → identity-service  (port 80)
        ├── /api/catalog  → catalog-service   (port 80, 2 replicas)
        ├── /api/orders   → order-service     (port 80)
        └── /             → frontend          (React + Vite, port 80)

order-service ──HTTP──► catalog-service   (product lookup at checkout)

Each service ──► SQL Server (host.minikube.internal:1433)
                  ├── KubeCart_Identity
                  ├── KubeCart_Catalog
                  └── KubeCart_Orders
```

---

## Services

| Service | Port | Database | Description |
|---|---|---|---|
| `identity-service` | 5001 | KubeCart_Identity | JWT auth — register / login |
| `catalog-service` | 5002 | KubeCart_Catalog | Products & categories (2 replicas in K8s) |
| `order-service` | 5003 | KubeCart_Orders | Cart, checkout, order history |
| `ui` | 5173 (dev) | — | React 19 + Vite 8 SPA |

---

## Tech Stack

- **Backend:** .NET 10 Minimal API, Dapper, SQL Server
- **Frontend:** React 19, Vite 8, React Router v7
- **Auth:** JWT (HS256, 7-day expiry)
- **Containers:** Docker (multi-stage builds)
- **Orchestration:** Kubernetes — Deployments, Services, ConfigMaps, Secrets, Ingress
- **Cluster:** Minikube (docker driver)
- **Monitoring:** Prometheus (`prometheus-net.AspNetCore`) + Grafana (`kube-prometheus-stack`)

---

## Local Development

### Prerequisites
- Docker Desktop, .NET 10 SDK, Node.js 20+

### Start all services

```bash
# 1. Start SQL Server
docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=YourStrong@Pass123 \
  -p 1433:1433 --name kubecart-sqlserver -d \
  mcr.microsoft.com/mssql/server:2022-latest

# 2. Copy env files
cp identity-service/.env.example identity-service/.env
cp catalog-service/.env.example  catalog-service/.env
cp order-service/.env.example    order-service/.env

# 3. Start everything
bash start-local.sh
```

| Service | URL |
|---|---|
| Identity | http://localhost:5001 |
| Catalog | http://localhost:5002 |
| Order | http://localhost:5003 |
| UI | http://localhost:5173 |

---

## Kubernetes Deployment (Minikube)

### Prerequisites
- Minikube, Helm (`brew install helm`), kubectl

```bash
# 1. Start Minikube
minikube start

# 2. Build & load images
docker build -t identity-service:latest ./identity-service
docker build -t catalog-service:latest  ./catalog-service
docker build -t order-service:latest    ./order-service
docker build -t frontend:latest         ./ui

minikube image load identity-service:latest
minikube image load catalog-service:latest
minikube image load order-service:latest
minikube image load frontend:latest

# 3. Deploy
kubectl apply -f k8s/demo/namespace.yaml
kubectl apply -f k8s/demo/secret.yaml
kubectl apply -f k8s/demo/identity/
kubectl apply -f k8s/demo/catalog/
kubectl apply -f k8s/demo/order/
kubectl apply -f k8s/demo/frontend/
kubectl apply -f k8s/demo/ingress.yaml

# 4. Add hosts entry + tunnel
echo "$(minikube ip)  kubecart.local" | sudo tee -a /etc/hosts
sudo minikube tunnel
```

App: **http://kubecart.local**

---

## Monitoring (Prometheus + Grafana)

```bash
bash deploy-monitoring.sh
```

Grafana at `http://$(minikube ip):32000` — login `admin / admin`  
Dashboard: **KubeCart — Service Overview**

---

## Health Checks

```bash
curl http://localhost:5001/health   # identity
curl http://localhost:5002/health   # catalog
curl http://localhost:5003/health   # order
```

---

## Project Structure

```
kubecart-devops-ab/
├── identity-service/       # .NET 10 — JWT auth
├── catalog-service/        # .NET 10 — products & categories
├── order-service/          # .NET 10 — cart, checkout, orders
├── ui/                     # React 19 + Vite 8 SPA
├── k8s/demo/
│   ├── identity/           # deployment, service, configmap
│   ├── catalog/            # deployment (2 replicas), service, configmap
│   ├── order/              # deployment, service, configmap
│   ├── frontend/           # deployment, service, configmap
│   ├── monitoring/         # prometheus-values.yaml, grafana dashboard
│   ├── namespace.yaml
│   ├── secret.yaml
│   └── ingress.yaml
├── start-local.sh
└── deploy-monitoring.sh
```
