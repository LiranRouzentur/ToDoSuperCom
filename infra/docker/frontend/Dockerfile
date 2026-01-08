FROM node:20 AS build
WORKDIR /app

# Copy package files
COPY frontend/app/package*.json ./

# Install dependencies
RUN npm ci

# Copy source
COPY frontend/app ./

# Build
RUN npm run build

# Production image with nginx
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html

# Optional: custom nginx config
# COPY infra/docker/frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

