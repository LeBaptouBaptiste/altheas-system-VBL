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
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
RUN adduser --system --uid 1001 dotnet && chown -R dotnet /app
USER dotnet
ENTRYPOINT ["dotnet", "API_Althea-systems.dll"]
