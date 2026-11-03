# =============================================================
# Stage 1: Build React/Vite SPA bundle
# =============================================================
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies
COPY package*.json ./
RUN npm install

# Copy source code and build production distribution
COPY . ./

# Allow overriding API base URL at build time (defaults to localhost:5175)
ARG VITE_API_BASE_URL=http://localhost:5175
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build

# =============================================================
# Stage 2: Serve with Nginx
# =============================================================
FROM nginx:alpine AS final

# Clean default Nginx html files
RUN rm -rf /usr/share/nginx/html/*

# Copy build artifacts to Nginx server directory
COPY --from=build /app/dist /usr/share/nginx/html

# Copy custom Nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]

