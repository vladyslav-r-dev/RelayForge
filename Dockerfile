FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/RelayForge.Domain/RelayForge.Domain.csproj src/RelayForge.Domain/
COPY src/RelayForge.Application/RelayForge.Application.csproj src/RelayForge.Application/
COPY src/RelayForge.Infrastructure/RelayForge.Infrastructure.csproj src/RelayForge.Infrastructure/
COPY src/RelayForge.Api/RelayForge.Api.csproj src/RelayForge.Api/
RUN dotnet restore src/RelayForge.Api/RelayForge.Api.csproj
COPY src/ src/
RUN dotnet publish src/RelayForge.Api/RelayForge.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "RelayForge.Api.dll"]
