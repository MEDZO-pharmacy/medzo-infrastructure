FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Medzo.Gateway/Medzo.Gateway.csproj src/Medzo.Gateway/
RUN dotnet restore src/Medzo.Gateway/Medzo.Gateway.csproj
COPY src/ src/
RUN dotnet publish src/Medzo.Gateway/Medzo.Gateway.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Medzo.Gateway.dll"]
