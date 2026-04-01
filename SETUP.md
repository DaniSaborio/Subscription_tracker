# Subscription Tracker MVP - Setup & Execution Guide

## Prerequisites

Ensure you have installed:
- **Docker & Docker Compose** (for PostgreSQL)
- **.NET 10.0 SDK**
- **MAUI workload**: `dotnet workload install maui`
- **Android SDK** (if targeting Android)

## Project Structure

```
Subscription_tracker/
├── docker-compose.yml          # PostgreSQL + API containers
├── Subscription_tracker.sln     # Solution file
├── Subscription_tracker/        # MAUI Frontend App
│   ├── MauiProgram.cs
│   ├── App.xaml.cs
│   ├── AppShell.xaml
│   ├── MainPage.xaml / MainPage.xaml.cs
│   ├── Models/SharedModels.cs
│   ├── Pages/ (LoginPage, RegisterPage, AddSubscriptionPage)
│   ├── Services/ (ApiService, TokenService, LocalStorageService, SyncService)
│   └── Subscription_tracker.csproj
└── Subscription_tracker.API/    # .NET Core Backend API
    ├── Program.cs
    ├── appsettings.json
    ├── Models/ (User, Subscription)
    ├── DTOs/ (DTOs.cs)
    ├── Data/ (AppDbContext.cs)
    ├── Controllers/ (AuthController, SubscriptionsController)
    ├── Services/ (JwtTokenService.cs)
    ├── Dockerfile
    └── Subscription_tracker.API.csproj
```

---

## Step 1: Start Infrastructure (Docker)

From the **project root** directory:

```bash
# Start PostgreSQL + API
docker compose up -d

# Verify containers are running
docker compose ps

# View API logs
docker compose logs -f api
```

**Expected output:**
- PostgreSQL listening on `localhost:5432`
- API running on `http://localhost:5000`
- Swagger UI available at `http://localhost:5000/swagger`

### Database Access (Optional)
```bash
docker exec -it subscription_tracker_db psql -U admin -d subscription_tracker

# Inside psql:
\dt                    # List tables
\d users               # Describe users table
SELECT * FROM users;   # Query users
```

---

## Step 2: Build & Run MAUI Frontend

In the project root:

```bash
# Restore NuGet packages

# Build the MAUI app
dotnet build -f net10.0-android

# Run the app (Android emulator)
dotnet run -f net10.0-android
```

Or from **Visual Studio**:
1. Open `Subscription_tracker.sln`
2. Set **Subscription_tracker** as startup project
3. Select target (Android emulator)
4. Press `F5` or click **Run**

---

## Step 3: Test the MVP

### 3.1 Register a New User
1. App launches → **Login Page**
2. Click **"Register"**
3. Enter:
   - Email: `test@example.com`
   - Password: `Password123!`
   - Confirm Password: `Password123!`
4. Click **Register**
5. → Redirects to **MainPage** (authenticated)

### 3.2 Add a Subscription
1. On **MainPage**, click **"+"** button (top-right)
2. Fill in form:
   - Service Name: `Netflix`
   - Category: `Entertainment`
   - Amount: `15.99`
   - Billing Cycle: `Monthly`
   - Next Billing Date: (tomorrow)
   - Notes: `Family plan`
3. Click **Save**
4. Subscription appears in list
5. Monthly total updates dynamically

### 3.3 Test Search & Filter
- **Search Bar**: Type "Net" → filters to Netflix
- **Billing Cycle Filter**: Select "Monthly" → shows only monthly subs

### 3.4 Test Offline Sync
1. **Online state**: Add a subscription → syncs to API + saved locally
2. **Offline state**:
   - Disconnect internet (dev tools or airplane mode)
   - Add another subscription
   - Changes saved locally (pending sync)
3. **Re-connect**:
   - Restore internet
   - App auto-syncs pending changes to API
   - Data refreshes from server

### 3.5 Delete Subscription
1. Long-tap or select a subscription
2. Choose "Delete" from action sheet
3. Confirm deletion
4. Subscription removed

---

## API Testing (Postman)

### 1. Register User
```
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "email": "user@test.com",
  "password": "Password123!",
  "confirmPassword": "Password123!"
}

Response:
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": 1,
    "email": "user@test.com"
  }
}
```

### 2. Login
```
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "user@test.com",
  "password": "Password123!"
}
```

### 3. Get Subscriptions (Protected)
```
GET http://localhost:5000/api/subscriptions
Authorization: Bearer <token_from_login>
```

### 4. Create Subscription
```
POST http://localhost:5000/api/subscriptions
Authorization: Bearer <token>
Content-Type: application/json

{
  "serviceName": "Spotify",
  "category": "Music",
  "amount": 9.99,
  "billingCycle": "Monthly",
  "nextBillingDate": "2026-05-01T00:00:00Z",
  "isActive": true,
  "paymentMethod": "Credit Card",
  "notes": "Personal account"
}
```

---

## Troubleshooting

### Docker Issues

**Error: `Connection refused`**
```bash
# Ensure Docker is running
docker ps

# Restart containers
docker-compose restart

# Check logs
docker-compose logs api
```

**PostgreSQL connection failed**
```bash
# Wait for healthcheck to pass (may take 10 seconds)
docker-compose logs postgres | grep "ready to accept"

# Force recreate
docker-compose down -v
docker-compose up -d
```

### MAUI Build Errors

**Missing Android SDK**
```bash
dotnet sdk check
dotnet workload repair
dotnet workload install android
```

**Port 5000 already in use**
```bash
# Find process using port
lsof -i :5000

# Kill process
kill -9 <PID>

# Or use different port in appsettings.json
```

### API Errors

**JWT Token Invalid**
- Ensure token is fresh (not expired)
- Check `Jwt:SecretKey` in `appsettings.json`

**CORS Error**
- Verify `AllowLocalhost` CORS policy in `Program.cs`
- Add device/emulator IP if not localhost

---

## Key Features Implemented

✅ **Authentication**: JWT-based login/register
✅ **CRUD Operations**: Create, read, update, delete subscriptions
✅ **Offline Support**: Local SQLite storage + sync queue
✅ **Auto-sync**: Pending changes sync when connection restored
✅ **Search & Filter**: By service name, billing cycle
✅ **Responsive UI**: Mobile-first MAUI design
✅ **Docker**: PostgreSQL + API in containers

---

## Next Steps (Future Enhancements)

- 📱 Push notifications for upcoming billing dates
- 📊 Analytics dashboard with spending trends
- 💾 Export to PDF/CSV
- 🌙 Dark mode theme
- 🔄 Multi-device sync
- 👥 Shared subscriptions (family plans)

---

## Environment Variables & Configuration

**API Configuration** (`appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=postgres;Port=5432;Database=subscription_tracker;..."
  },
  "Jwt": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "subscription-tracker-api",
    "Audience": "subscription-tracker-app",
    "ExpirationMinutes": 1440
  }
}
```

**MAUI Configuration**:
- API Base URL: `http://localhost:5000` (in `ApiService.cs`)
- Modify for production endpoints

---

## Architecture Overview

```
┌─────────────────┐
│   MAUI App      │  ← Frontend (Android/iOS native)
│  (C#/XAML)      │
└────────┬────────┘
         │ HTTP(S)
         ↓
┌──────────────────────────┐
│  ASP.NET Core API        │
│  (JWT Auth Protected)    │
└────────┬─────────────────┘
         │
         ↓
┌──────────────────────────┐
│  PostgreSQL Database     │
│  (Docker Container)      │
└──────────────────────────┘
```

**Flow**: MAUI ← (API calls) → Backend ← (EF Core) → PostgreSQL

---

## Support

For issues or questions, refer to:
- Plan: `/home/sabo/.claude/plans/linked-soaring-wolf.md`
- Backend logs: `docker-compose logs api`
- Frontend logs: Visual Studio Output window
- API Swagger: `http://localhost:5000/swagger`
