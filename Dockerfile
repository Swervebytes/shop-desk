FROM node:22-alpine AS web
WORKDIR /src/web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS api
WORKDIR /src
COPY api/ api/
COPY --from=web /src/api/src/ShopDesk.Api/wwwroot api/src/ShopDesk.Api/wwwroot
RUN dotnet publish api/src/ShopDesk.Api/ShopDesk.Api.csproj -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=api /out ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "ShopDesk.Api.dll"]
