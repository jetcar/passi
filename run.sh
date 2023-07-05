sudo apt-get clean
sudo git clean -fdx
git checkout .
git fetch
git pull

sudo docker-compose down
sudo docker stop $(docker ps -aq)
sudo docker rm $(docker ps -aq)

sudo docker image prune -f --all
sudo docker volume prune -f --all
sudo service docker stop
sudo rm -rf /var/lib/docker/overlay2
sudo service docker start


sudo docker pull mcr.microsoft.com/dotnet/sdk:7.0
sudo docker pull mcr.microsoft.com/dotnet/aspnet:7.0

sudo docker build . -f WebApp/Dockerfile -t webapp:1.0.4
sudo docker build . -f IdentityServer/Dockerfile -t identityserver:1.0.4
sudo docker build . -f passiwebapi/Dockerfile -t passiwebapi:1.0.4


sudo docker-compose -f docker-compose.yml up
