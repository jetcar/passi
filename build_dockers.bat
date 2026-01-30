docker build . -f Dockerfile -t common_image:latest -t common_image:latest -t common_image:dev
docker build . -f webapp/Dockerfile -t jetcar/webapp:latest -t jetcar/webapp:latest -t jetcar/webapp:dev
docker build . -f passiwebapi/Dockerfile -t jetcar/passiwebapi:latest -t jetcar/passiwebapi:latest -t jetcar/passiwebapi:dev
docker build . -f OpenIDC/Dockerfile -t jetcar/openidc:latest -t jetcar/openidc:latest -t jetcar/openidc:dev
pause
