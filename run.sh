#!/bin/bash
cd /home/jetcarq/passi
sudo apt-get clean
sudo git clean -fdx
git checkout .
git fetch
git pull
./run_deploy.sh

