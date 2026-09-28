# SDK base for build and test
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS sdk-base
ENV NUGET_FALLBACK_PACKAGES=""
WORKDIR /src
COPY src/GiftPlanner.Domain/*.csproj src/GiftPlanner.Domain/
COPY src/GiftPlanner.Application/*.csproj src/GiftPlanner.Application/
COPY src/GiftPlanner.Infrastructure/*.csproj src/GiftPlanner.Infrastructure/
COPY src/GiftPlanner.Api/*.csproj src/GiftPlanner.Api/
COPY tests/GiftPlanner.Application.Tests/*.csproj tests/GiftPlanner.Application.Tests/
RUN dotnet restore src/GiftPlanner.Api/GiftPlanner.Api.csproj --interactive false
RUN dotnet restore tests/GiftPlanner.Application.Tests/GiftPlanner.Application.Tests.csproj --interactive false

# Test stage
FROM sdk-base AS test
COPY . .
RUN dotnet test tests/GiftPlanner.Application.Tests/GiftPlanner.Application.Tests.csproj --no-restore

# Build stage
FROM sdk-base AS build
COPY src/ src/
RUN dotnet build src/GiftPlanner.Api/GiftPlanner.Api.csproj -c Release --no-restore && \
    dotnet publish src/GiftPlanner.Api/GiftPlanner.Api.csproj -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "GiftPlanner.Api.dll"]
