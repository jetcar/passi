#!/bin/bash

sudo docker stop $(docker ps -a -q)
sudo docker compose down
sudo docker system prune -a -f
sudo docker image prune -f --all
sudo docker volume prune -f --all


sudo docker pull mcr.microsoft.com/dotnet/sdk:7.0
sudo docker pull mcr.microsoft.com/dotnet/aspnet:7.0

sudo docker compose -f docker-compose.yml build


sudo docker compose -f docker-compose.yml up
