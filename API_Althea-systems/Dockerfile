FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["API_Althea-systems.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish API_Althea-systems.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
# Use the pre-created non-root 'app' user (UID 1654) shipped with all
# .NET 8+ official images — works on default Debian, chiseled, and alpine
# variants. No `adduser` call needed (which fails on minimal images).
USER app
ENTRYPOINT ["dotnet", "API_Althea-systems.dll"]
