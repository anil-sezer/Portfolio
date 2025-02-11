# docker build -f Portfolio.Ui/Dockerfile -t portfolio-ui .
# https://devblogs.microsoft.com/dotnet/improving-multiplatform-container-support/
ARG DOTNET_VERSION=9.0
ARG BUILD_CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS base
LABEL maintainer="Anil Sezer - msnanil@hotmail.com"

ARG BUILDOS
ARG TARGETOS
ARG BUILDARCH
ARG TARGETARCH
ARG BUILDVARIANT
ARG TARGETVARIANT
ARG BUILDPLATFORM
ARG TARGETPLATFORM
RUN echo "Building on ${BUILDPLATFORM}, targeting ${TARGETPLATFORM}"
RUN echo "Building on ${BUILDOS} and ${BUILDARCH} with optional variant ${BUILDVARIANT}"
RUN echo "Targeting ${TARGETOS} and ${TARGETARCH} with optional variant ${TARGETVARIANT}"

# USER $APP_UID # todo: https://devblogs.microsoft.com/dotnet/securing-containers-with-rootless/
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

WORKDIR /src
COPY protos/ protos/
COPY Portfolio.Ui/ Portfolio.Ui/
COPY Portfolio.Grpc/ Portfolio.Grpc/
COPY Portfolio.Domain/ Portfolio.Domain/
COPY Portfolio.Infrastructure/ Portfolio.Infrastructure/
# For preventing this: error NETSDK1152: Found multiple publish output files with the same relative path: /src/Portfolio.Grpc/appsettings.Development.json, /src/Portfolio.Ui/appsettings.Development.json, /src/Portfolio.Grpc/appsettings.json, /src/Portfolio.Ui/appsettings.json. [/src/Polio.Ui/appsettings.json. [/src/Portfolio.Ui/Portfolio.Ui.csproj]
RUN rm -f Portfolio.Grpc/appsettings.json Portfolio.Grpc/appsettings.Development.json

ARG TARGETARCH
ARG BUILD_CONFIGURATION
RUN dotnet restore "Portfolio.Ui/Portfolio.Ui.csproj" --arch ${TARGETARCH}

WORKDIR /src/Portfolio.Ui
RUN dotnet build "Portfolio.Ui.csproj" --arch ${TARGETARCH} --configuration ${BUILD_CONFIGURATION} --output /app/build

FROM build AS publish

ARG TARGETARCH
ARG BUILD_CONFIGURATION
RUN dotnet publish --arch ${TARGETARCH} "Portfolio.Ui.csproj" --configuration ${BUILD_CONFIGURATION} --output /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Portfolio.Ui.dll"]
