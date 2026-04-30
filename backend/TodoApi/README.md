# Subscription Tracker API (.NET 8 + PostgreSQL + Dapper + JWT)

Backend del MVP de gestion de suscripciones para la PWA.

## Funcionalidades
- Registro y login con JWT (`/auth/register`, `/auth/login`)
- CRUD de suscripciones por usuario autenticado
- Compartir suscripciones con otros usuarios registrados por email
- Filtros por busqueda, categoria y ciclo de facturacion
- Endpoint de proximos cobros
- Endpoint de resumen con costo mensual y anual equivalente

## Stack
- ASP.NET Core Web API (.NET 8)
- PostgreSQL
- Dapper
- JWT Bearer Authentication
- Swagger con soporte para Bearer Token

## Endpoints

Publicos:
- `POST /auth/register`
- `POST /auth/login`

Protegidos (Bearer token):
- `GET /subscriptions`
- `GET /subscriptions/{id}`
- `POST /subscriptions`
- `PUT /subscriptions/{id}`
- `DELETE /subscriptions/{id}`
- `POST /subscriptions/{id}/share`
- `DELETE /subscriptions/{id}/share?email=usuario@correo.com`
- `GET /subscriptions/upcoming?days=30`
- `GET /subscriptions/summary`

## Configuracion
Archivo `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=subscription_tracker_db;Username=postgres;Password=postgres"
},
"Jwt": {
  "Key": "CHANGE_ME_SUPER_SECRET_KEY_AT_LEAST_32_CHARS",
  "Issuer": "SubscriptionTrackerApi",
  "Audience": "SubscriptionTrackerPwa",
  "ExpiresMinutes": 120
}
```

Importante:
- Cambia `Jwt:Key` por una clave fuerte real antes de produccion.

## Docker (base de datos)

Desde la carpeta `TodoApi`:

```bash
docker compose up -d
```

Esto inicia PostgreSQL en `localhost:5432` con:
- base `subscription_tracker_db`
- usuario `postgres`
- password `postgres`
- scripts automáticos:
  - `Scripts/02_create_table_tasks.sql` (schema)
  - `Scripts/03_seed_tasks.sql` (seed)

Para detener:

```bash
docker compose down
```

Para limpiar tambien el volumen:

```bash
docker compose down -v
```

## Ejecutar API

```bash
dotnet restore
dotnet run
```

API:
- `http://localhost:5000`

Swagger:
- `http://localhost:5000/swagger`

## Flujo rapido de uso
1. Registrar usuario con `POST /auth/register`
2. Copiar token JWT del response
3. En Swagger, usar `Authorize` con `Bearer <token>`
4. Crear suscripciones desde `POST /subscriptions`
5. Consultar resumen con `GET /subscriptions/summary`
6. Consultar proximos cobros con `GET /subscriptions/upcoming?days=30`
