FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Vuelos_Backend.csproj ./
RUN dotnet restore Vuelos_Backend.csproj
COPY . ./
RUN dotnet publish Vuelos_Backend.csproj --configuration Release --no-restore --output /publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /publish ./
COPY deploy/appsettings.json ./appsettings.json
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["/bin/sh", "-c", "exec dotnet Vuelos_Backend.dll --urls \"http://0.0.0.0:${PORT:-8080}\""]
