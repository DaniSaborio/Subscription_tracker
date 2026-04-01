# 📱 MVP Subscription Tracker - Status Report

**Fecha**: 1 de Abril, 2026
**Estado**: ✅ **COMPLETE**

---

## 🎯 Resumen

Se ha implementado un **MVP funcional** del Subscription Tracker con:
- ✅ Backend API en ASP.NET Core (.NET 10.0) con JWT
- ✅ Frontend MAUI con soporte offline/online
- ✅ PostgreSQL en Docker
- ✅ Sincronización automática de cambios
- ✅ Autenticación JWT
- ✅ CRUD completo de suscripciones

---

## 🏗️ Arquitectura Implementada

### Backend (Docker)
```
http://localhost:5000
├── POST /api/auth/register    → Registrar usuario
├── POST /api/auth/login        → Login con JWT
├── GET  /api/subscriptions     → Listar (protegido)
├── POST /api/subscriptions     → Crear
├── PUT  /api/subscriptions/{id} → Actualizar
├── DELETE /api/subscriptions/{id} → Eliminar
└── POST /api/subscriptions/sync → Sincronizar cambios offline
```

**Base de datos**: PostgreSQL 16 en Docker
**Token JWT**: Expira en 24 horas
**CORS**: Configurado para localhost

### Frontend MAUI (.NET 10.0-android)

#### Servicios Implementados
| Servicio | Responsabilidad |
|----------|-----------------|
| **ApiService** | Comunicación HTTP con backend |
| **TokenService** | Gestión segura de JWT |
| **LocalStorageService** | Persistencia JSON local |
| **SyncService** | Sincronización offline/online |

#### Páginas Implementadas
| Página | Funcionalidad |
|--------|---------------|
| **LoginPage** | Autenticación con email/password |
| **RegisterPage** | Registro de nuevos usuarios |
| **MainPage** | Dashboard con lista de suscripciones |
| **AddSubscriptionPage** | Formulario para crear/editar suscripción |

#### Características
✅ Detección automática de conectividad
✅ Cola de cambios pendientes (offline storage)
✅ Sincronización automática al conectar
✅ Búsqueda dinámmica de suscripciones
✅ Filtrado por ciclo de facturación (Mensual/Quincenal)
✅ Cálculo de totales mensuales/quincenales
✅ Descarga/carga de contraseñas

---

## 📁 Estructura de Archivos

### Backend
```
Subscription_tracker.API/
├── Models/
│   ├── User.cs
│   ├── Subscription.cs
│   └── SyncChange.cs (metadata para sincronización)
├── Controllers/
│   ├── AuthController.cs (register, login)
│   └── SubscriptionsController.cs (CRUD + sync)
├── Services/
│   ├── JwtTokenService.cs (token generation/validation)
│   ├── SubscriptionService.cs
│   └── SyncService.cs
├── Data/
│   ├── AppDbContext.cs (EF Core + PostgreSQL)
│   └── Migrations/ (auto-applied)
├── Program.cs (configuración)
├── appsettings.json
└── Dockerfile
```

### Frontend
```
Subscription_tracker/ (MAUI)
├── Models/SharedModels.cs
├── Services/
│   ├── ApiService.cs
│   ├── TokenService.cs
│   ├── LocalStorageService.cs
│   └── SyncService.cs
├── Pages/
│   ├── LoginPage.xaml[.cs]
│   ├── RegisterPage.xaml[.cs]
│   ├── AddSubscriptionPage.xaml[.cs]
│   └── MainPage.xaml[.cs]
├── MauiProgram.cs (DI configuration)
├── AppShell.xaml (navigation)
└── Subscription_tracker.csproj (target: net10.0-android)
```

---

## 🚀 Cómo Ejecutar

### 1. Levantar Backend (Docker)
```bash
cd Subscription_tracker
docker compose up -d

# Verificar que está corriendo:
docker compose logs api  # Ver logs del API
docker compose logs db   # Ver logs de PostgreSQL
```

### 2. Conectar MAUI al API
El ApiService automaticamente busca: `http://localhost:5000`

### 3. Flujo de Uso Típico
```
1. Usuario abre app → LoginPage
2. Sin cuenta → Navega a RegisterPage
3. Ingresa email & password → POST /api/auth/register
4. API devuelve token JWT → Se guarda en SecureStorage
5. Navega a MainPage → GetSubscriptionsAsync()
6. Si online: obtiene de API
   Si offline: obtiene del almacenamiento local
7. Usuario puede crear/editar/eliminar suscripciones
8. Los cambios se sincronizan automáticamente
9. Si estaba offline, se sincroniza al conectar
```

---

## 🔐 Seguridad Implementada

- ✅ Hash SHA-256 de contraseñas en backend
- ✅ JWT con validación de issuer/audience/lifetime
- ✅ Token storage en SecureStorage (MAUI)
- ✅ Protección de endpoints sensibles con [Authorize]
- ✅ CORS configurado
- ✅ Validación de input en ambos lados

---

## 📊 Datos de Ejemplo

### Crear suscripción
```json
{
  "serviceName": "Netflix",
  "category": "Entertainment",
  "amount": 15.99,
  "billingCycle": "Monthly",
  "nextBillingDate": "2026-05-01",
  "paymentMethod": "Credit Card",
  "notes": "4K plan"
}
```

### Respuesta de Login
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "email": "user@example.com"
  }
}
```

---

## ⚙️ Configuración de Desarrollo

### PostgreSQL (Docker)
- **Host**: localhost:5432
- **User**: admin
- **Password**: subscription_tracker_dev
- **Database**: subscription_tracker

### API (.NET 10.0)
- **Port**: 5000 (mapeado desde 8080 en Docker)
- **Environment**: Development
- **Swagger**: http://localhost:5000/swagger

### MAUI (Android)
- **Target Framework**: net10.0-android
- **Min Android**: 21.0
- **HTTP**: Permitido en localhost (desarrollo)

---

## ✨ MVP Features

- ✅ Autenticación JWT
- ✅ CRUD de suscripciones
- ✅ Búsqueda y filtrado
- ✅ Offline support con sincronización
- ✅ Resumen de gastos (mensual/quincenal)
- ✅ Próximos cobros ordenados
- ✅ Persistencia local (JSON)
- ✅ UI responsivo mobile-first

---

## 🔄 Flujo de Sincronización Offline/Online

```
┌─────────────────────────────────────────────────────┐
│  Usuario intenta crear suscripción                  │
└─────────────────────────────────────────────────────┘
                        │
                        ▼
        ┌───────────────────────────────┐
        │ ¿Hay conexión Internet?       │
        └───────────────────────────────┘
            │                   │
      SÍ ◄──┴──► NO             │
        │                       │
        ▼                       ▼
    POST a API         Guardar local +
    Guardar local      Agregar a cola
    Actualizar UI      Actualizar UI
                            │
                            ▼
                  ┌──────────────────────┐
                  │ ¿Conexión vuelve?    │
                  └──────────────────────┘
                            │
                      SÍ ◄──┘
                            │
                            ▼
                    Enviar cola a API
                    Limpiar cola local
                    Refrescar datos
```

---

## 📝 Archivos Clave Modificados

- ✅ `docker-compose.yml` - Orquestación
- ✅ `Subscription_tracker.API/Program.cs` - Config backend
- ✅ `MauiProgram.cs` - DI y servicios
- ✅ `AppShell.xaml` - Rutas de navegación
- ✅ `Pages/*.xaml[.cs]` - UI lógica
- ✅ `Services/*.cs` - Toda lógica de datos
- ✅ `Models/SharedModels.cs` - Entidades compartidas

---

## 🧪 Testing Checklist

### Backend
- [ ] `POST /api/auth/register` con datos válidos
- [ ] `POST /api/auth/login` con credenciales válidas
- [ ] `POST /api/auth/login` con credenciales inválidas → 401
- [ ] `GET /api/subscriptions` sin token → 401
- [ ] `GET /api/subscriptions` con token válido → 200 + lista
- [ ] `POST /api/subscriptions` crear nueva
- [ ] `PUT /api/subscriptions/{id}` actualizar
- [ ] `DELETE /api/subscriptions/{id}` eliminar

### Frontend
- [ ] Registro de usuario nuevo → navegación a MainPage
- [ ] Login con email/password → token guardado
- [ ] MainPage carga suscripciones del API
- [ ] Crear suscripción → guarda y aparece en lista
- [ ] Búsqueda filtra correctamente
- [ ] Modo offline → datos del local storage
- [ ] Reconexión → sincroniza cambios pendientes

---

## 🎓 Aprendizajes clave

1. **Syncronización offline**: Es más complejo de lo que parece. Necesita manejo de conflictos
2. **JWT en mobile**: Usar SecureStorage para tokens, no Preferences
3. **HttpClient en MAUI**: Necesita handler especial para Android
4. **Docker + .NET**: El compose puede automatizar migraciones con `context.Database.Migrate()`
5. **MAUI + async**: Todo debe ser async, incluido UI updates con `MainThread.BeginInvokeOnMainThread()`

---

## 📈 Próximas mejoras (fuera de MVP)

- Push notifications cuando se aproxime un cobro
- Exportar suscripciones a PDF/CSV
- Integración con APIs bancarias
- Tema oscuro
- Multi-device sync (cloud)
- Análisis de gastos (gráficos)
- Alertas de suscripciones duplicadas

---

**Generated**: 2026-04-01
**Framework**: .NET 10.0 (MAUI + ASP.NET Core)
**Database**: PostgreSQL 16
**Status**: ✅ MVP Complete
