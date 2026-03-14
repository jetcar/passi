#!/bin/sh

# Manual ACME challenge file creation test

echo "=== Manual Challenge File Creation Test ==="
echo ""

# This simulates what certbot should do
CHALLENGE_DIR="/var/www/certbot/.well-known/acme-challenge"
CHALLENGE_TOKEN="manual-test-token-123456"
CHALLENGE_CONTENT="manual-test-content-xyz"

echo "[1] Ensure directory exists:"
mkdir -p "$CHALLENGE_DIR"
ls -la "$CHALLENGE_DIR"
echo ""

echo "[2] Create challenge file manually:"
echo "$CHALLENGE_CONTENT" > "$CHALLENGE_DIR/$CHALLENGE_TOKEN"
if [ $? -eq 0 ]; then
    echo "✓ File created successfully"
else
    echo "✗ Failed to create file (exit code: $?)"
    exit 1
fi
echo ""

echo "[3] Verify file exists:"
ls -la "$CHALLENGE_DIR/$CHALLENGE_TOKEN"
cat "$CHALLENGE_DIR/$CHALLENGE_TOKEN"
echo ""

echo "[4] Check file from httpd:"
wget -q -O- "http://localhost/.well-known/acme-challenge/$CHALLENGE_TOKEN" || echo "✗ HTTP server cannot serve the file"
echo ""

echo "[5] Now testing with actual certbot authenticator:"
echo "Running certbot with manual authenticator to see what happens..."
echo ""

# Try with manual authenticator first to see if webroot is the issue
certbot certonly \
    --manual \
    --preferred-challenges http \
    --email ${CERTBOT_EMAIL:-admin@passi.cloud} \
    --agree-tos \
    --non-interactive \
    --staging \
    --dry-run \
    -d ${DOMAIN:-passi.cloud} \
    --debug \
    2>&1 | tail -50

echo ""
echo "=== Test Complete ==="
