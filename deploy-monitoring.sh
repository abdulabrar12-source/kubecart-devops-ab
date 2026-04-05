#!/usr/bin/env bash
# deploy-monitoring.sh
# Installs Prometheus + Grafana via kube-prometheus-stack Helm chart
# and loads the KubeCart Grafana dashboard.
#
# Prerequisites:
#   - minikube running  (minikube start)
#   - helm installed    (brew install helm)
#   - kubectl configured to minikube context
#
# Usage:
#   bash deploy-monitoring.sh

set -euo pipefail

CHART_VERSION="61.3.2"   # kube-prometheus-stack — pin for reproducibility
RELEASE="kubecart-monitoring"
NAMESPACE="monitoring"
VALUES="k8s/demo/monitoring/prometheus-values.yaml"
DASHBOARD_CM="k8s/demo/monitoring/grafana-dashboard-configmap.yaml"

echo "==> Adding prometheus-community Helm repo..."
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts 2>/dev/null || true
helm repo update

echo ""
echo "==> Installing / upgrading kube-prometheus-stack..."
helm upgrade --install "$RELEASE" prometheus-community/kube-prometheus-stack \
  --namespace "$NAMESPACE" \
  --create-namespace \
  --version "$CHART_VERSION" \
  -f "$VALUES" \
  --wait \
  --timeout 5m

echo ""
echo "==> Applying KubeCart Grafana dashboard ConfigMap..."
kubectl apply -f "$DASHBOARD_CM"

echo ""
echo "==> Waiting for Grafana to be ready..."
kubectl rollout status deployment/"${RELEASE}-grafana" -n "$NAMESPACE" --timeout=120s

echo ""
echo "==> Done! Access Grafana:"
echo ""
echo "    minikube service ${RELEASE}-grafana -n ${NAMESPACE}"
echo ""
echo "    Or via NodePort: http://\$(minikube ip):32000"
echo "    Username: admin"
echo "    Password: admin"
echo ""
echo "    The 'KubeCart — Service Overview' dashboard will appear under"
echo "    Dashboards > Browse after Prometheus scrapes the first data points (~30 s)."
