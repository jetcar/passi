#!/bin/sh

# Request a certificate or expand the existing certificate with additional names.

DOMAIN="${DOMAIN:-passi.cloud}"
CERTBOT_DOMAINS="${CERTBOT_DOMAINS:-$DOMAIN}"
EMAIL="${CERTBOT_EMAIL:-admin@passi.cloud}"
STAGING="${STAGING:-false}"

echo "=== Requesting Let's Encrypt Certificate ==="
echo "Domain(s): $CERTBOT_DOMAINS"
echo "Email: $EMAIL"
echo ""

# Build certbot command
set -- certbot certonly --webroot --webroot-path=/var/www/certbot \
    --email "$EMAIL" --agree-tos --non-interactive \
    --cert-name "$DOMAIN" --expand

# Add staging flag if testing
if [ "$STAGING" = "true" ]; then
    echo "⚠ Using Let's Encrypt STAGING server (for testing)"
    set -- "$@" --staging
else
    echo "✓ Using Let's Encrypt PRODUCTION server"
fi

echo ""
echo "Checking webroot setup..."
echo "Webroot path: /var/www/certbot"
ls -la /var/www/certbot/ || echo "ERROR: Webroot doesn't exist!"
mkdir -p /var/www/certbot/.well-known/acme-challenge
echo "test" > /var/www/certbot/.well-known/acme-challenge/pre-flight-test.txt
echo "Pre-flight test file created"
ls -la /var/www/certbot/.well-known/acme-challenge/

# Add all domains
# Split the space-separated list without expanding shell wildcards.
set -f
for d in $CERTBOT_DOMAINS; do
    set -- "$@" -d "$d"
    echo "  - $d"
done

echo ""
echo "Requesting certificate..."
# Request certificate with verbose output
echo "Certbot command: $*"
echo ""
"$@" -v

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Certificate request successful!"
    echo ""
    echo "Deploying certificate to HAProxy format..."
    # Deploy the certificate
    /scripts/deploy-cert.sh || exit $?
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
