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

# Pull latest images even if tags are the same
sudo docker compose -f docker-compose.yml pull --ignore-pull-failures

# Rebuild with --no-cache to force fresh build
sudo docker compose -f docker-compose.yml build --no-cache --pull

# Force recreate all containers even if config hasn't changed
sudo docker compose -f docker-compose.yml up -d --force-recreate --remove-orphans

#sudo docker push jetcar/passiwebapi:1.0.10
#sudo docker push jetcar/identityserver:1.0.10
#sudo docker push jetcar/webapp:1.0.10


