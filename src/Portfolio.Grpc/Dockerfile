# docker buildx build -t imgregistry.anil-sezer.com/portfolio-grpc:latest -f src/Portfolio.Grpc/Dockerfile --platform linux/amd64,linux/arm64 --push .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
WORKDIR /app

COPY Directory.Packages.props ./
COPY protos/ protos/
COPY src/Portfolio.Grpc/ src/Portfolio.Grpc/
COPY src/Portfolio.Domain/ src/Portfolio.Domain/
COPY src/Portfolio.Infrastructure/ src/Portfolio.Infrastructure/

RUN dotnet restore "src/Portfolio.Grpc/Portfolio.Grpc.csproj" -a $TARGETARCH

WORKDIR /app/src/Portfolio.Grpc
RUN dotnet build "Portfolio.Grpc.csproj" --configuration $BUILD_CONFIGURATION -a $TARGETARCH --output /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Portfolio.Grpc.csproj" -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Portfolio.Grpc.dll"]
