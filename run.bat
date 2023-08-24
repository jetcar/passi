

docker compose down
docker system prune -a -f
docker image prune -f --all
docker volume prune -f --all

docker-compose -f docker-compose-cloud.yml build
docker-compose down
docker stop $(docker ps -a -q)
docker-compose -f docker-compose-cloud.yml up