# Haunted Inventory - Multi-stage Docker build
# Stage 1: Restore
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Copy project files and restore dependencies
COPY src/HauntedInventory.Web/HauntedInventory.Web.csproj src/HauntedInventory.Web/
COPY tests/HauntedInventory.Tests/HauntedInventory.Tests.csproj tests/HauntedInventory.Tests/
RUN dotnet restore "src/HauntedInventory.Web/HauntedInventory.Web.csproj"
RUN dotnet restore "tests/HauntedInventory.Tests/HauntedInventory.Tests.csproj"

# Stage 2: Build
FROM restore AS build
WORKDIR /src

# Copy all source files
COPY src/HauntedInventory.Web/ src/HauntedInventory.Web/
COPY tests/HauntedInventory.Tests/ tests/HauntedInventory.Tests/
COPY HauntedInventory.slnx ./

# Build the application
RUN dotnet build "src/HauntedInventory.Web/HauntedInventory.Web.csproj" -c Release -o /app/build

# Build tests (optional - for verification)
RUN dotnet build "tests/HauntedInventory.Tests/HauntedInventory.Tests.csproj" -c Release

# Stage 3: Publish
FROM build AS publish
WORKDIR /src

RUN dotnet publish "src/HauntedInventory.Web/HauntedInventory.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published files; the aspnet image ships a non-root 'app' user (UID 1654)
COPY --from=publish /app/publish .

# SQLite database lives in App_Data (mount a volume here to persist data)
RUN mkdir -p /app/App_Data && chown -R app:app /app/App_Data
VOLUME /app/App_Data

USER app

# Expose port
EXPOSE 8080

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "HauntedInventory.Web.dll"]