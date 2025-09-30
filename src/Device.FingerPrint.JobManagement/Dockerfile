# Multi-stage build for FPS AI-Powered Device Fingerprint JobManagement
# Stage 1: Build
FROM hub.nexdev.net/base/dotnet/core/sdk:9.0-alpine AS build

WORKDIR /src

# Install curl for health checks (if needed)
# Create non-root user for security
RUN apk add --no-cache curl \
&& addgroup -g 1001 -S appgroup \
&& adduser -S appuser -u 1001 -G appgroup

# Copy project files for dependency restoration
COPY ["src/Device.FingerPrint.JobManagement/Device.FingerPrint.JobManagement.csproj", "Device.FingerPrint.JobManagement/"]

# Add custom NuGet source
RUN dotnet nuget add source https://nuget.nexdev.net/api/v2

# Restore dependencies
WORKDIR "/src/Device.FingerPrint.JobManagement"
RUN dotnet restore "Device.FingerPrint.JobManagement.csproj" -v n

# Copy source code
COPY ["src/Device.FingerPrint.JobManagement/", "."]

# Build and publish the application
RUN dotnet publish "Device.FingerPrint.JobManagement.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    --verbosity normal

# Stage 2: Runtime
FROM hub.nexdev.net/base/dotnet/core/aspnet:9.0-alpine AS runtime

WORKDIR /app

# Install necessary packages for runtime
RUN apk add --no-cache \
    ca-certificates \
    curl \
    tzdata \
&& addgroup -g 1001 -S appgroup \
&& adduser -S appuser -u 1001 -G appgroup

# Copy published application
COPY --from=build /app/publish .

# Set ownership to non-root user
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Environment variables
ENV TZ="America/La_Paz"

# Entry point
ENTRYPOINT ["dotnet", "Device.FingerPrint.JobManagement.dll"]