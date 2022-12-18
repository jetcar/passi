#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
EXPOSE 5002
COPY WebApp app/
WORKDIR /app/publish
ENTRYPOINT ["dotnet", "WebApp.dll"]
