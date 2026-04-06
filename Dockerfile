FROM node:23 as vueWeb
COPY . /src
WORKDIR /src/WebApp/vue-project
RUN npm install
RUN npm run build

WORKDIR /src/OpenIDC/vue-project
RUN npm install
RUN npm run build


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
COPY --from=vueWeb /src/WebApp/wwwroot ./WebApp/wwwroot
COPY --from=vueWeb /src/OpenIDC/wwwroot ./OpenIDC/wwwroot

RUN dotnet restore passi.sln
RUN dotnet publish passi.sln -c Release /p:UseAppHost=false


ENTRYPOINT ["sleep", "3600"]