@echo off
setlocal enabledelayedexpansion

:: === CONFIGURATION ===
set "OLD_VERSION=1.0.45"
set "NEW_VERSION=1.0.46"
set "FILE_LIST=file-list.txt"

:: === PROCESS EACH FILE ===
for /f "usebackq delims=" %%F in ("%FILE_LIST%") do (
    echo Processing %%F
    set "TMP_FILE=%%F.tmp"
    >"!TMP_FILE!" (
        for /f "usebackq delims=" %%L in ("%%F") do (
            set "LINE=%%L"
            setlocal EnableDelayedExpansion
            echo(!LINE:%OLD_VERSION%=%NEW_VERSION%!
            endlocal
        )
    )
    move /Y "!TMP_FILE!" "%%F" >nul
)

echo Done updating versions from %OLD_VERSION% to %NEW_VERSION%.
pause