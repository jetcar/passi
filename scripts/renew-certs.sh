#!/bin/sh

# Certificate renewal script for Let's Encrypt with HAProxy
# This script renews certificates and reloads HAProxy

CERT_DIR="/etc/letsencrypt/live"
HAPROXY_CERT_DIR="/certs"
DOMAIN="${DOMAIN:-passi.cloud}"
EMAIL="${CERTBOT_EMAIL:-admin@passi.cloud}"

echo "$(date): Starting certificate renewal check for $DOMAIN"

# Check if certificate exists
if [ ! -d "$CERT_DIR/$DOMAIN" ]; then
    echo "$(date): No certificate found. Run: docker-compose exec certbot /scripts/request-cert.sh"
    exit 0
fi

# Show current certificate status
echo "$(date): Current certificate status:"
certbot certificates 2>&1 || echo "Failed to get certificate status"

# Renew certificates (certbot will only renew if expiring within 30 days)
echo "$(date): Running renewal check..."
certbot renew --webroot --webroot-path=/var/www/certbot --deploy-hook "/scripts/deploy-cert.sh" 2>&1

EXIT_CODE=$?
if [ $EXIT_CODE -eq 0 ]; then
    echo "$(date): Certificate renewal check completed successfully"
else
    echo "$(date): Certificate renewal check failed with exit code $EXIT_CODE"
    exit $EXIT_CODE
fi
