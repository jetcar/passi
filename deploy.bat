docker build . -f DockerfileBuildAll -t common_image:1.0.6
docker build . -f identityserver/Dockerfile -t jetcar/identityserver:1.0.6
docker build . -f webapp/Dockerfile -t jetcar/webapp:1.0.6
docker build . -f passiwebapi/Dockerfile -t jetcar/passiwebapi:1.0.6

docker push jetcar/identityserver:1.0.6
docker push jetcar/webapp:1.0.6
docker push jetcar/passiwebapi:1.0.6