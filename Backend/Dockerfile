FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
EXPOSE 8000

RUN dotnet tool install --global dotnet-ef --version 8.0.0

COPY ["Backend.csproj", "./"]
RUN dotnet restore "Backend.csproj"
COPY . .
RUN dotnet build "Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Backend.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS runtime
WORKDIR /app

ENV PATH="$PATH:/root/.dotnet/tools"

COPY --from=publish /app/publish .

COPY --from=build /root/.dotnet/tools /root/.dotnet/tools

ENTRYPOINT ["dotnet", "Backend.dll"]