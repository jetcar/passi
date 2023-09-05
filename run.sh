#!/bin/bash
cd /home/jetcarq/passi
sudo apt-get clean
sudo git clean -fdx
git checkout .
git fetch
git pull
#sudo ./run_deploy.sh

sudo docker system prune -a -f
sudo docker image prune -f --all
sudo docker volume prune -f --all

sudo docker compose -f docker-compose.yml build
sudo docker compose -f docker-compose.yml up -d

#sudo docker push jetcar/passiwebapi:1.0.5
#sudo docker push jetcar/identityserver:1.0.5
#sudo docker push jetcar/webapp:1.0.5


