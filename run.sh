cd /home/passi

git clean -fdx
git checkout .
git fetch
git pull

dotnet publish WebApp/WebApp.csproj -c Release -o WebApp/publish
dotnet publish passiwebapi/passiwebapi.csproj -c Release -o passiwebapi/publish
dotnet publish IdentityServer/IdentityServer.csproj -c Release -o IdentityServer/publish


sudo docker image prune -f
sudo docker volume prune -f

sudo docker-compose down
sudo docker-compose -f docker-compose.yml up --force-recreate --build
