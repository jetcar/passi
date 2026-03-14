#!/bin/sh

# Run certbot with filesystem monitoring using inotify

echo "=== Certbot with Filesystem Monitoring ==="
echo ""

# Install inotify tools if available
apk add --no-cache inotify-tools 2>/dev/null || echo "inotify-tools not available, using manual monitoring"

WEBROOT="/var/www/certbot"
CHALLENGE_DIR="$WEBROOT/.well-known/acme-challenge"

# Ensure directory exists
mkdir -p "$CHALLENGE_DIR"

echo "Monitoring directory: $CHALLENGE_DIR"
echo "Starting monitor in background..."
echo ""

# Start monitoring in background
(
    if command -v inotifywait >/dev/null 2>&1; then
        inotifywait -m -r "$WEBROOT" -e create,modify,delete 2>/dev/null | while read path action file; do
            echo "[FS EVENT] $action: $path$file"
        done
    else
        # Fallback to polling
        while true; do
            find "$WEBROOT" -type f -mmin -1 2>/dev/null | while read f; do
                echo "[FS EVENT] Recent file: $f"
            done
            sleep 1
        done
    fi
) &
MONITOR_PID=$!

echo "Monitor PID: $MONITOR_PID"
echo ""
echo "Running certbot with webroot authenticator..."
echo ""

# Run certbot
certbot certonly \
    --webroot \
    --webroot-path="$WEBROOT" \
    --email ${CERTBOT_EMAIL:-admin@passi.cloud} \
    --agree-tos \
    --non-interactive \
    --staging \
    -d ${DOMAIN:-passi.cloud} \
    --verbose \
    2>&1 | tee /tmp/certbot-output.log

CERTBOT_EXIT=$?

echo ""
echo "Certbot exit code: $CERTBOT_EXIT"
echo ""

# Kill monitor
kill $MONITOR_PID 2>/dev/null

echo "Files in challenge directory after certbot run:"
ls -la "$CHALLENGE_DIR"
echo ""

echo "Recent certbot log entries:"
tail -30 /var/log/letsencrypt/letsencrypt.log
echo ""

echo "=== Monitoring Complete ==="
