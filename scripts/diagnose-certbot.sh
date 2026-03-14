#!/bin/sh

# Comprehensive certbot diagnostics

echo "=== Certbot Challenge File Creation Diagnostics ==="
echo ""

echo "[1] Directory structure:"
ls -laR /var/www/certbot/ 2>&1
echo ""

echo "[2] Testing write permissions:"
TEST_FILE="/var/www/certbot/.well-known/acme-challenge/permission-test-$$"
if echo "test-content" > "$TEST_FILE" 2>&1; then
    echo "✓ Can write to $TEST_FILE"
    cat "$TEST_FILE"
    rm "$TEST_FILE"
else
    echo "✗ CANNOT write to acme-challenge directory!"
    echo "Error: $?"
fi
echo ""

echo "[3] Running user/permissions:"
echo "Current user: $(whoami)"
echo "User ID: $(id)"
echo ""

echo "[4] Directory ownership:"
stat /var/www/certbot
stat /var/www/certbot/.well-known 2>/dev/null || echo "  .well-known doesn't exist"
stat /var/www/certbot/.well-known/acme-challenge 2>/dev/null || echo "  acme-challenge doesn't exist"
echo ""

echo "[5] Creating test structure:"
mkdir -p /var/www/certbot/.well-known/acme-challenge
chmod -R 755 /var/www/certbot
echo "test" > /var/www/certbot/.well-known/acme-challenge/diagnostic-test.txt
echo "✓ Created diagnostic-test.txt"
ls -la /var/www/certbot/.well-known/acme-challenge/
echo ""

echo "[6] Running certbot with maximum verbosity (dry-run):"
certbot certonly \
    --webroot \
    --webroot-path=/var/www/certbot \
    --email ${CERTBOT_EMAIL:-admin@passi.cloud} \
    --agree-tos \
    --non-interactive \
    --staging \
    --dry-run \
    -d ${DOMAIN:-passi.cloud} \
    --debug \
    2>&1 | tail -100

echo ""
echo "[7] Checking if challenge file was created during dry-run:"
find /var/www/certbot -type f -name "*" 2>/dev/null
echo ""

echo "=== Diagnostics Complete ==="
