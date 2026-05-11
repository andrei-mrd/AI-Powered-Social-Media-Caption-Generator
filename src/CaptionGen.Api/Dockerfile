# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build
WORKDIR /src

COPY CaptionGen.sln ./
COPY src/CaptionGen.Api/CaptionGen.Api.csproj src/CaptionGen.Api/
COPY src/CaptionGen.Application/CaptionGen.Application.csproj src/CaptionGen.Application/
COPY src/CaptionGen.Domain/CaptionGen.Domain.csproj src/CaptionGen.Domain/
COPY src/CaptionGen.Infrastructure/CaptionGen.Infrastructure.csproj src/CaptionGen.Infrastructure/
RUN dotnet restore src/CaptionGen.Api/CaptionGen.Api.csproj

COPY src/CaptionGen.Api/ src/CaptionGen.Api/
COPY src/CaptionGen.Application/ src/CaptionGen.Application/
COPY src/CaptionGen.Domain/ src/CaptionGen.Domain/
COPY src/CaptionGen.Infrastructure/ src/CaptionGen.Infrastructure/
RUN dotnet publish src/CaptionGen.Api/CaptionGen.Api.csproj \
    -c Release \
    --no-restore \
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
