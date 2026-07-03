# syntax=docker/dockerfile:1

# ---------- Stage 1: build & publish ----------
# Usa l'SDK .NET 8 solo per compilare: non serve installarlo sulla macchina host.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Ripristino dipendenze (layer separato: sfrutta la cache se il .csproj non cambia).
COPY BlueHarbor.csproj ./
RUN dotnet restore BlueHarbor.csproj

# Copia il resto del sorgente e pubblica in modalita' Release.
COPY . ./
RUN dotnet publish BlueHarbor.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- Stage 2: runtime ----------
# Immagine leggera con solo il runtime ASP.NET Core: nessun SDK a bordo.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# In .NET 8 il runtime ascolta di default sulla porta 8080.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "BlueHarbor.dll"]
