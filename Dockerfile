FROM node:21 as vueWeb
COPY . /src
WORKDIR /src/WebApp/vue-project
RUN npm install
RUN npm run build

WORKDIR /src/IdentityServer/vue-project
RUN npm install
RUN npm run build


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY --from=vueWeb /src .


RUN dotnet restore passi.sln
RUN dotnet publish passi.sln -c Release /p:UseAppHost=false


ENTRYPOINT ["sleep", "3600"]