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
# Le Chromium de Playwright, à la révision exacte du paquet NuGet, dans un dossier
# copié tel quel dans l'image finale (D-2026-09-03 Rendu E-ink Par Chromium Headless).
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN dotnet exec --runtimeconfig /app/publish/HouseOs.Api.runtimeconfig.json /app/publish/Microsoft.Playwright.dll install chromium

# Étape 3 : image finale — base Debian : tzdata inclus, requis parce que le code
# vit en heure locale (TZ=America/Toronto passé par docker-compose).
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /app/publish ./
COPY --from=web /src/web/dist ./wwwroot
# curl : uniquement pour le healthcheck du compose (l'image aspnet n'en a pas).
# Les bibliothèques de Chromium (install-deps = la liste que Playwright maintient)
# et une police de repli pour les glyphes que Nunito n'a pas (« ✓ »).
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl fonts-dejavu-core \
    && dotnet exec --runtimeconfig /app/HouseOs.Api.runtimeconfig.json /app/Microsoft.Playwright.dll install-deps chromium \
    && rm -rf /var/lib/apt/lists/*
COPY --from=api /ms-playwright /ms-playwright
# Le node embarqué par Playwright perd son bit d'exécution au publish : sans lui,
# l'utilisateur non-root ne peut pas lancer le pilote.
RUN chmod -R a+rx /app/.playwright/node
# Conteneur non-root ($APP_UID fourni par l'image aspnet). Le dossier des fichiers
# doit appartenir à cet utilisateur pour que l'upload marche : un volume VIERGE
# hérite de ces permissions à sa création ; un volume existant (prod) exige un
# chown une fois :
#   docker run --rm -v house-os_fichiers:/f mcr.microsoft.com/dotnet/aspnet:10.0 chown -R 1654:1654 /f
# donnees/journal : tampon disque du sink Seq (pas de volume — un tampon perdu au
# rebuild n'est qu'une poignée d'évènements, et un volume de plus à gérer coûte plus).
RUN mkdir -p /app/donnees/fichiers /app/donnees/journal /app/donnees/protection \
    && chown -R $APP_UID:$APP_UID /app/donnees
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "HouseOs.Api.dll"]
