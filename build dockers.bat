docker build . -f Dockerfile -t common_image:1.0.38 -t common_image:latest -t common_image:dev
docker build . -f identityserver/Dockerfile -t jetcar/identityserver:1.0.38 -t jetcar/identityserver:latest -t jetcar/identityserver:dev
docker build . -f webapp/Dockerfile -t jetcar/webapp:1.0.38 -t jetcar/webapp:latest -t jetcar/webapp:dev
docker build . -f passiwebapi/Dockerfile -t jetcar/passiwebapi:1.0.38 -t jetcar/passiwebapi:latest -t jetcar/passiwebapi:dev

pause