

#dotnet publish WebApp/WebApp.csproj -c Release -o WebApp/publish
#dotnet publish passiwebapi/passiwebapi.csproj -c Release -o passiwebapi/publish
#dotnet publish IdentityServer/IdentityServer.csproj -c Release -o IdentityServer/publish



docker build -f ./WebApp/Dockerfile . -t jetcar/samplewebapp:1.0.2
#docker scan jetcar/samplewebapp:1.0.2 > webapp.txt
docker push jetcar/samplewebapp:1.0.2

docker build -f passiwebapi/Dockerfile . -t jetcar/passi:1.0.2
#docker scan jetcar/passi:1.0.2 > passi.txt
docker push jetcar/passi:1.0.2

docker build -f IdentityServer/Dockerfile . -t jetcar/identityserver:1.0.2
#docker scan jetcar/identityserver:1.0.2 > identityserver.txt
docker push jetcar/identityserver:1.0.2
