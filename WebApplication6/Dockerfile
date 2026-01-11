# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY WebApplication6.csproj ./
RUN dotnet restore ./WebApplication6.csproj

# Copy everything else and publish
COPY . ./
RUN dotnet publish ./WebApplication6.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Fly.io (and many PaaS) route traffic to 8080 by default
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "WebApplication6.dll"]
