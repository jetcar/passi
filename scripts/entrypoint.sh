#!/bin/sh

set -e

echo "=== Certbot Auto-Renewal Container Starting ==="
echo "Domain: ${DOMAIN:-passi.cloud}"
echo "Email: ${CERTBOT_EMAIL:-admin@passi.cloud}"
echo ""

# Check if certificates already exist
if [ -d "/etc/letsencrypt/live/${DOMAIN:-passi.cloud}" ]; then
    echo "✓ Certificate found for ${DOMAIN:-passi.cloud}"
    certbot certificates
else
    echo "⚠ No certificate found yet. Run: docker-compose exec certbot /scripts/request-cert.sh"
fi

echo ""
echo "Cron schedule: Renewal checks at 2:00 AM and 2:00 PM daily"
echo "Manual renewal: docker-compose exec certbot /scripts/renew-certs.sh"
echo ""
echo "Starting cron daemon..."

# Start crond in foreground with logging
exec crond -f -d 8
