FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Backend.csproj", "./"]
RUN dotnet restore "Backend.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Backend.csproj" -c Release -o /app/publish

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install EF Core tools and wait script
RUN apt-get update && \
    apt-get install -y curl && \
    curl -SL --output wait https://github.com/ufoscout/docker-compose-wait/releases/download/2.9.0/wait && \
    chmod +x wait

# Set up localization
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
RUN apt-get update && \
    apt-get install -y locales && \
    sed -i -e 's/# en_US.UTF-8 UTF-8/en_US.UTF-8 UTF-8/' /etc/locale.gen && \
    dpkg-reconfigure --frontend=noninteractive locales && \
    locale-gen en_US.UTF-8
ENV LANG=en_US.UTF-8 \
    LANGUAGE=en_US:en \
    LC_ALL=en_US.UTF-8

ENTRYPOINT ["dotnet", "Backend.dll"] 