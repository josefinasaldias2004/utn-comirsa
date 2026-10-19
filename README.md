# Conecta · UTN FRSN + COMIRSA

Tienda comunitaria con **Angular 20**, **ASP.NET Core 10 (C#)** y **SQL Server 2022**.
La interfaz permite solicitar una cuenta, ingresar, buscar productos y servicios y crear, editar o eliminar publicaciones propias. El administrador verifica la pertenencia institucional y aprueba solicitudes.

## Estructura

- `frontend/`: Angular standalone, formularios y cliente HTTP.
- `backend/Conecta.Api/`: API REST, servicios de negocio, autenticación y acceso a datos con Entity Framework Core.
- `backend/Conecta.Api.Tests/`: pruebas de reglas de negocio, permisos, login y CSRF.
- `database/001_initial.sql`: esquema SQL inicial de usuarios y publicaciones.
- `compose.yaml`: entorno local con SQL Server, API y Nginx.
- `.github/workflows/ci.yml`: compilación de Angular y pruebas/compilación de C#.

La versión anterior de React/Sites está conservada en el historial Git (commit 80fbdf3). Esta versión reemplaza ese runtime y ya no depende del login de ChatGPT ni de D1.

## Inicio con Docker Desktop

Requiere Docker Desktop con contenedores Linux.

1. Copiar `.env.example` a `.env` y completar `SQL_PASSWORD` con una contraseña fuerte (no subir ese archivo).
2. Ejecutar `docker compose up --build -d`.
3. Abrir **http://localhost:8080**.
4. Crear el primer administrador con variables temporales (PowerShell):

```powershell
$env:Bootstrap__Email = "administracion@tu-institucion.edu.ar"
$env:Bootstrap__Organization = "UTN FRSN"
$secret = Read-Host "Contraseña del administrador (mínimo 12 caracteres)" -AsSecureString
$env:Bootstrap__Password = [System.Net.NetworkCredential]::new("", $secret).Password
docker compose exec -e Bootstrap__Email -e Bootstrap__Organization -e Bootstrap__Password api dotnet Conecta.Api.dll --seed-admin
Remove-Item Env:Bootstrap__Password
```

El comando rechaza crear otro administrador si ya existe uno; nunca promueve al primer visitante ni contiene contraseñas predeterminadas.

El servicio `init` crea las tablas de una base vacía usando EF Core. El SQL de `database/` es una alternativa para DBA, **no debe ejecutarse después de inicializar con EF**. No se modifica ni borra una base existente. Para futuras versiones, añadir migraciones SQL revisadas antes de desplegar; `EnsureCreated` no es un sistema de migraciones incrementales.

## Desarrollo sin Docker para la aplicación

Requiere Node.js 24, SDK .NET 10 y una instancia SQL Server accesible.
La base SQL puede iniciarse con `docker compose up -d sql`.

```powershell
# Terminal del backend
$env:ConnectionStrings__Default = "Server=localhost,1433;Database=Conecta;User Id=sa;Password=TU_CLAVE;Encrypt=True;TrustServerCertificate=True"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project backend/Conecta.Api -- --init-db
# Configurar Bootstrap__Email, Bootstrap__Password y Bootstrap__Organization como arriba:
dotnet run --project backend/Conecta.Api -- --seed-admin
dotnet run --project backend/Conecta.Api --urls http://localhost:5080

# Otra terminal, desde la raíz del proyecto
npm --prefix frontend ci
npm --prefix frontend start
```

Abrir **http://localhost:4200**. Angular usa un proxy a `localhost:5080`; las llamadas son relativas a `/api`, sin CORS permisivo.

## Pruebas y compilación

```powershell
dotnet test backend/Conecta.Api.Tests -c Release
dotnet publish backend/Conecta.Api -c Release -o publish
npm --prefix frontend ci
npm --prefix frontend run build
```

Seis pruebas usan SQLite en memoria para aislar servicios y autenticación. La prueba de integración SQL Server se ejecuta si existe SQL_TEST_CONNECTION y crea/elimina una base de prueba aislada; GitHub Actions la configura automáticamente. Consultar `VERIFICACION.md` para el resultado de la ejecución en este entorno.

## Datos y reglas

- Registro: nombre, email, contraseña y comunidad UTN FRSN o COMIRSA.
- Acceso pendiente hasta aprobación manual tras verificar afiliación; seleccionar la institución no prueba pertenencia.
- Contraseñas con el hasher de ASP.NET Core; cookies HttpOnly, SameSite, caducidad de 8 horas, CSRF en todas las mutaciones y límite de intentos.
- La organización de un aviso se obtiene del propietario autenticado, nunca del formulario.
- Alta, edición y baja autorizadas en el servidor; un miembro no puede modificar avisos ajenos.
- Precio decimal en ARS; vacío significa “a convenir”.
- Imágenes JPG/PNG/WebP hasta 1 MB, almacenadas en SQL como `varbinary(max)`. Se sirven únicamente a usuarios autenticados; se valida tipo y firma inicial.
- Ventas por contacto directo: no hay cobros integrados, carrito ni pasarela de pagos.
- No hay envío de emails, recuperación automática de contraseña ni validación automática de afiliación en esta versión.

## API

| Método y ruta | Función |
|---|---|
| GET /api/auth/csrf | Obtener token CSRF y cookies |
| POST /api/auth/register | Solicitar cuenta |
| POST /api/auth/login | Iniciar sesión |
| POST /api/auth/logout | Cerrar sesión |
| GET /api/auth/me | Perfil autenticado |
| GET /api/members/pending | Solicitudes, solo administrador |
| POST /api/members/{id}/approve | Aprobar miembro, solo administrador |
| GET /api/listings | Buscar con q, type, org, category y mine |
| POST /api/listings | Crear publicación |
| PUT /api/listings/{id} | Editar publicación propia |
| DELETE /api/listings/{id} | Eliminar publicación propia |
| GET /api/listings/{id}/image | Imagen del aviso |

## Alojamiento

Este backend requiere un servidor compatible con .NET y SQL Server; **no se puede desplegar como GitHub Pages ni en el runtime Cloudflare Workers de Sites**. GitHub aloja el código y puede ejecutar CI, pero eso no publica la aplicación.

El Compose incluido está limitado a localhost y usa modo Development para HTTP local. Para producción: configurar HTTPS en el servidor/proxy, ASPNETCORE_ENVIRONMENT=Production, certificados SQL válidos (TrustServerCertificate=False), usuario SQL con permisos mínimos, almacenamiento persistente de claves Data Protection y copias de seguridad. Si un proxy termina TLS, configurar explícitamente proxies de confianza y cabeceras reenviadas antes de autenticación. No exponer el entorno de desarrollo a Internet.

La base anterior D1 no se conecta ni se convierte automáticamente; no se verificó que contuviera datos de usuarios.
