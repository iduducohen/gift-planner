@echo off
REM Start Gift Planner services with Docker Compose

echo.
echo 🚀 Starting Gift Planner Services...
echo.

REM Generate JWT signing key if not set
if "%JWT_SIGNING_KEY%"=="" (
    echo 📝 Generating JWT signing key...
    REM Generate a random 32-character hex string
    for /f "delims=" %%A in ('powershell -Command "[System.BitConverter]::ToString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)) -replace '-','' | ToLower"') do (
        set JWT_SIGNING_KEY=%%A
    )
    echo ✅ JWT_SIGNING_KEY generated
) else (
    echo ✅ Using provided JWT_SIGNING_KEY
)

echo.
echo 🐳 Starting Docker Compose...
docker compose up --build

echo.
echo ✅ Services started!
echo.
echo 📍 Access the application at: http://localhost:8100
echo 🔌 API endpoint: http://localhost:5080/api/v1
echo 🗄️  Database: localhost:5432 (user: gift, password: gift^)
echo 💾 Cache: localhost:6379
echo.
echo To run tests in another terminal:
echo   cd client ^&^& npm test
echo   cd server ^&^& dotnet test tests/GiftPlanner.Application.Tests/
echo   cd e2e ^&^& npm test
pause
