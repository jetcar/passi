FROM node:21 as vueWeb
COPY WebAppUi /app
WORKDIR /app/vue-project
RUN npm install
RUN npm run build


FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY . .
COPY --from=vueWeb /app/vue-project/dist ./WebApp/wwwroot

RUN dotnet restore passi.sln
RUN dotnet publish passi.sln -c Release /p:UseAppHost=false


ENTRYPOINT ["sleep", "3600"]