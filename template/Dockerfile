# Crypt Application - Multi-stage Docker build
# Stage 1: Restore
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Copy project files and restore dependencies
COPY src/CryptApp.Web/CryptApp.Web.csproj src/CryptApp.Web/
COPY tests/CryptApp.Tests/CryptApp.Tests.csproj tests/CryptApp.Tests/
RUN dotnet restore "src/CryptApp.Web/CryptApp.Web.csproj"
RUN dotnet restore "tests/CryptApp.Tests/CryptApp.Tests.csproj"

# Stage 2: Build
FROM restore AS build
WORKDIR /src

# Copy all source files
COPY src/CryptApp.Web/ src/CryptApp.Web/
COPY tests/CryptApp.Tests/ tests/CryptApp.Tests/
COPY CryptApp.slnx ./

# Build the application
RUN dotnet build "src/CryptApp.Web/CryptApp.Web.csproj" -c Release -o /app/build

# Build tests (optional - for verification)
RUN dotnet build "tests/CryptApp.Tests/CryptApp.Tests.csproj" -c Release

# Stage 3: Publish
FROM build AS publish
WORKDIR /src

RUN dotnet publish "src/CryptApp.Web/CryptApp.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r app && useradd -r -g app app

# Copy published files
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R app:app /app

# Switch to non-root user
USER app

# Expose port
EXPOSE 8080

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "CryptApp.Web.dll"]