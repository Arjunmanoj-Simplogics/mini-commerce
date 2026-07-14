# syntax=docker/dockerfile:1.7
# React storefront — Node LTS build + unprivileged Nginx.
# Context: frontend/
#   docker build -f deploy/docker/Frontend.Dockerfile -t minicommerce/frontend:latest ./frontend

# ---- deps (cached while package-lock.json unchanged) ----
FROM node:lts-alpine AS deps
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts

# ---- production Vite build ----
FROM deps AS build
WORKDIR /app

COPY . .

# Public API base URLs are baked in at build time (override with --build-arg)
ARG VITE_API_BASE_URL=http://localhost:8080
ARG VITE_INVENTORY_API_BASE_URL=http://localhost:8081
ARG VITE_NOTIFICATION_API_BASE_URL=http://localhost:8082
ARG VITE_AUTH_API_BASE_URL=http://localhost:8083
ARG VITE_CATALOG_API_BASE_URL=http://localhost:8084
ARG VITE_CART_API_BASE_URL=http://localhost:8085
ARG VITE_PAYMENT_API_BASE_URL=http://localhost:8086

ENV VITE_API_BASE_URL=$VITE_API_BASE_URL \
    VITE_INVENTORY_API_BASE_URL=$VITE_INVENTORY_API_BASE_URL \
    VITE_NOTIFICATION_API_BASE_URL=$VITE_NOTIFICATION_API_BASE_URL \
    VITE_AUTH_API_BASE_URL=$VITE_AUTH_API_BASE_URL \
    VITE_CATALOG_API_BASE_URL=$VITE_CATALOG_API_BASE_URL \
    VITE_CART_API_BASE_URL=$VITE_CART_API_BASE_URL \
    VITE_PAYMENT_API_BASE_URL=$VITE_PAYMENT_API_BASE_URL \
    NODE_ENV=production

RUN npm run build

# ---- Nginx (non-root, SPA + gzip) ----
FROM nginxinc/nginx-unprivileged:1.27-alpine AS final

# Prefer deploy artifact when present; fall back to frontend/nginx.conf
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

ENV NGINX_ENTRYPOINT_QUIET_LOGS=1

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/healthz || exit 1

CMD ["nginx", "-g", "daemon off;"]
