@echo off
setlocal enableextensions

set "ROOT=%~dp0.."
set "NET=passi-ui-e2e"
set "PG=passi-ui-e2e-postgres"
set "REDIS=passi-ui-e2e-redis"
set "MAILHOG=passi-ui-e2e-mailhog"
set "API=passi-ui-e2e-api"
set "API_IMAGE=passiwebapi-e2e:local"
for /f "tokens=2 delims=:" %%A in ('ipconfig ^| findstr /R "IPv4.*192\." 2^>nul') do (
  set "HOST_IP=%%A"
  goto :got_ip
)
for /f "tokens=2 delims=:" %%A in ('ipconfig ^| findstr /R "IPv4" 2^>nul') do (
  set "HOST_IP=%%A"
  goto :got_ip
)
:got_ip
set "HOST_IP=%HOST_IP: =%"
set "BASE_URL=http://%HOST_IP%:5004/passiapi"
set "MAILHOG_URL=http://%HOST_IP%:8025"
set "PKG=com.passi.cloud.passi_android"

where adb >nul 2>nul
if errorlevel 1 (
  echo adb not found in PATH.
  exit /b 1
)

echo Cleaning previous local UI-E2E containers...
docker rm -f %API% %MAILHOG% %REDIS% %PG% >nul 2>nul
docker network rm %NET% >nul 2>nul

echo Building API images for test mode...
docker build "%ROOT%" -f "%ROOT%\Dockerfile" -t jetcar/common_image:latest -t common_image:dev
if errorlevel 1 exit /b 1
docker build "%ROOT%" -f "%ROOT%\passiwebapi\Dockerfile" -t %API_IMAGE%
if errorlevel 1 exit /b 1

echo Starting UI-E2E services...
docker network create %NET% >nul
docker run -d --name %PG% --network %NET% --network-alias postgres -e POSTGRES_PASSWORD=test1 -e POSTGRES_USER=postgres -e POSTGRES_DB=Passi postgres:15.5 >nul
docker run -d --name %REDIS% --network %NET% --network-alias redis redis:7-alpine >nul
docker run -d --name %MAILHOG% --network %NET% --network-alias mailhog -p 8025:8025 mailhog/mailhog:latest >nul
docker run -d --name %API% --network %NET% --network-alias passiwebapi -p 5004:5004 ^
  -e DbHost=postgres ^
  -e DbPassword=test1 ^
  -e DbPort=5432 ^
  -e DbUser=postgres ^
  -e DbSslMode=Allow ^
  -e PassiDbName=Passi ^
  -e redis=redis ^
  -e redisPort=6379 ^
  -e smtpHost=mailhog ^
  -e smtpPort=1025 ^
  -e smtpUsername=test ^
  -e smtpPassword=test ^
  -e emailFrom=passi@test.com ^
  -e DoNotSendMail=false ^
  -e IsTest=true ^
  -e SmtpDisableSsl=true ^
  -e google-services-json-path=/nonexistent ^
  %API_IMAGE% >nul

if errorlevel 1 (
  echo Failed to start API container.
  exit /b 1
)

echo Waiting for API health endpoint...
powershell -NoProfile -Command "$deadline=(Get-Date).AddSeconds(90); while((Get-Date)-lt $deadline){ try { $r=Invoke-WebRequest -Uri 'http://localhost:5004/passiapi/health' -UseBasicParsing -TimeoutSec 4; if($r.StatusCode -eq 200){ exit 0 } } catch {}; Start-Sleep -Milliseconds 500 }; exit 1"
if errorlevel 1 (
  echo API health check failed.
  docker logs %API%
  exit /b 1
)

echo Ensure emulator is connected and visible...
adb wait-for-device
adb shell settings put system show_touches 1 >nul 2>nul
adb shell settings put system pointer_location 1 >nul 2>nul
adb shell pm clear %PKG% >nul 2>nul

echo Running visible full-flow instrumentation test...
cd /d "%~dp0"
call .\gradlew :app:connectedDebugAndroidTest -Pandroid.testInstrumentationRunnerArguments.class=com.passi.cloud.passi_android.FullFlowVisibleTest -Pandroid.testInstrumentationRunnerArguments.BASE_URL=%BASE_URL% -Pandroid.testInstrumentationRunnerArguments.MAILHOG_URL=%MAILHOG_URL% --no-daemon --rerun-tasks
set "TEST_EXIT=%ERRORLEVEL%"

echo Stopping UI-E2E services...
docker rm -f %API% %MAILHOG% %REDIS% %PG% >nul 2>nul
docker network rm %NET% >nul 2>nul

exit /b %TEST_EXIT%
