#!/bin/sh

# Initial certificate request script
# Run this once to get your first certificate

DOMAIN="${DOMAIN:-passi.cloud}"
EMAIL="${CERTBOT_EMAIL:-admin@passi.cloud}"
STAGING="${STAGING:-false}"

echo "=== Requesting Let's Encrypt Certificate ==="
echo "Domain(s): $DOMAIN"
echo "Email: $EMAIL"
echo ""

# Build certbot command
CMD="certbot certonly --webroot --webroot-path=/var/www/certbot"
CMD="$CMD --email $EMAIL"
CMD="$CMD --agree-tos"
CMD="$CMD --non-interactive"

# Add staging flag if testing
if [ "$STAGING" = "true" ]; then
    echo "⚠ Using Let's Encrypt STAGING server (for testing)"
    CMD="$CMD --staging"
else
    echo "✓ Using Let's Encrypt PRODUCTION server"
fi

# Add all domains
for d in $DOMAIN; do
    CMD="$CMD -d $d"
    echo "  - $d"
done

echo ""
echo "Requesting certificate..."
# Request certificate
eval $CMD

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Certificate request successful!"
    echo ""
    echo "Deploying certificate to HAProxy format..."
    # Deploy the certificate
    /scripts/deploy-cert.sh
    echo ""
    echo "=== Setup Complete ==="
    certbot certificates
else
    echo ""
    echo "✗ Certificate request failed!"
    echo ""
    echo "Common issues:"
    echo "  - Domain DNS not pointing to this server"
    echo "  - Port 80 not accessible from internet"
    echo "  - HAProxy not routing /.well-known/acme-challenge/ correctly"
    echo ""
    echo "Check logs above for details."
    exit 1
fi
