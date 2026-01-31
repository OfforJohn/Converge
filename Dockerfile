# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

WORKDIR /src

COPY ["src/Converge.Configuration.API/Converge.Configuration.API.csproj", "src/Converge.Configuration.API/"]
COPY ["src/Converge.Configuration.Application/Converge.Configuration.Application.csproj", "src/Converge.Configuration.Application/"]
COPY ["src/Converge.Configuration.Persistence/Converge.Configuration.Persistence.csproj", "src/Converge.Configuration.Persistence/"]
COPY ["src/Converge.Configuration.Domain/Converge.Configuration.Domain.csproj", "src/Converge.Configuration.Domain/"]
COPY ["src/ConvergeERP.Shared.Abstractions/ConvergeERP.Shared.Abstractions.csproj", "src/ConvergeERP.Shared.Abstractions/"]

RUN dotnet restore "src/Converge.Configuration.API/Converge.Configuration.API.csproj"

COPY . .
WORKDIR "/src/src/Converge.Configuration.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final

WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Converge.Configuration.API.dll"]
