FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore "./PaieApi.csproj"
RUN dotnet publish "./PaieApi.csproj" -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

# Vérifiez que appsettings.json est présent
# RUN ls -la

ENTRYPOINT ["dotnet", "PaieApi.dll"]