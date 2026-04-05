# ─── Stage 1: Build ──────────────────────────────────────────────────────────
FROM node:22-alpine AS builder

WORKDIR /app

# Install dependencies first (better layer caching)
COPY package.json package-lock.json ./
RUN npm ci

# Copy source and build
COPY . .

# VITE_API_BASE_URL is intentionally left empty so the browser sends
# relative /api/* requests — nginx proxies them to the backend at runtime.
ARG VITE_API_BASE_URL=""
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build

# ─── Stage 2: Serve ──────────────────────────────────────────────────────────
FROM nginx:1.27-alpine AS runner

# Remove the default nginx site
RUN rm -f /etc/nginx/conf.d/default.conf

# Copy our nginx config template — envsubst replaces
# $IDENTITY_SERVICE_HOST, $CATALOG_SERVICE_HOST, $ORDER_SERVICE_HOST at container start.
COPY nginx.conf /etc/nginx/templates/app.conf.template

# Copy the Vite build output
COPY --from=builder /app/dist /usr/share/nginx/html

# IDENTITY_SERVICE_HOST, CATALOG_SERVICE_HOST, ORDER_SERVICE_HOST are injected
# at runtime via envsubst; each points to its respective K8s service name.
ENV IDENTITY_SERVICE_HOST=localhost:5001
ENV CATALOG_SERVICE_HOST=localhost:5002
ENV ORDER_SERVICE_HOST=localhost:5003

EXPOSE 80

# nginx docker image automatically runs envsubst on /etc/nginx/templates/*.template
# and writes the result to /etc/nginx/conf.d/ before starting nginx.
CMD ["nginx", "-g", "daemon off;"]
