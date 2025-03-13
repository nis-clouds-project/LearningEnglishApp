FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

RUN dotnet tool install --global dotnet-ef --version 8.0.0
ENV PATH="$PATH:/root/.dotnet/tools"

COPY ["Backend.csproj", "./"]
RUN dotnet restore "Backend.csproj"

COPY . .
RUN dotnet build "Backend.csproj" -c Release -o /tmp/build

FROM build AS publish
RUN dotnet publish "Backend.csproj" -c Release -o /tmp/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /tmp/app

ENV PATH="$PATH:/root/.dotnet/tools"

COPY --from=publish /tmp/publish .

COPY --from=build /root/.dotnet/tools /root/.dotnet/tools

COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]