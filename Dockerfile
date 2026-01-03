# Stage 1: Build Angular frontend
FROM node:22-alpine AS frontend-build
WORKDIR /app/ClientApp

# Copy package files and install dependencies
COPY src/PraxisNote.Web/ClientApp/package*.json ./
RUN npm ci

# Copy source and build
COPY src/PraxisNote.Web/ClientApp/ ./
RUN npm run build

# Stage 2: Build .NET application
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS backend-build
WORKDIR /src

# Copy project files for restore
COPY src/PraxisNote.Domain/*.csproj ./PraxisNote.Domain/
COPY src/PraxisNote.Application/*.csproj ./PraxisNote.Application/
COPY src/PraxisNote.Infrastructure/*.csproj ./PraxisNote.Infrastructure/
COPY src/PraxisNote.Web/*.csproj ./PraxisNote.Web/

# Restore dependencies (Web project pulls in all dependencies)
RUN dotnet restore PraxisNote.Web/PraxisNote.Web.csproj

# Copy all source code
COPY src/PraxisNote.Domain/ ./PraxisNote.Domain/
COPY src/PraxisNote.Application/ ./PraxisNote.Application/
COPY src/PraxisNote.Infrastructure/ ./PraxisNote.Infrastructure/
COPY src/PraxisNote.Web/ ./PraxisNote.Web/

# Copy built Angular app to wwwroot
COPY --from=frontend-build /app/wwwroot ./PraxisNote.Web/wwwroot/

# Build and publish
RUN dotnet publish PraxisNote.Web/PraxisNote.Web.csproj -c Release -o /app/publish --no-restore

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Create non-root user for security (useradd works on CBL-Mariner/Debian)
RUN useradd --create-home --shell /bin/bash appuser || adduser -D -s /bin/sh appuser

# Copy published app
COPY --from=backend-build /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app
USER appuser

# Cloud Run uses PORT environment variable
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "PraxisNote.Web.dll"]
