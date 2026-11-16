# Nexus Store - ASP.NET Core MVC (.NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY VentasMVC.csproj ./
RUN dotnet restore VentasMVC.csproj
COPY . .
RUN dotnet publish VentasMVC.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 10000
CMD ["sh", "-c", "export ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000}; exec dotnet VentasMVC.dll"]
