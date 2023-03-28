cd /home/passi
sudo cp /home/passi/run.sh ./run.sh
sudo apt-get clean
sudo git clean -fdx
git checkout .
git fetch
git pull

sudo docker image prune -f
sudo docker volume prune -f

sudo docker-compose down
sudo docker-compose -f docker-compose-vpn.yml up
