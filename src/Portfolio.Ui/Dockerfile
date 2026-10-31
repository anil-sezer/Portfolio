# docker buildx build -t imgregistry.anil-sezer.com/portfolio-ui:latest -f src/Portfolio.Ui/Dockerfile --platform linux/amd64,linux/arm64 --push .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
WORKDIR /app

# Copy dependency specifications first to leverage Docker layer caching for restore
COPY Directory.Packages.props ./
COPY src/Portfolio.Domain/Portfolio.Domain.csproj src/Portfolio.Domain/
COPY src/Portfolio.Infrastructure/Portfolio.Infrastructure.csproj src/Portfolio.Infrastructure/
COPY src/Portfolio.Ui/Portfolio.Ui.csproj src/Portfolio.Ui/

RUN dotnet restore "src/Portfolio.Ui/Portfolio.Ui.csproj" -a $TARGETARCH

# Copy protobufs and project sources after restore
COPY protos/ protos/
COPY src/Portfolio.Domain/ src/Portfolio.Domain/
COPY src/Portfolio.Infrastructure/ src/Portfolio.Infrastructure/
COPY src/Portfolio.Ui/ src/Portfolio.Ui/

WORKDIR /app/src/Portfolio.Ui
RUN dotnet build "Portfolio.Ui.csproj" --configuration $BUILD_CONFIGURATION -a $TARGETARCH --no-restore --output /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Portfolio.Ui.csproj" -c $BUILD_CONFIGURATION -a $TARGETARCH --no-restore -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Portfolio.Ui.dll"]
