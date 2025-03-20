# ✅ Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-stage

# ✅ Step 1: Copy only .csproj files to optimize caching
WORKDIR /src
COPY MassTransitScheduler.csproj MassTransitScheduler.csproj

# ✅ Restore dependencies
WORKDIR /src
RUN dotnet restore MassTransitScheduler.csproj --verbosity detailed

# ✅ Copy other source codes
COPY Program.cs Program.cs
COPY OutboxDbContext.cs OutboxDbContext.cs
COPY OutboxMessage.cs OutboxMessage.cs

# ✅ Build
RUN dotnet build -c Release --no-restore

# ✅ Publish application
RUN dotnet publish -c Release -o /app/publish --no-build

# ✅ Runtime Stage
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime

# ✅ Copy build output từ build-stage
WORKDIR /app
# Docker sometimes fails if copying a directory without /., treating it as a file.
COPY --from=build-stage /app/publish/. .

# ✅ Run the App
ENTRYPOINT ["dotnet", "MassTransitScheduler.dll"]