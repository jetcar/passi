#!/bin/sh

# Deploy certificate to HAProxy format
# This script is called by certbot after successful renewal

CERT_DIR="/etc/letsencrypt/live"
HAPROXY_CERT_DIR="/certs"
DOMAIN="${RENEWED_DOMAINS:-${DOMAIN:-passi.cloud}}"

echo "$(date): Deploying certificate for domains: $DOMAIN"

# Certbot supplies the actual certificate directory when invoking a renewal hook.
# Domain ordering is not a reliable way to identify a multi-domain certificate.
CERT_PATH="${RENEWED_LINEAGE:-$CERT_DIR/${DOMAIN:-passi.cloud}}"

if [ ! -d "$CERT_PATH" ]; then
    echo "ERROR: Certificate directory not found: $CERT_PATH"
    exit 1
fi

echo "$(date): Found certificate directory: $CERT_PATH"

# Combine certificate and key for HAProxy (PEM format)
# HAProxy needs: private key + certificate + intermediate certificates
cat "$CERT_PATH/privkey.pem" \
    "$CERT_PATH/fullchain.pem" \
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
