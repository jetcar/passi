#!/bin/bash

# Run this script to set up automatic HTTPS certificate renewal

echo "=== HTTPS Certificate Renewal Setup ==="
echo ""
echo "This will set up automatic certificate renewal for your domain."
echo ""

# Check if configuration needs updating
read -p "Domain name (default: passi.cloud): " DOMAIN
DOMAIN=${DOMAIN:-passi.cloud}

read -p "Admin email (default: admin@$DOMAIN): " EMAIL
EMAIL=${EMAIL:-admin@$DOMAIN}

read -p "Use staging server for testing? (y/N): " STAGING
if [[ "$STAGING" =~ ^[Yy]$ ]]; then
    STAGING_FLAG="true"
else
    STAGING_FLAG="false"
fi

# Update docker-compose environment
echo ""
echo "Updating docker-compose configuration..."
export DOMAIN="$DOMAIN"
export CERTBOT_EMAIL="$EMAIL"
export STAGING="$STAGING_FLAG"

# Build certbot container
echo ""
echo "Building certbot container..."
docker-compose build certbot

# Start services
echo ""
echo "Starting services..."
docker-compose up -d haproxy certbot

# Wait for services to be ready
echo ""
echo "Waiting for services to start..."
sleep 5

# Request initial certificate
echo ""
echo "Requesting initial certificate..."
docker-compose exec -T certbot /scripts/request-cert.sh

echo ""
echo "=== Setup Complete ==="
echo ""
echo "Certificates will be automatically renewed twice daily (2am and 2pm)."
echo "Manual renewal: docker-compose exec certbot /scripts/renew-certs.sh"
echo ""
echo "Certificate location: ../passi_cert/passi_cloud_haproxy.ca-bundle"
echo ""
