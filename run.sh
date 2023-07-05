sudo apt-get clean
sudo git clean -fdx
git checkout .
git fetch
git pull

sudo docker image prune -f --all
sudo docker volume prune -f --all

sudo docker pull mcr.microsoft.com/dotnet/sdk:7.0
sudo docker pull mcr.microsoft.com/dotnet/aspnet:7.0

sudo docker build . -f WebApp/Dockerfile -t webapp:1.0.4
sudo docker build . -f IdentityServer/Dockerfile -t identityserver:1.0.4
sudo docker build . -f passiwebapi/Dockerfile -t passiwebapi:1.0.4


sudo docker-compose down
sudo docker-compose -f docker-compose.yml up
