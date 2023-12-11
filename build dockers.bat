docker build . -f Dockerfile -t common_image:1.0.12 -t common_image:latest
docker build . -f identityserver/Dockerfile -t jetcar/identityserver:1.0.12
docker build . -f webapp/Dockerfile -t jetcar/webapp:1.0.12
docker build . -f passiwebapi/Dockerfile -t jetcar/passiwebapi:1.0.12

pause