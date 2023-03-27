

docker image prune -f
docker volume prune -f

sudo docker pull mcr.microsoft.com/dotnet/sdk:7.0
sudo docker pull mcr.microsoft.com/dotnet/aspnet:7.0

docker build . -f WebApp/Dockerfile -t webapp:1.0.4
docker build . -f IdentityServer/Dockerfile -t identityserver:1.0.4
docker build . -f passiwebapi/Dockerfile -t passiwebapi:1.0.4

docker-compose down
docker-compose -f docker-compose.yml up  --force-recreate --build
