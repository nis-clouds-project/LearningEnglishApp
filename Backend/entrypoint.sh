#!/bin/sh
dotnet ef database update --project Backend.csproj
dotnet Backend.dll