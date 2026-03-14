#!/bin/sh

# Monitor webroot directory during certificate request

echo "=== Monitoring Certbot Webroot During Certificate Request ==="
echo "Watching: /var/www/certbot/.well-known/acme-challenge/"
echo ""
echo "Press Ctrl+C to stop"
echo ""

# Watch the directory for changes
while true; do
    clear
    echo "=== $(date) ==="
    echo ""
    echo "Files in webroot:"
    find /var/www/certbot -type f 2>/dev/null | sort
    echo ""
    echo "Challenge directory contents:"
    ls -la /var/www/certbot/.well-known/acme-challenge/ 2>/dev/null || echo "Directory doesn't exist yet"
    echo ""
    echo "---"
    sleep 2
done
