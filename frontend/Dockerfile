# Builds the app, then serves it with nginx, which also passes /api on to the API container.

FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# The app calls the API on its own origin, through nginx, so the browser needs no CORS and no second address.
ENV VITE_API_URL=/api
RUN npm run build

FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
