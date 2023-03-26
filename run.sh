cd /home/passi
sudo cp /home/passi/run.sh ./run.sh
sudo apt-get clean
sudo git clean -fdx
git checkout .
git fetch
git pull

sudo docker image prune -f
sudo docker volume prune -f

sudo docker pull mcr.microsoft.com/dotnet/sdk:7.0
sudo docker pull mcr.microsoft.com/dotnet/aspnet:7.0

sudo docker build . -f WebApp/Dockerfile -t webapp:1.0.3
sudo docker build . -f IdentityServer/Dockerfile -t identityserver:1.0.3
sudo docker build . -f passiwebapi/Dockerfile -t passiwebapi:1.0.3

sudo docker-compose -f docker-compose.yml up
