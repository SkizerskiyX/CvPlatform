FROM node:22-alpine AS spa
WORKDIR /build-web
COPY CvPlatform.Web/package*.json ./
RUN npm install --no-audit --no-fund
COPY CvPlatform.Web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV SpaBuildEnabled=false
WORKDIR /src
COPY ["CvPlatform.API/CvPlatform.API.csproj", "CvPlatform.API/"]
COPY ["CvPlatform.Application/CvPlatform.Application.csproj", "CvPlatform.Application/"]
COPY ["CvPlatform.Infrastructure/CvPlatform.Infrastructure.csproj", "CvPlatform.Infrastructure/"]
COPY ["CvPlatform.Domain/CvPlatform.Domain.csproj", "CvPlatform.Domain/"]
RUN dotnet restore "./CvPlatform.API/CvPlatform.API.csproj"
COPY . .
COPY --from=spa /build-web/wwwroot ./CvPlatform.API/wwwroot
WORKDIR "/src/CvPlatform.API"
RUN dotnet build "./CvPlatform.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CvPlatform.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=spa /build-web/wwwroot ./wwwroot
ENTRYPOINT ["dotnet", "CvPlatform.API.dll"]