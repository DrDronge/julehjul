# Frontend image for Svelte + Vite
FROM node:22-alpine AS builder
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci --silent
COPY . .
EXPOSE 5173
CMD ["npm", "run", "dev"]
