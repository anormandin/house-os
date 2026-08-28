# Étape 1 : build du frontend (Vite → dist)
FROM node:26-alpine AS web
WORKDIR /src/web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

# Étape 2 : publish de l'API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY server/HouseOs.sln server/
COPY server/HouseOs.Api/HouseOs.Api.csproj server/HouseOs.Api/
COPY server/HouseOs.Tests/HouseOs.Tests.csproj server/HouseOs.Tests/
RUN dotnet restore server/HouseOs.Api
COPY server/ server/
RUN dotnet publish server/HouseOs.Api -c Release --no-restore -o /app/publish

# Étape 3 : image finale — base Debian : tzdata inclus, requis parce que le code
# vit en heure locale (TZ=America/Toronto passé par docker-compose).
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# curl : uniquement pour le healthcheck du compose (l'image aspnet n'en a pas).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=api /app/publish ./
COPY --from=web /src/web/dist ./wwwroot
# Conteneur non-root ($APP_UID fourni par l'image aspnet). Le dossier des fichiers
# doit appartenir à cet utilisateur pour que l'upload marche : un volume VIERGE
# hérite de ces permissions à sa création ; un volume existant (prod) exige un
# chown une fois :
#   docker run --rm -v house-os_fichiers:/f mcr.microsoft.com/dotnet/aspnet:10.0 chown -R 1654:1654 /f
RUN mkdir -p /app/donnees/fichiers && chown -R $APP_UID:$APP_UID /app/donnees
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "HouseOs.Api.dll"]
