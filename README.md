# 🚀 KubeCart DevOps Project

## 📌 Overview

This project demonstrates a production-style deployment of a full-stack Todo application using Kubernetes (Minikube).

It includes:
- **Frontend:** React (Vite)
- **Backend:** .NET Web API
- **Database:** SQL Server (running externally on Docker)
- **Infrastructure:** Kubernetes with Deployments, Services, ConfigMaps, Secrets, and Ingress

---

## 🏗️ Architecture

```
Browser → Ingress → UI → API → SQL Server (External)
```

---

## 🛠️ Tech Stack

- React (Vite)
- .NET Web API
- Kubernetes (Minikube)
- Docker
- SQL Server
- Prometheus & Grafana

---

## ⚙️ Kubernetes Setup

- **Namespace:** `demo`
- **API Deployment:** 2 replicas
- **UI Deployment:** 1 replica
- **Services:**
  - `api-service` (ClusterIP)
  - `ui-service` (ClusterIP / NodePort)
- **ConfigMap:** Stores `DB_HOST` and configuration
- **Secret:** Stores database password
- **Ingress:**
  - `/` → UI
  - `/api` → API

---

## 🌐 Access

Run:

```bash
sudo minikube tunnel
```

Open: **http://127.0.0.1**

> Ingress is exposed using Minikube tunnel due to macOS Docker driver limitations.

---

## 🚀 How to Run

**1. Clone repo:**
```bash
git clone <your-repo-url>
cd kubecart-devops
```

**2. Run SQL Server:**
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Pass123" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

**3. Build images:**
```bash
docker build -t api-image ./Api
docker build -t ui-image ./ui
```

**4. Load into Minikube:**
```bash
minikube image load api-image
minikube image load ui-image
```

**5. Deploy:**
```bash
kubectl apply -f k8s/
```

**6. Start tunnel:**
```bash
sudo minikube tunnel
```

---

## 🎯 Features

- ➕ Add Todo
- 🗑️ Delete Todo
- ✔️ Mark Completed
- 🔘 Filter (All / Completed / Pending)

---

## 🐛 Debugging Scenarios

### 1. DB_HOST Issue
- `localhost` failed inside pod — containers cannot reach the host via localhost
- **Fixed** by using `host.minikube.internal` in the ConfigMap

### 2. Secret Password Issue
- Wrong password caused SQL Server connection failure on startup
- **Fixed** by updating the Kubernetes Secret with the correct base64-encoded value

### 3. Frontend Build Error (Vite crash)
- A `multi_replace` operation accidentally dropped the `export async function createTodo(title) {` declaration in `todoApi.js`, leaving the function body as loose top-level code
- **Error:** `A 'return' statement can only be used within a function body`
- **Fixed** by restoring the missing function declaration

### 4. Stale image in Minikube (UI & API)
- Rebuilding and reloading `image:latest` did **not** update the running pod — minikube cached the old digest
- **Symptom:** Checkbox PUT requests returned `HTTP 404`; new UI features were missing from the bundle
- **Fixed** by using explicit version tags (`api-image:v2`, `ui-image:v2`) and updating the manifests

---

## 📊 Monitoring

- Installed **Prometheus & Grafana** via `kube-prometheus-stack` Helm chart
- Pre-provisioned dashboard (`k8s/monitoring/values.yaml`) includes:
  - CPU usage per pod
  - Memory usage per pod
  - Total pod count
  - Container restarts
  - Network traffic (received & transmitted)
  - Node CPU usage %
  - Node memory usage %

> Some pod-level metrics may show "No Data" due to Minikube resource limitations.

---

## 📸 Screenshots

Available in: `docs/screenshots/`

Includes:
- Kubernetes pods & services
- Ingress configuration
- Application UI
- Grafana dashboard

---

## 🧠 Key Learnings

- Kubernetes architecture (Pods, Services, Ingress)
- ConfigMap vs Secret
- Why `localhost` fails inside pods
- Debugging with `kubectl logs` and `kubectl exec`
- Minikube image caching gotchas with `imagePullPolicy: Never`
- Monitoring using Prometheus & Grafana

---

## 👨‍💻 Author

**Abdul Abrar**