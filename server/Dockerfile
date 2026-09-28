# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/GiftPlanner.Domain/*.csproj src/GiftPlanner.Domain/
COPY src/GiftPlanner.Application/*.csproj src/GiftPlanner.Application/
COPY src/GiftPlanner.Infrastructure/*.csproj src/GiftPlanner.Infrastructure/
COPY src/GiftPlanner.Api/*.csproj src/GiftPlanner.Api/
RUN dotnet restore src/GiftPlanner.Api/GiftPlanner.Api.csproj
COPY src/ src/
RUN dotnet publish src/GiftPlanner.Api/GiftPlanner.Api.csproj -c Release -o /app --no-restore

# Run (non-root, no SDK)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "GiftPlanner.Api.dll"]
