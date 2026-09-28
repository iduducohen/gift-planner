# Running Gift Planner - Web & Mobile

This guide explains how to run the Gift Planner application in a web browser and on Android.

## Prerequisites

### For Web Browser
- Docker and Docker Compose installed
- Any modern web browser (Chrome, Firefox, Safari, Edge)

### For Android Studio
- Android Studio (latest version)
- Android SDK (API level 24+)
- Java JDK 11 or higher
- Node.js 24+ and npm
- .NET SDK 10.0 (for backend services)

---

## 🌐 Running in Web Browser

### Quick Start (Easiest)

```bash
# In project root directory
JWT_SIGNING_KEY=$(openssl rand -hex 32) docker compose up --build

# On Windows PowerShell:
$env:JWT_SIGNING_KEY = "$(python -c 'import secrets; print(secrets.token_hex(32))')"
docker compose up --build
```

Then open your browser to:
- **Application**: http://localhost:8100
- **API**: http://localhost:5080/api/v1

### Step-by-Step (Without Docker)

**Terminal 1 - Start Backend Services:**

```bash
cd server
dotnet restore
dotnet run --project src/GiftPlanner.Api/GiftPlanner.Api.csproj
```

**Terminal 2 - Start Frontend:**

```bash
cd client
npm install
npm run start
```

Then open: http://localhost:4200

### Access Services

| Service | URL | Credentials |
|---------|-----|-------------|
| Web App | http://localhost:8100 | Dev login available |
| API | http://localhost:5080/api/v1 | - |
| Database | localhost:5432 | gift/gift |
| Redis | localhost:6379 | - |

---

## 📱 Running on Android Studio

### Step 1: Build the Web App

```bash
cd client
npm install
npm run build
```

This creates `client/dist/client/browser/` which Capacitor will use.

### Step 2: Generate Android Project

```bash
cd client

# Initialize Capacitor (if first time)
npx cap init

# Add Android platform
npx cap add android

# Copy built files to Android
npx cap copy
```

### Step 3: Open in Android Studio

```bash
# This opens Android Studio automatically
npx cap open android
```

Or manually:
1. Open Android Studio
2. File → Open
3. Navigate to `client/android/`
4. Select the `android` folder and click OK

### Step 4: Configure Android Project

**In Android Studio:**

1. **Sync Gradle Files**
   - Android Studio will prompt to sync
   - Click "Sync Now"

2. **Select Build Variant**
   - Bottom left: Click "Build Variants"
   - Select "debug" or "release"

3. **Configure Emulator or Device**
   - **Option A (Emulator):**
     - Tools → AVD Manager
     - Create or select an Android Virtual Device
     - Click Play to start emulator
   
   - **Option B (Physical Device):**
     - Enable Developer Mode on your Android phone
     - Enable USB Debugging
     - Connect via USB cable
     - Trust the computer when prompted

### Step 5: Build and Run

**Method 1 (Using Android Studio UI):**
1. Click "Run" button (green play icon) or press Shift+F10
2. Select target device/emulator
3. Click OK

**Method 2 (Using Command Line):**

```bash
cd client/android

# List connected devices/emulators
./gradlew devices

# Build and install
./gradlew installDebug

# Or build APK
./gradlew assembleDebug
# APK will be at: client/android/app/build/outputs/apk/debug/app-debug.apk
```

### Step 6: Run the App

The app will automatically launch on your device/emulator:
- App name: **רגע** (Hebrew: "Moment")
- App ID: `app.giftplanner.rega`

### Step 7: Connect to Backend

The app needs to communicate with the backend API. Configure the URL:

**For Emulator:**
- Emulator localhost (10.0.2.2) automatically maps to host machine
- The app is configured to use: `http://10.0.2.2:5080/api/v1`

**For Physical Device:**
- The app needs your computer's local IP address
- Edit `client/src/app/core/api.service.ts` if needed:
  ```typescript
  const API_BASE = 'http://YOUR_COMPUTER_IP:5080/api/v1';
  ```

**Ensure backend is running:**

```bash
# Terminal - Start backend services
JWT_SIGNING_KEY=$(openssl rand -hex 32) docker compose up --build

# Or without Docker:
cd server
dotnet run --project src/GiftPlanner.Api/GiftPlanner.Api.csproj
```

---

## 🔄 Development Workflow

### Web Development

**Live Reload:**
```bash
cd client
npm run start
# Changes auto-refresh in browser
```

**Build for Production:**
```bash
npm run build
# Output: client/dist/client/browser/
```

### Mobile Development

**After code changes:**

```bash
# 1. Rebuild web assets
cd client
npm run build

# 2. Sync with Capacitor
npx cap copy

# 3. Run on device/emulator
npx cap run android
```

**Or use Capacitor Live Reload:**

```bash
# This serves the app and enables live reload
npx cap run android --live-reload
```

---

## 🔧 Troubleshooting

### Web Browser Issues

**Port 8100 already in use:**
```bash
# Find and kill process
lsof -i :8100 | grep -v COMMAND | awk '{print $2}' | xargs kill -9

# Or change port in docker-compose.yml
```

**Database connection failed:**
```bash
# Check PostgreSQL is running
docker compose logs postgres

# Reset database
docker compose down -v
docker compose up --build
```

**API not responding:**
```bash
# Verify API is running
curl http://localhost:5080/api/v1/health

# Check logs
docker compose logs api
```

### Android Studio Issues

**Gradle sync fails:**
1. File → Invalidate Caches
2. Restart Android Studio
3. Sync Gradle again

**Build fails with "cannot find symbol":**
```bash
# Clean and rebuild
./gradlew clean
./gradlew build
```

**Emulator won't start:**
1. Tools → AVD Manager
2. Right-click device → Wipe Data
3. Click Play again

**App crashes on startup:**
1. Check logcat: View → Tool Windows → Logcat
2. Look for exceptions
3. Ensure backend is running and accessible

**Can't reach backend from emulator:**
```bash
# Test connection
adb shell
ping 10.0.2.2
```

**Physical device can't reach backend:**
1. Get your computer's IP:
   ```bash
   # Windows (PowerShell)
   ipconfig | findstr IPv4
   
   # Mac/Linux
   ifconfig | grep inet
   ```
2. Update API_BASE in api.service.ts
3. Rebuild: `npm run build`
4. Sync: `npx cap copy`
5. Rebuild in Android Studio

---

## 📊 Testing

### Test Web App
```bash
cd client
npm test                    # Run unit tests
npm run test:watch        # Watch mode
```

### Test Mobile App
```bash
# Run tests for Android build
cd client/android
./gradlew testDebug       # Unit tests
./gradlew connectedAndroidTest  # Instrumented tests (requires device/emulator)
```

### E2E Tests
```bash
cd e2e
npm install
npm test
```

---

## 🚀 Deployment

### Web (Docker)
```bash
docker compose build
docker compose push
# Push to registry and deploy to cloud
```

### Mobile (Play Store)
```bash
# Generate signed APK
cd client/android
./gradlew bundleRelease

# Or APK for direct install
./gradlew assembleRelease
```

Output: `client/android/app/build/outputs/`

---

## 📱 App Features

- ✅ User authentication (anonymous + OAuth)
- ✅ Gift recommendations based on:
  - Recipient relationship & age
  - Budget and interests
  - Occasion
  - Previous gifts (history)
- ✅ Save favorites and create plans
- ✅ Track gift history
- ✅ Manage recipients
- ✅ Purchase tracking
- ✅ Responsive design (mobile & web)

---

## 🆘 Getting Help

### View Logs

**Web:**
```bash
docker compose logs -f client
docker compose logs -f api
```

**Mobile:**
- Android Studio → Logcat (View → Tool Windows → Logcat)
- Device logs: `adb logcat`

### Run Tests
```bash
npm test          # Client tests
dotnet test       # Server tests
npm test          # E2E tests (requires services running)
```

### Debug Mode

**Web:**
- Browser DevTools (F12)
- Network tab to monitor API calls
- Console for errors

**Mobile:**
- Android Studio Debugger (Run → Debug 'app')
- Set breakpoints in code
- Inspect variables and call stack

---

## 📝 Configuration

### Backend Configuration
Edit `server/appsettings.Development.json`:
- Database connection
- JWT settings
- Redis connection
- LLM API key (Anthropic)

### Frontend Configuration
Edit `client/src/app/core/api.service.ts`:
- API base URL
- API timeout
- Request interceptors

### Mobile Configuration
Edit `client/capacitor.config.ts`:
- App ID and name
- Web directory
- Server scheme

---

## 🎯 Next Steps

1. **Run in Browser**: Follow "Quick Start" section above
2. **Explore Web App**: Create a profile, search for gift ideas
3. **Run on Android**: Follow Android Studio steps
4. **Run Tests**: Execute test suite to verify everything works
5. **Deploy**: Follow deployment instructions for production

Enjoy using Gift Planner! 🎁
