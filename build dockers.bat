docker build . -f WebAppUi/Dockerfile -t webappui:1.0.11 -t webappui:latest
docker build . -f Dockerfile -t common_image:1.0.11 -t common_image:latest
docker build . -f identityserver/Dockerfile -t jetcar/identityserver:1.0.11
docker build . -f webapp/Dockerfile -t jetcar/webapp:1.0.11
docker build . -f passiwebapi/Dockerfile -t jetcar/passiwebapi:1.0.11

pause