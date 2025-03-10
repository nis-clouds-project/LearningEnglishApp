# Используем образ SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

COPY ["Backend.csproj", "./"]
RUN dotnet restore "Backend.csproj"

COPY . .
RUN dotnet build "Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Backend.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Backend.dll"]
