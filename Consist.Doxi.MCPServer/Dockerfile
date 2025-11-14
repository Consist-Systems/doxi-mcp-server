# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Consist.Doxi.MCPServer/Consist.Doxi.MCPServer.csproj", "Consist.Doxi.MCPServer/"]
COPY ["Consist.Doxi.MCPServer.Domain/Consist.Doxi.MCPServer.Domain.csproj", "Consist.Doxi.MCPServer.Domain/"]
RUN dotnet restore "Consist.Doxi.MCPServer/Consist.Doxi.MCPServer.csproj"
COPY . .
WORKDIR /src/Consist.Doxi.MCPServer

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Consist.Doxi.MCPServer.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false

FROM base AS final
ARG APP_UID=0
RUN if [ "${APP_UID}" != "0" ]; then \
      useradd --create-home --shell /usr/sbin/nologin --uid "${APP_UID}" appuser; \
    fi
WORKDIR /app
COPY --from=publish /app/publish .
USER ${APP_UID}
ENTRYPOINT ["dotnet", "Consist.Doxi.MCPServer.dll"]
