@echo off
setlocal enabledelayedexpansion

REM Quick start script for running Gift Planner in browser on Windows

echo.
echo 🎁 Gift Planner - Web Browser
echo =============================
echo.

REM Generate JWT signing key if not provided
if "!JWT_SIGNING_KEY!"=="" (
    echo 📝 Generating JWT signing key...
    REM Generate a random 32-byte hex string
    for /f "delims=" %%A in ('powershell -Command "[System.BitConverter]::ToString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)) -replace '-','' | ToLower"') do (
        set "JWT_SIGNING_KEY=%%A"
    )
    echo ✅ Key generated: !JWT_SIGNING_KEY:~0,16!...
) else (
    echo ✅ Using provided JWT_SIGNING_KEY
)

echo.
echo 🐳 Starting Docker Compose services...
echo    - PostgreSQL ^(5432^)
echo    - Redis ^(6379^)
echo    - API ^(5080^)
echo    - Client ^(8100^)
echo.

docker compose up --build

echo.
echo ✨ Gift Planner is ready!
echo.
echo 📍 Open in browser: http://localhost:8100
echo.
echo Press Ctrl+C to stop services
echo.
pause
