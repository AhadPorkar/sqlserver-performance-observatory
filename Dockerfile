FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Observatory.slnx
RUN dotnet publish src/Observatory.Web/Observatory.Web.csproj -c Release --no-restore -o /out /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0.12 AS runtime
WORKDIR /app
COPY --from=build /out ./
RUN mkdir -p /app/state && chown -R 1654:1654 /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    Observatory__StateDirectory=/app/state
USER 1654:1654
EXPOSE 8080
ENTRYPOINT ["dotnet", "Observatory.Web.dll"]
