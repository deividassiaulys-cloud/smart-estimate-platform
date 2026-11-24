# Web image (feature 022, T363): Caddy with the built React client in /srv. Build context: client/
#   docker build -t smart-estimate-web:<tag> client/
# The Caddyfile is not part of the image: Compose mounts deploy/Caddyfile, which the developer maintains.

FROM node:22-alpine AS build
WORKDIR /src
# Install first, so the dependency layer is reused while only the code changes.
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM caddy:2.11-alpine AS final
COPY --from=build /src/dist /srv
