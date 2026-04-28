# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build
WORKDIR /src

COPY . .
RUN dotnet publish src/CaptionGen.Api/CaptionGen.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:UseSharedCompilation=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

RUN addgroup -S captiongen && adduser -S captiongen -G captiongen
COPY --from=build /app/publish .
RUN mkdir -p /app/media && chown -R captiongen:captiongen /app

USER captiongen
ENTRYPOINT ["dotnet", "CaptionGen.Api.dll"]
