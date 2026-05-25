FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TestTask.sln", "."]
COPY ["RouterNode.Api/RouterNode.Api.csproj", "RouterNode.Api/"]
COPY ["RouterNode.Application/RouterNode.Application.csproj", "RouterNode.Application/"]
COPY ["RouterNode.Domain/RouterNode.Domain.csproj", "RouterNode.Domain/"]
COPY ["RouterNode.Infrastructure/RouterNode.Infrastructure.csproj", "RouterNode.Infrastructure/"]
RUN dotnet restore "RouterNode.Api/RouterNode.Api.csproj"
COPY . .
WORKDIR "/src/RouterNode.Api"
RUN dotnet build "./RouterNode.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RouterNode.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RouterNode.Api.dll"]
