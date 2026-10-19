# Verificación de la migración — 19/10/2026

- SDK .NET 10.0.402 descargado del sitio oficial de Microsoft a una carpeta temporal del workspace y verificado con SHA-512. No se instaló globalmente.
- Backend ASP.NET Core: compilación Release correcta.
- `dotnet publish`: correcto.
- Pruebas C#: seis correctas; una prueba de integración SQL Server omitida explícitamente porque no hay SQL Server ni Docker instalado en este equipo.
- Angular: `ngc -p tsconfig.app.json` correcto (TypeScript y plantillas con comprobación estricta).
- Angular: `ng build` bloqueado por `spawn EPERM` al iniciar procesos de compilación del entorno. No se confirmó el bundle de producción.
- No se ejecutó una prueba visual en navegador de la aplicación migrada ni el entorno Docker completo.
- GitHub Actions incluye build Angular, test/publish .NET y SQL Server temporal para la prueba de integración pendiente.

Para poder comprobar C# se restauraron paquetes oficiales de NuGet desde una carpeta local descargada por HTTPS, porque el cliente TLS nativo del entorno no pudo conectarse al registro. La auditoría en línea de NuGet no estuvo disponible en esa restauración local; no se deshabilitó en la configuración del proyecto ni en CI.
