docker build . -f WebApp/Dockerfile -t webapp:1.0.3
docker build . -f IdentityServer/Dockerfile -t identityserver:1.0.3
docker build . -f passiwebapi/Dockerfile -t passiwebapi:1.0.3
