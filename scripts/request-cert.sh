#!/bin/bash

# Initial certificate request script
# Run this once to get your first certificate

set -e

DOMAIN="${DOMAIN:-passi.cloud}"
EMAIL="${CERTBOT_EMAIL:-admin@passi.cloud}"
STAGING="${STAGING:-false}"

echo "Requesting certificate for domain(s): $DOMAIN"
echo "Email: $EMAIL"

# Build certbot command
CMD="certbot certonly --webroot --webroot-path=/var/www/certbot"
CMD="$CMD --email $EMAIL"
CMD="$CMD --agree-tos"
CMD="$CMD --non-interactive"

# Add staging flag if testing
if [ "$STAGING" = "true" ]; then
    echo "Using Let's Encrypt STAGING server (for testing)"
    CMD="$CMD --staging"
fi

# Add all domains
for d in $DOMAIN; do
    CMD="$CMD -d $d"
done

# Request certificate
eval $CMD

# Deploy the certificate
/scripts/deploy-cert.sh
