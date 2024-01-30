#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ha.iobroker.service/ha.iobroker.service.csproj", "ha.iobroker.service/"]
RUN dotnet restore "ha.iobroker.service/ha.iobroker.service.csproj"
COPY . .
WORKDIR "/src/ha.iobroker.service"
RUN dotnet build "ha.iobroker.service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ha.iobroker.service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ha.iobroker.service.dll"]