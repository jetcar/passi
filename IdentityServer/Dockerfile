#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["IdentityServer/IdentityServer.csproj", "IdentityServer/"]
COPY ["ConfigurationManager/ConfigurationManager.csproj", "ConfigurationManager/"]
COPY ["WebApiDto/WebApiDto.csproj", "WebApiDto/"]
RUN dotnet restore "IdentityServer/IdentityServer.csproj"
COPY . .

WORKDIR "/src/IdentityServer"
#RUN dotnet ef dbcontext optimize --output-dir CompiledModel

RUN dotnet build "IdentityServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IdentityServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdentityServer.dll"]