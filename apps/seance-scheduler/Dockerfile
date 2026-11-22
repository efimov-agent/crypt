# Seance Scheduler - Multi-stage Docker build
# Stage 1: Restore
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Copy project files and restore dependencies
COPY src/SeanceScheduler.Web/SeanceScheduler.Web.csproj src/SeanceScheduler.Web/
COPY tests/SeanceScheduler.Tests/SeanceScheduler.Tests.csproj tests/SeanceScheduler.Tests/
RUN dotnet restore "src/SeanceScheduler.Web/SeanceScheduler.Web.csproj"
RUN dotnet restore "tests/SeanceScheduler.Tests/SeanceScheduler.Tests.csproj"

# Stage 2: Build
FROM restore AS build
WORKDIR /src

# Copy all source files
COPY src/SeanceScheduler.Web/ src/SeanceScheduler.Web/
COPY tests/SeanceScheduler.Tests/ tests/SeanceScheduler.Tests/
COPY SeanceScheduler.slnx ./

# Build the application
RUN dotnet build "src/SeanceScheduler.Web/SeanceScheduler.Web.csproj" -c Release -o /app/build

# Build tests (optional - for verification)
RUN dotnet build "tests/SeanceScheduler.Tests/SeanceScheduler.Tests.csproj" -c Release

# Stage 3: Publish
FROM build AS publish
WORKDIR /src

RUN dotnet publish "src/SeanceScheduler.Web/SeanceScheduler.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

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
ENTRYPOINT ["dotnet", "SeanceScheduler.Web.dll"]