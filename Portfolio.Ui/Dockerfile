# docker buildx build -t imgregistry.anil-sezer.com/portfolio-ui:latest -f Portfolio.Ui/Dockerfile --platform linux/amd64,linux/arm64 --push .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8081

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
WORKDIR /src

COPY protos/ protos/
COPY Portfolio.Ui/ Portfolio.Ui/
COPY Portfolio.Grpc/ Portfolio.Grpc/
COPY Portfolio.Domain/ Portfolio.Domain/
COPY Portfolio.Infrastructure/ Portfolio.Infrastructure/
# For preventing this: error NETSDK1152: Found multiple publish output files with the same relative path: /src/Portfolio.Grpc/appsettings.Development.json, /src/Portfolio.Ui/appsettings.Development.json, /src/Portfolio.Grpc/appsettings.json, /src/Portfolio.Ui/appsettings.json. [/src/Polio.Ui/appsettings.json. [/src/Portfolio.Ui/Portfolio.Ui.csproj]
RUN rm -f Portfolio.Grpc/appsettings.json Portfolio.Grpc/appsettings.Development.json

RUN dotnet restore "Portfolio.Ui/Portfolio.Ui.csproj" -a $TARGETARCH

WORKDIR /src/Portfolio.Ui
RUN dotnet build "Portfolio.Ui.csproj" --configuration $BUILD_CONFIGURATION -a $TARGETARCH --output /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Portfolio.Ui.csproj" -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Portfolio.Ui.dll"]
