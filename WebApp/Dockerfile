#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["WebApp/WebApp.csproj", "WebApp/"]
COPY ["OpenIdLib/OpenIdLib.csproj", "OpenIdLib/"]
COPY ["Services/Services.csproj", "Services/"]
COPY ["AppCommon/AppCommon.csproj", "AppCommon/"]
COPY ["AppConfig/AppConfig.csproj", "AppConfig/"]
COPY ["Models/Models.csproj", "Models/"]
COPY ["Repos/Repos.csproj", "Repos/"]
COPY ["ConfigurationManager/ConfigurationManager.csproj", "ConfigurationManager/"]
COPY ["WebApiDto/WebApiDto.csproj", "WebApiDto/"]
RUN dotnet restore "WebApp/WebApp.csproj"
COPY . .
WORKDIR "/src/WebApp"
RUN dotnet build "WebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApp.dll"]