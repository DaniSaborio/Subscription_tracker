# Subscription Tracker MVP

A modern Progressive Web App built with .NET MAUI and ASP.NET Core that helps users manage and track their recurring subscriptions with offline support and automatic synchronization.

## 🎯 Project Overview

**Subscription Tracker** is a subscription management MVP that allows users to:
- ✅ Register and login with JWT authentication
- ✅ Add, edit, and delete subscriptions
- ✅ View monthly and bilingual costs
- ✅ Search and filter subscriptions by category or billing cycle
- ✅ Use the app offline with automatic sync when reconnected
- ✅ Track upcoming billing dates

## 🏗️ Architecture

```
┌──────────────────┐
│  MAUI App        │  (Frontend - C#/XAML)
│  Android/iOS     │
└────────┬─────────┘
         │ HTTP(S)
         ↓
┌──────────────────────────────┐
│  ASP.NET Core Web API        │
│  - JWT Authentication        │
│  - REST Endpoints            │
│  - Entity Framework Core     │
└────────┬─────────────────────┘
         │
         ↓
┌──────────────────────────────┐
│  PostgreSQL (Docker)         │
│  - Users                     │
│  - Subscriptions             │
└──────────────────────────────┘
```

## 📋 Tech Stack

### Frontend
- **Framework**: .NET MAUI (Multi-platform App UI)
- **Language**: C# 12
- **UI**: XAML
- **Storage**: SQLite (local) + SecureStorage (tokens)
- **Networking**: HttpClient + custom ApiService

### Backend
- **Framework**: ASP.NET Core 10.0 (Web API)
- **Language**: C# 12
- **Authentication**: JWT Bearer tokens
- **ORM**: Entity Framework Core
- **Database**: PostgreSQL 16
- **Containerization**: Docker & Docker Compose

## 🚀 Quick Start

### Prerequisites
- Docker & Docker Compose
- .NET 10.0 SDK
- MAUI workload: `dotnet workload install maui`
- Android SDK (for Android target)

### 1. Start Docker Containers
```bash
cd Subscription_tracker
docker-compose up -d
```

### 2. Run MAUI App
```bash
dotnet run -f net10.0-android
```

### 3. Access API (Optional)
- Swagger UI: `http://localhost:5000/swagger`
- Base URL: `http://localhost:5000/api`

See **[SETUP.md](./SETUP.md)** for detailed instructions.

## 📁 Project Structure

```
Subscription_tracker/
├── 📄 docker-compose.yml       # PostgreSQL + API containers
├── 📄 SETUP.md                 # Detailed setup guide
├── 📄 README.md                # This file
│
├── 📁 Subscription_tracker/    # MAUI Frontend
│   ├── Models/
│   │   └── SharedModels.cs     # User, Subscription, DTOs
│   ├── Pages/
│   │   ├── LoginPage.xaml      # Authentication UI
│   │   ├── RegisterPage.xaml   # Registration UI
│   │   ├── AddSubscriptionPage.xaml  # Add subscription form
│   │   └── MainPage.xaml       # Dashboard & list view
│   ├── Services/
│   │   ├── ApiService.cs       # HTTP client for API calls
│   │   ├── TokenService.cs     # JWT token management
│   │   ├── LocalStorageService.cs   # Offline storage
│   │   └── SyncService.cs      # Offline sync logic
│   ├── MainPage.xaml.cs        # Dashboard logic
│   ├── MauiProgram.cs          # DI & app initialization
│   └── AppShell.xaml           # Navigation structure
│
└── 📁 Subscription_tracker.API/  # ASP.NET Core Backend
    ├── Models/
    │   ├── User.cs             # User entity
    │   └── Subscription.cs     # Subscription entity
    ├── DTOs/
    │   └── DTOs.cs             # Data transfer objects
    ├── Data/
    │   └── AppDbContext.cs    # Entity Framework DbContext
    ├── Controllers/
    │   ├── AuthController.cs   # Login/Register endpoints
    │   └── SubscriptionsController.cs  # CRUD endpoints
    ├── Services/
    │   └── JwtTokenService.cs  # JWT token generation
    ├── Program.cs              # API configuration
    ├── appsettings.json        # Configuration
    ├── Dockerfile              # Container build
    └── Subscription_tracker.API.csproj
```

## 🔑 Key Features

### Authentication
- User registration with email and password
- JWT token-based authentication
- Secure token storage in device's secure storage
- Token expiration handling (24 hours)

### Subscriptions Management
- Full CRUD operations (Create, Read, Update, Delete)
- Support for monthly and biweekly billing cycles
- Category classification (Entertainment, Education, etc.)
- Upcoming billing date tracking
- Custom notes and payment method recording

### Offline Support
- Local SQLite database for offline access
- Pending changes queue for offline operations
- Auto-sync when connection is restored
- Conflict resolution (last-write-wins)

### UI/UX
- Mobile-first design responsive layout
- Real-time search and filtering
- Summary dashboard with expense totals
- Connectivity status indicator
- Loading states and error handling

## 🔌 API Endpoints

### Authentication
```
POST   /api/auth/register      # Register new user
POST   /api/auth/login         # Login and get JWT token
```

### Subscriptions (Protected)
```
GET    /api/subscriptions      # Get all user subscriptions
GET    /api/subscriptions/{id} # Get specific subscription
POST   /api/subscriptions      # Create new subscription
PUT    /api/subscriptions/{id} # Update subscription
DELETE /api/subscriptions/{id} # Delete subscription
POST   /api/subscriptions/sync # Sync offline changes
```

## 🧪 Testing

### Postman Collection
Use Postman to test API endpoints:
1. Create collection with endpoints listed above
2. Add Authorization header: `Bearer <jwt_token>`
3. Test CRUD operations

### App Testing
1. **User Flow**: Register → Login → Add subscription → View dashboard
2. **Offline Test**: Disconnect internet → Add subscription → Reconnect
3. **Sync Validation**: Verify offline changes sync to server

See [SETUP.md](./SETUP.md#step-3-test-the-mvp) for detailed test procedures.

## 🔒 Security

- **Password Hashing**: SHA256 with salt
- **JWT Authentication**: HS256 algorithm, 24-hour expiration
- **Secure Storage**: Platform-specific secure storage for tokens
- **HTTPS Ready**: Configure in production
- **CORS Protection**: Limited to trusted origins

## 📊 Database Schema

### Users Table
```sql
CREATE TABLE users (
  id SERIAL PRIMARY KEY,
  email VARCHAR(255) UNIQUE NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  created_at TIMESTAMP DEFAULT NOW()
);
```

### Subscriptions Table
```sql
CREATE TABLE subscriptions (
  id SERIAL PRIMARY KEY,
  user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  service_name VARCHAR(255) NOT NULL,
  category VARCHAR(100) NOT NULL,
  amount DECIMAL(10,2) NOT NULL,
  billing_cycle VARCHAR(50) NOT NULL,
  next_billing_date TIMESTAMP NOT NULL,
  is_active BOOLEAN DEFAULT TRUE,
  payment_method VARCHAR(255),
  notes TEXT,
  updated_at TIMESTAMP DEFAULT NOW()
);
```

## 🐛 Troubleshooting

### Docker
```bash
# View logs
docker-compose logs api

# Restart containers
docker-compose restart

# Stop containers
docker-compose down
```

### MAUI Build
```bash
# Install workload
dotnet workload install maui

# Clean build
dotnet clean && dotnet build -f net10.0-android
```

See [SETUP.md](./SETUP.md#troubleshooting) for more solutions.

## 📈 Development Status

### MVP Completed ✅
- [x] User authentication (register/login)
- [x] Subscription CRUD
- [x] Offline storage with SQLite
- [x] Sync mechanism
- [x] Search & filtering
- [x] Responsive UI
- [x] Docker containerization

### Future Enhancements
- [ ] Push notifications
- [ ] Spending analytics dashboard
- [ ] PDF/CSV export
- [ ] Dark mode
- [ ] Multi-device sync
- [ ] Family/shared subscriptions
- [ ] Subscription recommendations

## 🤝 Contributing

This is an educational MVP project. Contributions are welcome for:
- Bug fixes
- UI/UX improvements
- Performance optimization
- Documentation enhancements

## 📄 License

Project for educational purposes.

