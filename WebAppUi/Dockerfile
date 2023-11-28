#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM node:18
WORKDIR /app

COPY WebAppUi/vue .
WORKDIR /app/ClientApp

RUN npm install

EXPOSE 44481


ENTRYPOINT ["npm", "start"]