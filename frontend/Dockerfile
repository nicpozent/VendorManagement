# syntax=docker/dockerfile:1
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
# Build-time public config (baked into the bundle). Empty => dev-auth fallback.
ARG VITE_ENTRA_CLIENT_ID=
ARG VITE_ENTRA_TENANT_ID=
ARG VITE_API_SCOPE=
ARG VITE_API_BASE=/api
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
