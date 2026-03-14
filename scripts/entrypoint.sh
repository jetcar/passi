#!/bin/sh

echo "=== Certbot Auto-Renewal Container Starting ==="
echo "Domain: ${DOMAIN:-passi.cloud}"
echo "Email: ${CERTBOT_EMAIL:-admin@passi.cloud}"
echo ""

# Ensure required directories exist
echo "Setting up directories..."
mkdir -p /var/www/certbot
mkdir -p /certs
mkdir -p /var/log
touch /var/log/certbot-renew.log

echo "✓ Directories ready"
echo ""

# Start simple HTTP server for ACME challenges on port 80
echo "Starting HTTP server for ACME challenges on port 80..."
httpd -f -p 80 -h /var/www/certbot &
HTTPD_PID=$!
echo "✓ HTTP server running (PID: $HTTPD_PID)"
echo ""

# Check if certificates already exist
DOMAIN_DIR="/etc/letsencrypt/live/${DOMAIN:-passi.cloud}"
if [ -d "$DOMAIN_DIR" ]; then
    echo "✓ Certificate found for ${DOMAIN:-passi.cloud}"
    echo ""
    certbot certificates 2>&1 || echo "Could not display certificate info"
else
    echo "⚠ No certificate found yet."
    echo ""
    echo "To request your first certificate, run:"
    echo "  docker-compose exec certbot /scripts/request-cert.sh"
    echo ""
    echo "For testing (staging server):"
    echo "  docker-compose exec -e STAGING=true certbot /scripts/request-cert.sh"
fi

echo ""
echo "=== Automatic Renewal Configuration ==="
echo "Cron schedule: Daily at 2:00 AM and 2:00 PM"
echo "Renewal trigger: Certificates expiring within 30 days"
echo ""
echo "Manual commands:"
echo "  Renew now:    docker-compose exec certbot /scripts/renew-certs.sh"
echo "  Check status: docker-compose exec certbot certbot certificates"
echo "  View logs:    docker-compose exec certbot cat /var/log/certbot-renew.log"
echo ""
echo "Starting cron daemon..."
echo ""

# Start crond in foreground with logging
exec crond -f -d 8
