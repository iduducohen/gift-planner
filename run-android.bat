@echo off
setlocal enabledelayedexpansion

REM Quick start script for running Gift Planner on Android on Windows

echo.
echo 🎁 Gift Planner - Android
echo =========================
echo.

REM Check prerequisites
echo 📋 Checking prerequisites...

where node >nul 2>nul
if errorlevel 1 (
    echo ❌ Node.js is not installed
    echo    Download from: https://nodejs.org
    pause
    exit /b 1
)
for /f "tokens=*" %%A in ('node --version') do set NODE_VERSION=%%A
echo ✅ Node.js: !NODE_VERSION!

where npm >nul 2>nul
if errorlevel 1 (
    echo ❌ npm is not installed
    pause
    exit /b 1
)
for /f "tokens=*" %%A in ('npm --version') do set NPM_VERSION=%%A
echo ✅ npm: !NPM_VERSION!

where npx >nul 2>nul
if errorlevel 1 (
    echo ❌ npx is not installed
    pause
    exit /b 1
)
echo ✅ npx available

REM Check if in client directory
if not exist "package.json" (
    echo.
    echo 📁 Entering client directory...
    cd client
)

echo.
echo Step 1️⃣  Installing dependencies...
call npm install --legacy-peer-deps
if errorlevel 1 (
    echo ❌ Failed to install dependencies
    pause
    exit /b 1
)

echo.
echo Step 2️⃣  Building web assets...
call npm run build
if errorlevel 1 (
    echo ❌ Failed to build web assets
    pause
    exit /b 1
)

echo.
echo Step 3️⃣  Setting up Capacitor Android platform...

if not exist "android" (
    echo    Creating Android platform...
    call npx cap add android
    if errorlevel 1 (
        echo ❌ Failed to add Android platform
        pause
        exit /b 1
    )
) else (
    echo    Android platform already exists
)

echo.
echo Step 4️⃣  Syncing built files with Capacitor...
call npx cap copy
if errorlevel 1 (
    echo ❌ Failed to sync files
    pause
    exit /b 1
)

echo.
echo Step 5️⃣  Opening Android Studio...
call npx cap open android
if errorlevel 1 (
    echo ⚠️  Could not auto-open Android Studio
    echo    Open manually: android/
)

echo.
echo ✨ Android Studio is opening!
echo.
echo 📝 Next steps in Android Studio:
echo    1. Wait for Gradle sync to complete
echo    2. Select an Android Virtual Device or connect a physical device
echo    3. Click Run ^(green play button^) or press Shift+F10
echo    4. The app will build and launch on your device/emulator
echo.
echo 💡 Tip: Make sure the backend API is running:
echo    run-web.bat
echo.
pause
