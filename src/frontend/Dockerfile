# syntax=docker/dockerfile:1

FROM node:24-alpine AS build
WORKDIR /app

COPY src/frontend/package*.json ./
RUN npm ci --ignore-scripts

COPY src/frontend/ ./
ARG VITE_STRIPE_PUBLISHABLE_KEY=""
ENV VITE_STRIPE_PUBLISHABLE_KEY=${VITE_STRIPE_PUBLISHABLE_KEY}
RUN npm run build

FROM nginx:1.27-alpine AS runtime

ENV API_UPSTREAM=http://api:8080
EXPOSE 8080

COPY src/frontend/nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
