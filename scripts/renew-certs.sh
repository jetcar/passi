#!/bin/bash

# Certificate renewal script for Let's Encrypt with HAProxy
# This script renews certificates and reloads HAProxy

set -e

CERT_DIR="/etc/letsencrypt/live"
HAPROXY_CERT_DIR="/certs"
DOMAIN="${DOMAIN:-passi.cloud}"
EMAIL="${CERTBOT_EMAIL:-admin@passi.cloud}"

echo "$(date): Starting certificate renewal process"

# Renew certificates (certbot will only renew if expiring within 30 days)
certbot renew --webroot --webroot-path=/var/www/certbot --quiet --deploy-hook "/scripts/deploy-cert.sh"

echo "$(date): Certificate renewal check completed"
