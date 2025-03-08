FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Frontend.csproj", "./"]
RUN dotnet restore "Frontend.csproj"
COPY . .
RUN dotnet build "Frontend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Frontend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY .env ./

# Install wait script
RUN apt-get update && \
    apt-get install -y curl && \
    curl -SL --output wait https://github.com/ufoscout/docker-compose-wait/releases/download/2.9.0/wait && \
    chmod +x wait

# Wait for backend and start the application
ENTRYPOINT ["/bin/sh", "-c"]
CMD ["/wait && dotnet Frontend.dll"]
