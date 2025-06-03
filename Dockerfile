FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8090
ENV ASPNETCORE_ENVIRONMENT=Development

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
ARG BUILD_CONFIGURATION=Release
ARG GITHUB_USERNAME
ARG GITHUB_TOKEN

WORKDIR /src
COPY . .

RUN dotnet nuget add source \
    --username $GITHUB_USERNAME \
    --password $GITHUB_TOKEN \
    --store-password-in-clear-text \
    --name github "https://nuget.pkg.github.com/nhanne/index.json"

RUN dotnet restore "./APIGateway.csproj"
RUN dotnet build "./APIGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./APIGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "APIGateway.dll"]