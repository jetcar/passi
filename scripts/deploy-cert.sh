#!/bin/sh

# Deploy certificate to HAProxy format
# This script is called by certbot after successful renewal

CERT_DIR="/etc/letsencrypt/live"
HAPROXY_CERT_DIR="/certs"
DOMAIN="${RENEWED_DOMAINS:-${DOMAIN:-passi.cloud}}"

echo "$(date): Deploying certificate for domains: $DOMAIN"

# Get the first domain from the list
MAIN_DOMAIN=$(echo $DOMAIN | cut -d' ' -f1)

if [ ! -d "$CERT_DIR/$MAIN_DOMAIN" ]; then
    echo "ERROR: Certificate directory not found: $CERT_DIR/$MAIN_DOMAIN"
    exit 1
fi

echo "$(date): Found certificate directory: $CERT_DIR/$MAIN_DOMAIN"

# Combine certificate and key for HAProxy (PEM format)
# HAProxy needs: private key + certificate + intermediate certificates
cat "$CERT_DIR/$MAIN_DOMAIN/privkey.pem" \
    "$CERT_DIR/$MAIN_DOMAIN/fullchain.pem" \
    > "$HAPROXY_CERT_DIR/passi_cloud_haproxy.ca-bundle"

if [ $? -eq 0 ]; then
    echo "$(date): Certificate deployed to $HAPROXY_CERT_DIR/passi_cloud_haproxy.ca-bundle"
else
    echo "ERROR: Failed to combine certificate files"
    exit 1
fi

# Reload HAProxy gracefully
echo "$(date): Reloading HAProxy..."
if docker exec haproxy kill -USR2 1 2>&1; then
    echo "$(date): HAProxy reloaded successfully"
else
    echo "$(date): Warning - Could not reload HAProxy. It may reload on restart."
fi
