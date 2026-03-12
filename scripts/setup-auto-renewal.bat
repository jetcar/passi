@echo off
REM Setup script for HTTPS certificate auto-renewal on Windows

echo === HTTPS Certificate Renewal Setup ===
echo.

set /p DOMAIN="Domain name (default: passi.cloud): "
if "%DOMAIN%"=="" set DOMAIN=passi.cloud

set /p EMAIL="Admin email (default: admin@%DOMAIN%): "
if "%EMAIL%"=="" set EMAIL=admin@%DOMAIN%

set /p STAGING_INPUT="Use staging server for testing? (y/N): "
if /i "%STAGING_INPUT%"=="y" (
    set STAGING=true
) else (
    set STAGING=false
)

echo.
echo Building certbot container...
docker-compose build certbot
if errorlevel 1 (
    echo ERROR: Failed to build certbot container
    pause
    exit /b 1
)

echo.
echo Starting services...
docker-compose up -d haproxy certbot
if errorlevel 1 (
    echo ERROR: Failed to start services
    pause
    exit /b 1
)

echo.
echo Waiting for services to start...
timeout /t 5 /nobreak >nul

echo.
echo Requesting initial certificate...
docker-compose exec -T -e DOMAIN=%DOMAIN% -e CERTBOT_EMAIL=%EMAIL% -e STAGING=%STAGING% certbot /scripts/request-cert.sh
if errorlevel 1 (
    echo ERROR: Failed to request certificate
    echo Check logs: docker-compose logs certbot
    pause
    exit /b 1
)

echo.
echo === Setup Complete ===
echo.
echo Certificates will be automatically renewed twice daily (2am and 2pm).
echo Manual renewal: docker-compose exec certbot /scripts/renew-certs.sh
echo.
echo Certificate location: ..\passi_cert\passi_cloud_haproxy.ca-bundle
echo.
pause
