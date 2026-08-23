# Étape 1 — build du front
FROM node:24-alpine AS web
WORKDIR /src
COPY web/package*.json ./
RUN npm ci --ignore-scripts
COPY web/ ./
RUN npm run build

# Étape 2 — publish du serveur
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS server
WORKDIR /src
COPY server/ ./
RUN dotnet publish HouseOs.Api -c Release -o /app

# Étape 3 — image finale : un seul runtime, la PWA servie par le serveur .NET
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=server /app ./
COPY --from=web /src/dist ./wwwroot
EXPOSE 8080
ENTRYPOINT ["dotnet", "HouseOs.Api.dll"]
