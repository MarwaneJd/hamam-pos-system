# ============================================
# Stage 1: Build React app
# ============================================
FROM node:18-alpine AS build
WORKDIR /app

# Copy package files and install dependencies (layer caching)
COPY package.json package-lock.json ./
RUN npm ci --silent

# Copy source code and build
COPY . .
RUN npm run build

# ============================================
# Stage 2: Serve with Nginx
# ============================================
FROM nginx:alpine AS runtime

# Remove default nginx config
RUN rm /etc/nginx/conf.d/default.conf

# Copy custom nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built React app
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
