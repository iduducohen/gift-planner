#!/bin/bash
# Quick start script for running Gift Planner on Android

set -e

echo "🎁 Gift Planner - Android"
echo "========================="
echo ""

# Check prerequisites
echo "📋 Checking prerequisites..."

if ! command -v node &> /dev/null; then
    echo "❌ Node.js is not installed"
    exit 1
fi
echo "✅ Node.js: $(node --version)"

if ! command -v npm &> /dev/null; then
    echo "❌ npm is not installed"
    exit 1
fi
echo "✅ npm: $(npm --version)"

if ! command -v npx &> /dev/null; then
    echo "❌ npx is not installed"
    exit 1
fi

# Check if in client directory
if [ ! -f "package.json" ]; then
    echo ""
    echo "📁 Entering client directory..."
    cd client
fi

echo ""
echo "Step 1️⃣  Installing dependencies..."
npm install --legacy-peer-deps

echo ""
echo "Step 2️⃣  Building web assets..."
npm run build

echo ""
echo "Step 3️⃣  Setting up Capacitor Android platform..."

if [ ! -d "android" ]; then
    echo "   Creating Android platform..."
    npx cap add android
else
    echo "   Android platform already exists"
fi

echo ""
echo "Step 4️⃣  Syncing built files with Capacitor..."
npx cap copy

echo ""
echo "Step 5️⃣  Opening Android Studio..."
npx cap open android

echo ""
echo "✨ Android Studio is opening!"
echo ""
echo "📝 Next steps in Android Studio:"
echo "   1. Wait for Gradle sync to complete"
echo "   2. Select an Android Virtual Device or connect a physical device"
echo "   3. Click Run (green play button) or press Shift+F10"
echo "   4. The app will build and launch on your device/emulator"
echo ""
echo "💡 Tip: Make sure the backend API is running:"
echo "   JWT_SIGNING_KEY=\$(openssl rand -hex 32) docker compose up --build"
