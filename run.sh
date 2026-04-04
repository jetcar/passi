#!/bin/bash

run_privileged() {
	if "$@"; then
		return 0
	fi

	if command -v sudo >/dev/null 2>&1 && sudo -n true >/dev/null 2>&1; then
		sudo "$@"
		return 0
	fi

	return 1
}

cd /home/jetcarq/passi
run_privileged apt-get clean || true
run_privileged git clean -fdx
git checkout .
git fetch
git pull
#sudo ./run_deploy.sh
containers_running=$(docker ps -q)
if [ -n "$containers_running" ]; then
	run_privileged docker stop $containers_running
fi

containers_all=$(docker ps -a -q)
if [ -n "$containers_all" ]; then
	run_privileged docker rm $containers_all
fi

# Pull latest images even if tags are the same
run_privileged docker compose -f docker-compose.yml pull --ignore-pull-failures

# Rebuild with --no-cache to force fresh build
run_privileged docker compose -f docker-compose.yml build --no-cache --pull

# Force recreate all containers even if config hasn't changed
run_privileged docker compose -f docker-compose.yml up -d --force-recreate --remove-orphans

#sudo docker push jetcar/passiwebapi:1.0.10
#sudo docker push jetcar/identityserver:1.0.10
#sudo docker push jetcar/webapp:1.0.10


