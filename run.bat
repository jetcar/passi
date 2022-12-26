docker image prune -f
docker volume prune -f

docker-compose down
docker-compose -f docker-compose.yml up --force-recreate --build
