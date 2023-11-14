FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Portfolio.Web.Api/Portfolio.Web.Api.csproj", "Portfolio.Web.Api/"]
COPY ["Portfolio.Domain/Portfolio.Domain.csproj", "Portfolio.Domain/"]
COPY ["Portfolio.DataAccess/Portfolio.DataAccess.csproj", "Portfolio.DataAccess/"]
RUN dotnet restore "Portfolio.Web.Api/Portfolio.Web.Api.csproj"

COPY . .
WORKDIR "/src/Portfolio.Web.Api"
RUN dotnet build "Portfolio.Web.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Portfolio.Web.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Portfolio.Web.Api.dll"]
