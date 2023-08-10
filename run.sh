#!/bin/bash
cd /home/jetcarq/passi
sudo apt-get clean
sudo git clean -fdx
git checkout .
git fetch
git pull

sudo docker pull mcr.microsoft.com/dotnet/sdk:7.0
sudo docker pull mcr.microsoft.com/dotnet/aspnet:7.0

sudo docker compose -f docker-compose.yml build
sudo docker compose down
docker system prune -a -f
sudo docker image prune -f --all
sudo docker volume prune -f --all


sudo docker compose -f docker-compose.yml up
