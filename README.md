# Conecta — UTN FRSN + COMIRSA

Implementación de tienda comunitaria en React/Vinext con base de datos D1 y autenticación de Sites (cuenta ChatGPT).

## Funciones implementadas
- Acceso mediante el login de Sites.
- Catálogo compartido, búsqueda y filtros por comunidad, tipo y categoría.
- Publicación de productos y servicios con foto de hasta 1 MB, precio en ARS y email de contacto.
- Listado y eliminación de publicaciones propias; autorización comprobada en el servidor.
- Contacto directo por email; no incluye cobros o pagos integrados.

## Estado de verificación
TypeScript pasó sin errores. La migración SQLite se generó con la API oficial de Drizzle.
No se logró compilar, iniciar el servidor ni publicar: el entorno Windows bloquea procesos hijos con EPERM. El helper de preparación de Sites también falló. No hay despliegue operativo verificado.
La vista previa HTML es estática y solo muestra el diseño.

## Acceso institucional
El sitio registrado permanece privado para el propietario. Antes de habilitarlo a la comunidad deben configurarse los usuarios autorizados de UTN FRSN y COMIRSA en Sites.
Elegir una comunidad en el formulario describe la publicación, no comprueba afiliación institucional.
No se enviaron invitaciones.

## Continuación
Se conserva la identidad del sitio en .openai/hosting.json; no crear otro sitio.
Instalar dependencias con scripts/install-ci.mjs a través de npm run install:ci en un entorno que permita los procesos necesarios.
Ejecutar npx tsc --noEmit y npm run build.
Continuar la publicación mediante el workflow de Sites, con credencial nueva, push de código y despliegue privado.
Aplicar las migraciones de drizzle/ en el entorno local para pruebas; Sites las aplica al desplegar.
Probar login, alta de publicaciones, persistencia entre sesiones, búsqueda y prohibición de eliminar avisos ajenos antes de invitar usuarios.

Los archivos de imagen de esta primera implementación se almacenan como datos en la publicación (límite 1 MB). Para mayor volumen, migrarlos a R2.
