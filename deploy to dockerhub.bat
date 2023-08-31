
docker build -f ./WebApp/Dockerfile . -t jetcar/webapp:%1
#docker scan jetcar/webapp:%1 > webapp.txt
docker push jetcar/webapp:%1

docker build -f passiwebapi/Dockerfile . -t jetcar/passiwebapi:%1
#docker scan jetcar/passiwebapi:%1 > passiwebapi.txt
docker push jetcar/passiwebapi:%1

docker build -f IdentityServer/Dockerfile . -t jetcar/identityserver:%1
#docker scan jetcar/identityserver:%1 > identityserver.txt
docker push jetcar/identityserver:%1
