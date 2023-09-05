#!/bin/bash

sudo docker exec -it postgres pg_ctl stop -m smart

echo "Instance is shutting down at $(date)" >> /var/log/shutdown.log

exit 0