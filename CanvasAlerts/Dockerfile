FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["CanvasAlerts/CanvasAlerts.csproj", "CanvasAlerts/"]
RUN dotnet restore "CanvasAlerts/CanvasAlerts.csproj"
COPY . .
WORKDIR "/src/CanvasAlerts"
RUN dotnet build "CanvasAlerts.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CanvasAlerts.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CanvasAlerts.dll"]
