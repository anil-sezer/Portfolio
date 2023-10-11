FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Portfolio.WebUi/.", "Portfolio.WebUi/"]
COPY ["Portfolio.Domain/.", "Portfolio.Domain/"]
COPY ["Portfolio.DataAccess/.", "Portfolio.DataAccess/"]
RUN if [ ! -f Portfolio.WebUi/Portfolio.WebUi.csproj ]; then echo "File(s) missing for RESTORE! Check the paths."; exit 1; fi
RUN dotnet restore "Portfolio.WebUi/Portfolio.WebUi.csproj"

COPY . /src/Portfolio.WebUi/
WORKDIR "/src"
RUN if [ ! -f Portfolio.WebUi/Portfolio.WebUi.csproj ]; then echo "File(s) missing BUILD! Check the paths."; exit 1; fi
RUN if [ ! -f Portfolio.WebUi/Program.cs ]; then echo "File(s) missing PROGRAM.CS! Check the paths."; exit 1; fi
RUN dotnet build "Portfolio.WebUi/Portfolio.WebUi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Portfolio.WebUi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Portfolio.WebUi.dll"]
