# Subscription Tracker MVP (PWA + .NET API + PostgreSQL)

Este proyecto fue migrado de ToDo a Subscription Tracker con soporte offline.

## Requisitos funcionales cubiertos
- Registro y login con JWT
- CRUD de suscripciones
- Vista de costos mensual y anual equivalente
- Busqueda y filtros por categoria/ciclo
- Modo offline con cola de cambios y sincronizacion automatica al reconectar
- Seguimiento de proximos cobros
- Docker para levantar la base de datos PostgreSQL

## Estructura principal
- `backend/TodoApi`: backend .NET 8 API
- `frontend`: frontend PWA (HTML/CSS/JS + IndexedDB + Service Worker)

## Proceso de migracion (documentado)
1. **Backend**
   - Se reemplazo el dominio de `tasks` por `users` + `subscriptions`.
   - Se agregaron endpoints de autenticacion JWT:
     - `POST /auth/register`
     - `POST /auth/login`
   - Se agregaron endpoints protegidos para suscripciones y resumen:
     - `GET/POST/PUT/DELETE /subscriptions`
     - `GET /subscriptions/upcoming`
     - `GET /subscriptions/summary`
   - Se configuro Swagger con esquema Bearer.
2. **Base de datos**
   - Se actualizaron scripts SQL para crear tablas e indices de:
     - `users`
     - `subscriptions`
   - Se configuro `docker-compose.yml` para PostgreSQL en `subscription_tracker_db`.
3. **PWA**
   - Se rediseño la UI para incluir:
     - login/register
     - formulario de suscripcion
     - filtros y busqueda
     - resumen de costos y proximos cobros
   - Se implemento almacenamiento local con IndexedDB:
     - store de suscripciones
     - store de operaciones pendientes (`create/update/delete`)
   - Se implemento sync manual y automatico al evento `online`.
   - Se actualizo Service Worker y manifest de la app.

## Levantar el sistema
1. Iniciar base de datos:
   - `cd backend/TodoApi`
   - `docker compose up -d`
2. Ejecutar API:
   - `dotnet restore`
   - `dotnet run`
3. Ejecutar PWA en servidor estatico (ejemplo):
   - `cd ../../frontend`
   - `python3 -m http.server 8080`
   - abrir `http://localhost:8080`

## Nota de seguridad
- Cambia `Jwt:Key` en `TodoApiSolution/TodoApi/appsettings.json` por un secreto fuerte antes de cualquier despliegue.
- Cambia `Jwt:Key` en `backend/TodoApi/appsettings.json` por un secreto fuerte antes de cualquier despliegue.
