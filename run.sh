#!/bin/bash
cd /home/jetcarq/passi
sudo apt-get clean
sudo git clean -fdx
git checkout .
git fetch
git pull
#sudo ./run_deploy.sh
sudo docker stop $(sudo docker ps -q)
sudo docker rm $(sudo docker ps -a -q)

sudo docker compose -f docker-compose.yml build
sudo docker compose -f docker-compose.yml up -d --remove-orphans

#sudo docker push jetcar/passiwebapi:1.0.10
#sudo docker push jetcar/identityserver:1.0.10
#sudo docker push jetcar/webapp:1.0.10


