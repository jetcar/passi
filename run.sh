#!/bin/bash
cd /home/jetcarq/passi
sudo apt-get clean
sudo git clean -fdx
git checkout .
git fetch
git pull
#sudo ./run_deploy.sh

sudo docker exec -it postgres pg_ctl stop -m smart
sudo docker compose down
sudo docker stop $(docker ps -a -q)
sudo docker system prune -a -f
sudo docker image prune -f --all
sudo docker volume prune -f --all

sudo docker compose -f docker-compose-cloud.yml build
sudo docker compose -f docker-compose-cloud.yml up

