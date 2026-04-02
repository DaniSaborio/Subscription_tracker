# Subscription Tracker MVP - Setup & Execution Guide

## ⚡ Quick Start (5 minutes)

```bash
# 1. Start containers
docker compose up -d

# 2. Wait for PostgreSQL healthcheck
sleep 10

# 3. Verify API is running
curl http://localhost:5000/swagger

# 4. Test registration
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123","confirmPassword":"Test123"}'
```

---

## Prerequisites

Ensure you have installed:
- **Docker & Docker Compose** (check: `docker --version` && `docker compose --version`)
- **.NET 10.0 SDK** (check: `dotnet --version`)
- **MAUI workload**: `dotnet workload install maui`
- **Android SDK** (if targeting Android)

### On Linux (Ubuntu/Debian):
```bash
# Docker
sudo apt-get install docker.io docker-compose
sudo systemctl start docker
sudo usermod -aG docker $USER  # Allow non-root docker commands

# .NET 10.0
wget https://dot.net/v1/dotnet-install.sh
sudo bash dotnet-install.sh --channel 10.0

# MAUI
dotnet workload install maui android
```

## Project Structure

```
Subscription_tracker/
├── docker-compose.yml          # PostgreSQL + API containers
├── Subscription_tracker.sln     # Solution file (MAUI + API)
├── Subscription_tracker/        # MAUI Frontend App
│   ├── MauiProgram.cs          # DI configuration
│   ├── MainPage.xaml / .cs     # Dashboard (list subscriptions)
│   ├── Models/SharedModels.cs  # Shared DTOs
│   ├── Pages/
│   │   ├── LoginPage.xaml / .cs
│   │   ├── RegisterPage.xaml / .cs
│   │   └── AddSubscriptionPage.xaml / .cs
│   ├── Services/
│   │   ├── ApiService.cs            # HTTP client to API
│   │   ├── TokenService.cs          # JWT token storage
│   │   ├── LocalStorageService.cs   # SQLite offline cache
│   │   └── SyncService.cs           # Offline/online sync
│   └── Subscription_tracker.csproj
└── Subscription_tracker.API/    # ASP.NET Core Backend API
    ├── Program.cs               # DI + JWT configuration
    ├── appsettings.json         # Connection strings & JWT
    ├── Models/ (User, Subscription)
    ├── Data/AppDbContext.cs     # Entity Framework
    ├── Controllers/
    │   ├── AuthController.cs    # POST /register, /login
    │   └── SubscriptionsController.cs  # GET/POST/PUT/DELETE
    ├── Services/JwtTokenService.cs
    ├── Dockerfile
    └── Subscription_tracker.API.csproj
```

---

## Step 1: Verify Docker Installation

### Check Docker is running:
```bash
docker ps
# Should list running containers (may be empty)

docker compose version
# Should show: Docker Compose version vX.Y.Z
```

### If Docker not running (Linux):
```bash
sudo systemctl start docker
sudo usermod -aG docker $USER
# Log out and back in, or: newgrp docker
```

---

## Step 2: Start Infrastructure (Docker + PostgreSQL + API)

From the **project root** directory:

```bash
# Start all containers
docker compose up -d

# Verify all containers are healthy
docker compose ps
# Expected:
#   subscription_tracker_db    Healthy
#   subscription_tracker_api   Running
```

### Check logs if containers don't start:
```bash
# View API logs
docker compose logs api

# View PostgreSQL logs
docker compose logs db

# Full logs with timestamp
docker compose logs -f --timestamps
```

### Wait for PostgreSQL to be ready:
```bash
# Check healthcheck status (wait ~10 seconds)
for i in {1..30}; do
  docker exec subscription_tracker_db pg_isready -U admin && break
  sleep 1
done

echo "Database ready!"
```

**Expected after ~10 seconds:**
- PostgreSQL listening on `localhost:5432`
- API running on `http://localhost:5000/swagger`
- API accepting requests

---

## Step 3: Test API Endpoints (From Curl/Postman)

### 3.1 Check API is alive:
```bash
curl -i http://localhost:5000/swagger
# Should return 200 OK with HTML
```

### 3.2 Test Registration:
```bash
RESPONSE=$(curl -s -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email":"testuser@example.com",
    "password":"SecurePass123",
    "confirmPassword":"SecurePass123"
  }')

echo "$RESPONSE"
# Expected output:
# {
#   "token": "eyJhbGciOiJIUzI1NiIs...",
#   "user": {"id": 1, "email": "testuser@example.com"}
# }
```

### 3.3 Extract and save token:
```bash
TOKEN=$(echo "$RESPONSE" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
echo "Token saved: $TOKEN"
```

### 3.4 Test Protected Endpoint (Get Subscriptions):
```bash
curl -X GET http://localhost:5000/api/subscriptions \
  -H "Authorization: Bearer $TOKEN" \
  -w "\nStatus: %{http_code}\n"

# Expected: 200 OK, empty array: []
```

### 3.5 Create a Subscription:
```bash
curl -X POST http://localhost:5000/api/subscriptions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "serviceName": "Netflix",
    "category": "Entertainment",
    "amount": 15.99,
    "billingCycle": "Monthly",
    "nextBillingDate": "2026-05-01T00:00:00Z",
    "isActive": true,
    "paymentMethod": "Credit Card",
    "notes": "Family plan"
  }'

# Expected: 201 Created with subscription ID
```

### 3.6 Verify subscription was created:
```bash
curl -X GET http://localhost:5000/api/subscriptions \
  -H "Authorization: Bearer $TOKEN"

# Should show the Netflix subscription in the array
```

---

## Step 4: Build & Run MAUI Frontend

### Option A: Command line:
```bash
# From project root (NOT the API folder)
cd /home/sabo/GIT/Subscription_tracker

# Restore NuGet packages
dotnet restore

# Build for Android
dotnet build Subscription_tracker.csproj -f net10.0-android

# If you have Android emulator running:
dotnet run -f net10.0-android
```

### Option B: Visual Studio / VS Code:
1. Open `Subscription_tracker.sln`
2. Select **Subscription_tracker** project (not the API)
3. Choose target: **Android Emulator** (or connected device)
4. Press `F5` or click **Run**

### If no emulator available:
```bash
# List available emulators
emulator -list-avds

# Start an emulator
emulator -avd Pixel_API_36 &

# Then run the app
dotnet run -f net10.0-android
```

**Note for Android Emulator**:
- If API calls fail, your `Services/ApiService.cs` may need adjustment
- Emulator uses `http://10.0.2.2:5000` to access host localhost
- Device uses `http://<your-pc-ip>:5000`

---

## Step 5: End-to-End Test in MAUI App

### 5.1 Launch & Register
1. **App starts** → **Login Page** appears
2. Click **"Register"** link
3. **Register Page**:
   - Email: `testuser@example.com`
   - Password: `SecurePass123`
   - Confirm: `SecurePass123`
   - Click **Register**
4. **Expected**: App navigates to **MainPage** (authenticated)

### 5.2 Add First Subscription
1. On **MainPage**, click **"+"** button (add subscription)
2. **Add Subscription Page**:
   - Service Name: `Netflix`
   - Category: `Entertainment`
   - Amount: `15.99`
   - Billing Cycle: `Monthly`
   - Next Billing Date: *(auto-set to tomorrow)*
   - Payment Method: `Credit Card`
   - Notes: `Family plan`
   - Click **Save**
3. **Expected**:
   - Subscription appears in list
   - Monthly total updates: `$15.99`
   - Count shows: `1 active`

### 5.3 Add More Subscriptions
Repeat, adding:
- **Spotify**: $9.99 Monthly
- **Adobe CC**: $54.99 Monthly

**Expected**: Dashboard shows ~$80 monthly total

### 5.4 Test Search
1. In search bar, type `Net`
2. List filters to show only Netflix
3. Clear search → shows all again

### 5.5 Test Filter by Billing Cycle
1. **Billing Cycle Picker** → Select `Monthly`
2. Shows all monthly subscriptions
3. Select `Biweekly` (if any exist) → shows only those

### 5.6 Test Delete
1. Tap/long-press a subscription in the list
2. Action sheet appears → **Delete**
3. Confirm: `Yes, delete this subscription`
4. Subscription removed from list

### 5.7 Test Offline Sync
1. **Online**: Add a subscription → confirms immediately
2. **Offline** (disable WiFi):
   - Status indicator shows "**Offline**" (orange)
   - Add another subscription
   - Changes saved locally
3. **Re-connect** (enable internet):
   - Status changes to "**Online**" (green)
   - App auto-syncs pending changes
   - Data refreshes from server

---

## API Testing with curl / Postman

### Save token for reuse:
```bash
# After registration or login, save the token
TOKEN="eyJhbGciOiJIUzI1NiIs..."

# Or extract it:
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"testuser@example.com","password":"SecurePass123"}' \
  | grep -o '"token":"[^"]*' | cut -d'"' -f4)
```

### Test endpoints:

**1. POST /api/auth/register**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email":"newuser@test.com",
    "password":"Test123",
    "confirmPassword":"Test123"
  }'
```

**2. POST /api/auth/login**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email":"testuser@example.com",
    "password":"SecurePass123"
  }'
```

**3. GET /api/subscriptions** (Protected)
```bash
curl -X GET http://localhost:5000/api/subscriptions \
  -H "Authorization: Bearer $TOKEN"
```

**4. POST /api/subscriptions** (Protected)
```bash
curl -X POST http://localhost:5000/api/subscriptions \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "serviceName":"Hulu",
    "category":"Entertainment",
    "amount":7.99,
    "billingCycle":"Monthly",
    "nextBillingDate":"2026-05-15T00:00:00Z",
    "isActive":true,
    "paymentMethod":"Credit Card"
  }'
```

**5. PUT /api/subscriptions/{id}** (Protected)
```bash
curl -X PUT http://localhost:5000/api/subscriptions/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "serviceName":"Netflix Premium",
    "category":"Entertainment",
    "amount":19.99,
    "billingCycle":"Monthly",
    "nextBillingDate":"2026-05-01T00:00:00Z",
    "isActive":true
  }'
```

**6. DELETE /api/subscriptions/{id}** (Protected)
```bash
curl -X DELETE http://localhost:5000/api/subscriptions/1 \
  -H "Authorization: Bearer $TOKEN"

# Expected: 200 OK or 204 No Content
```

---

## Database Access (Optional)

### Connect directly to PostgreSQL:
```bash
docker exec -it subscription_tracker_db psql -U admin -d subscription_tracker

# Inside psql:
\dt                              # List all tables
\d users                         # Show users table structure
\d subscriptions                 # Show subscriptions table structure

SELECT * FROM users;             # List all users
SELECT * FROM subscriptions;     # List all subscriptions
SELECT COUNT(*) FROM subscriptions;  # Count subscriptions

# Exit
\q
```

### Backup & Restore:
```bash
# Backup database
docker exec subscription_tracker_db pg_dump -U admin -d subscription_tracker > backup.sql

# Restore from backup
docker exec -i subscription_tracker_db psql -U admin -d subscription_tracker < backup.sql
```

---

## Troubleshooting

### Docker Issues

**Error: `Connection refused` connecting to `localhost:5000`**
```bash
# Verify containers are running
docker compose ps

# If API is not running:
docker compose up -d

# Check why it exited:
docker compose logs api --tail=100
```

**Error: `PostgreSQL connection timeout`**
```bash
# Wait for healthcheck (may take 10-15 seconds)
docker compose ps
# If still "starting", wait more

# Restart if needed:
docker compose restart db
docker compose restart api
```

**Error: `Port 5000 already in use`**
```bash
# Find what's using port 5000
lsof -i :5000

# Kill process
kill -9 <PID>

# Or change port in docker-compose.yml:
# ports: "5001:8080"  # Use 5001 instead
```

### MAUI Build Errors

**Error: `Cannot find TargetFramework net10.0-android`**
```bash
# Install/repair MAUI workload
dotnet workload install maui
dotnet workload install android

# Or repair
dotnet workload repair
```

**Error: `API calls return 401 Unauthorized`**
1. Token may be expired (24-hour expiry)
2. Try registering/logging in again
3. Check token is in Authorization header: `Bearer <token>`

**Error: `CORS error` or `Connection refused from app`**
- On **Android emulator**: Change AP URL from `localhost` to `10.0.2.2` (special host alias)
- On **physical device**: Use your PC's IP address, e.g., `http://192.168.1.100:5000`
- Edit `Services/ApiService.cs` _baseUrl

### API Connection Issues

**From MAUI app: API calls fail with `Connection refused`**
1. Verify API URL in `Services/ApiService.cs`
2. For emulator: `http://10.0.2.2:5000`
3. For device: `http://<your-pc-ip>:5000`
4. Check firewall isn't blocking port 5000

**Fix for Android Emulator**:
```csharp
// In Services/ApiService.cs
var baseUrl = DeviceInfo.Platform == DevicePlatform.Android
    ? "http://10.0.2.2:5000"   // Emulator special alias
    : "http://localhost:5000";  // Desktop
```

---

## Key Features Implemented

✅ **Authentication**: JWT token-based login/register
✅ **CRUD**: Create, read, update, delete subscriptions
✅ **Offline Support**: Local SQLite cache + sync queue
✅ **Auto-sync**: Pending changes sync when connection restored
✅ **Search & Filter**: By service name, billing cycle, category
✅ **Responsive UI**: Mobile-first MAUI design
✅ **Docker**: PostgreSQL + .NET API in containers
✅ **API Docs**: Swagger UI at `http://localhost:5000/swagger`

---

## Architecture Overview

```
┌──────────────────────────┐
│   MAUI Android App       │
│  (C# XAML + Services)    │
└────────────┬─────────────┘
             │ HTTP(S)
             │ (JWT Bearer Token)
             ↓
┌──────────────────────────────────┐
│  ASP.NET Core 10.0 API           │
│  - AuthController                │
│  - SubscriptionsController       │
│  - JWT Bearer auth               │
└────────────┬─────────────────────┘
             │ (EF Core)
             ↓
┌──────────────────────────────────┐
│  PostgreSQL 16 (Docker)          │
│  - Users table                   │
│  - Subscriptions table           │
└──────────────────────────────────┘
```

**Offline Flow:**
```
MAUI (offline)
  ↓
LocalStorageService (SQLite)
  ↓
Pending changes queue (JSON file)
  ↓
(connection restored)
  ↓
SyncService (auto-triggers)
  ↓
API (/api/subscriptions/sync)
  ↓
PostgreSQL (updates synced)
```

---

## Environment & Configuration

### API Config (`Subscription_tracker.API/appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=postgres;Port=5432;Database=subscription_tracker;User Id=admin;Password=subscription_tracker_dev;"
  },
  "Jwt": {
    "SecretKey": "your-super-secret-key-here-at-least-32-characters",
    "Issuer": "subscription-tracker-api",
    "Audience": "subscription-tracker-app",
    "ExpirationMinutes": 1440
  }
}
```

### MAUI Config (`Services/ApiService.cs`):
```csharp
// Default for localhost development:
_baseUrl = "http://localhost:5000";

// For Android emulator:
_baseUrl = "http://10.0.2.2:5000";

// For production deployed API:
_baseUrl = "https://api.subscription-tracker.com";
```

---

## Next Steps (Future Enhancements)

- 📱 Push notifications for upcoming billing dates
- 📊 Analytics dashboard with spending trends
- 💾 Export to PDF/CSV
- 🌙 Dark mode theme
- 🔄 Multi-device sync
- 👥 Shared subscriptions

---

## Support & Resources

- **Complete Plan**: `/home/sabo/.claude/plans/linked-soaring-wolf.md`
- **API Logs**: `docker compose logs -f api`
- **App Logs**: Visual Studio Output / Device logcat
- **DB Access**: `docker exec -it subscription_tracker_db psql -U admin`
- **Swagger Docs**: `http://localhost:5000/swagger`
- **Docker Status**: `docker compose ps`
