# Stage 1: Base Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Stage 2: SDK Build & Restore
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project files for layer caching
COPY ["apps/AttendanceApi/AttendanceApi.csproj", "apps/AttendanceApi/"]
COPY ["tests/AttendanceApi.UnitTests/AttendanceApi.UnitTests.csproj", "tests/AttendanceApi.UnitTests/"]

RUN dotnet restore "apps/AttendanceApi/AttendanceApi.csproj"

# Copy full source code & build
COPY . .
WORKDIR "/src/apps/AttendanceApi"
RUN dotnet build "AttendanceApi.csproj" -c Release -o /app/build --no-restore

# Stage 3: Publish App
FROM build AS publish
RUN dotnet publish "AttendanceApi.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Stage 4: Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Security: Create non-root user
RUN adduser -D -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "AttendanceApi.dll"]
