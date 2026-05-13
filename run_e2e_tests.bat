@echo off
cd /d "%~dp0android-native"
echo.
echo Existing e2e containers before run:
docker ps -a --filter "name=passi-e2e-"
echo.
echo Running e2e tests with forced rerun (no Gradle cache)...
call gradlew.bat :e2e:test :e2e:jacocoTestReport --no-daemon --rerun-tasks --no-build-cache --console=plain
if %errorlevel% neq 0 (
    echo Tests failed
    pause
    exit /b %errorlevel%
)
echo.
echo e2e containers after run (tests clean them up in @AfterClass):
docker ps -a --filter "name=passi-e2e-"
echo.
echo Coverage report: %~dp0android-native\e2e\build\reports\jacoco\test\html\index.html
start "" "%~dp0android-native\e2e\build\reports\jacoco\test\html\index.html"
pause
