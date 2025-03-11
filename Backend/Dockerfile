# Слой сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
EXPOSE 8000

# Устанавливаем dotnet-ef с очисткой кэша для избежания ошибок
RUN rm -rf /root/.dotnet/tools && dotnet tool install --global dotnet-ef --version 8.0.0 --no-cache
ENV PATH="$PATH:/root/.dotnet/tools"

# Копируем файл проекта и восстанавливаем зависимости
COPY ["Backend.csproj", "./"]
RUN dotnet restore "Backend.csproj"

# Копируем остальные файлы и собираем приложение
COPY . .
RUN dotnet build "Backend.csproj" -c Release -o /app/build

# Слой публикации
FROM build AS publish
RUN dotnet publish "Backend.csproj" -c Release -o /app/publish

# Финальный слой с SDK для поддержки dotnet-ef
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS runtime
WORKDIR /app

# Устанавливаем путь для инструментов
ENV PATH="$PATH:/root/.dotnet/tools"

# Копируем опубликованные файлы
COPY --from=publish /app/publish .

# Копируем dotnet-ef из слоя build
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools

# Копируем скрипт для выполнения миграций
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Указываем точку входа
ENTRYPOINT ["/entrypoint.sh"]