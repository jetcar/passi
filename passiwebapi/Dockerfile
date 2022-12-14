#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5004

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["passiwebapi/passiwebapi.csproj", "passiwebapi/"]
COPY ["ConfigurationManager/ConfigurationManager.csproj", "ConfigurationManager/"]
COPY ["OpenIdLib/OpenIdLib.csproj", "OpenIdLib/"]
COPY ["Services/Services.csproj", "Services/"]
COPY ["AppCommon/AppCommon.csproj", "AppCommon/"]
COPY ["AppConfig/AppConfig.csproj", "AppConfig/"]
COPY ["Models/Models.csproj", "Models/"]
COPY ["Repos/Repos.csproj", "Repos/"]
COPY ["WebApiDto/WebApiDto.csproj", "WebApiDto/"]
RUN dotnet restore "passiwebapi/passiwebapi.csproj"
COPY . .
WORKDIR "/src/passiwebapi"
#RUN dotnet ef dbcontext optimize

RUN dotnet build "passiwebapi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "passiwebapi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "passiwebapi.dll"]