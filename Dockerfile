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
WORKDIR /app
COPY --from=api /app/publish ./
COPY --from=web /src/web/dist ./wwwroot
EXPOSE 8080
ENTRYPOINT ["dotnet", "HouseOs.Api.dll"]
