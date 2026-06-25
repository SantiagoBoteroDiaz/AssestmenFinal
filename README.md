# Rentas Cortas — Plataforma de reservas con KYC asistido por IA

Plataforma de rentas cortas que permite explorar alojamientos sin registro, valida la identidad de los huéspedes mediante IA antes de su primera reserva, y entrega a los propietarios herramientas de gestión, métricas y exportación de datos sobre sus inmuebles.

## Tabla de contenidos

- [Requisitos previos](#requisitos-previos)
- [Cómo levantar el proyecto con Docker](#cómo-levantar-el-proyecto-con-docker)
- [Arquitectura](#arquitectura)
- [Decisiones técnicas y cómo se abordaron los problemas](#decisiones-técnicas-y-cómo-se-abordaron-los-problemas)
- [Estructura de carpetas](#estructura-de-carpetas)
- [Flujo de roles](#flujo-de-roles)
- [Pendientes y mejoras futuras](#pendientes-y-mejoras-futuras)

---

## Requisitos previos

- [Docker](https://docs.docker.com/get-docker/) y Docker Compose (v2+)

No es necesario tener .NET SDK ni Postgres instalados localmente — todo corre dentro de los contenedores. La API key de Gemini y las credenciales SMTP ya están configuradas en `appsettings.Development.json` para fines de esta entrega; en un entorno real estos valores nunca se versionarían en el repositorio (ver nota de seguridad más abajo).

## Cómo levantar el proyecto con Docker

1. Clona el repositorio:

   ```bash
   git clone https://github.com/SantiagoBoteroDiaz/AssestmenFinal
   cd assestment
   ```

2. Levanta todo el entorno con un solo comando:

   ```bash
   docker compose up -d --build
   ```

   Esto hace lo siguiente, en orden:
   - Construye la imagen de la aplicación .NET a partir del `Dockerfile`.
   - Levanta un contenedor de Postgres 18 y ejecuta automáticamente `init_db.sql` la primera vez que el volumen de datos está vacío. Este script crea todas las tablas, constraints, índices, y carga datos de prueba (usuarios, inmuebles y reservas de ejemplo) para que la aplicación sea explorable de inmediato.
   - Espera a que Postgres confirme estar saludable (`healthcheck`) antes de levantar la aplicación.

3. La aplicación queda disponible en `http://localhost:5143`.

4. Para detener todo:

   ```bash
   docker compose down
   ```

   Para detener y borrar también los datos de la base de datos (reinicio completo, sin los datos de prueba):

   ```bash
   docker compose down -v
   ```

### Nota sobre el enfoque DB-first

Este proyecto usa Entity Framework Core en modalidad **DB-first**: las clases C# de las entidades (`Models/`) se generaron a partir del esquema real de Postgres mediante scaffolding (`dotnet ef dbcontext scaffold`), no al revés. El archivo `init_db.sql` —generado con `pg_dump --schema-only` (o con datos incluidos) a partir del entorno de desarrollo— es la fuente de verdad del esquema. Docker no ejecuta ningún scaffold en tiempo de build: solo compila el código C# ya generado y lo conecta a una base de datos que cumple esa misma estructura.

Si en algún momento se modifica el esquema directamente en la base de datos, el scaffold debe volver a correrse manualmente en el entorno de desarrollo para regenerar las entidades:

```bash
dotnet ef dbcontext scaffold \
  "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=<tu_password>" \
  Npgsql.EntityFrameworkCore.PostgreSQL \
  --output-dir Models \
  --context-dir Data \
  --context AppDbContext \
  --no-onconfiguring \
  --force
```

Y el `init_db.sql` debe regenerarse a partir del esquema actualizado:

```bash
pg_dump -h localhost -U postgres -d postgres --no-owner --no-privileges > init_db.sql
```

(Si el `pg_dump` instalado localmente es de una versión distinta a la del servidor de Postgres, instala el cliente correspondiente, por ejemplo `sudo apt install postgresql-client-18`, o ejecuta el dump desde un contenedor temporal con la imagen oficial de la versión correcta.)

### Nota sobre seguridad de credenciales

Para simplificar la entrega de esta prueba, la API key de Gemini y las credenciales SMTP viven directamente en `appsettings.Development.json`, el cual queda incluido en la imagen Docker. **Esto no es una práctica recomendada para producción** — en un entorno real, estos valores se inyectarían mediante variables de entorno gestionadas por un secret manager (Azure Key Vault, AWS Secrets Manager, variables de entorno del orquestador), y `appsettings.Development.json` se excluiría del control de versiones.

## Arquitectura

El proyecto sigue una organización por capas dentro de un único proyecto ASP.NET Core MVC (sin separar en múltiples `.csproj`, dado el alcance de la prueba):

```
Controllers/   → reciben requests HTTP, delegan al service correspondiente, devuelven vistas o redirects
Services/      → lógica de negocio: validaciones, cálculos, orquestación entre repositorios y servicios externos
Interfaces/    → contratos de cada service, para inyección de dependencias y desacoplar la implementación
Models/        → entidades generadas por scaffold a partir de la base de datos (DB-first)
Dto/           → objetos de transferencia usados en el binding de formularios y respuestas de servicio
ViewModels/    → modelos compuestos específicos para una vista, cuando un DTO simple no es suficiente
Data/          → AppDbContext (EF Core)
Views/         → vistas Razor (.cshtml), organizadas por controller
```

**Por qué MVC clásico y no Blazor o una SPA**: el enunciado especifica vistas servidas con `Controller` + `View()`. Esto también simplificó el manejo de autenticación, que se resuelve con cookies (ver más abajo) en lugar de tokens manejados desde JavaScript.

**Patrón de respuesta uniforme (`SystemResponse<T>`)**: todos los métodos de servicio devuelven un wrapper con `Success`, `Message` y `Data`, en lugar de lanzar excepciones de negocio o devolver `null` ambiguamente. Esto permite que los controllers manejen errores de forma consistente sin necesidad de `try/catch` repetido en cada acción, y que cada error de negocio tenga un mensaje claro para mostrar al usuario.

## Decisiones técnicas y cómo se abordaron los problemas

### Validación de identidad (KYC) con IA

- El usuario sube una foto de su cédula desde un `<input type="file" accept="image/*" capture="environment">`, lo cual abre directamente la cámara trasera en dispositivos móviles.
- La imagen se procesa **en memoria** y se envía directamente a la API de Google Gemini (modelo `gemini-2.5-flash`) usando el SDK oficial `Google.GenAI`. **La imagen nunca se almacena en disco ni en la base de datos** — se descarta inmediatamente después de extraer los datos, lo que satisface el requerimiento de "eliminación segura de los documentos de identidad" de la forma más simple posible: no hay nada que eliminar después, porque nunca se persistió.
- Los datos extraídos (nombres, apellidos, número de documento, fecha de nacimiento) se comparan contra los datos que el usuario ingresó al registrarse, usando comparación normalizada (sin tildes, mayúsculas, espacios) para tolerar pequeñas variaciones de OCR sin perder rigor en campos críticos como el número de documento.
- El veredicto (aprobado/rechazado) y un registro de auditoría (sin la imagen) se persisten en `kyc_verifications`, con una relación 1 a 1 con el usuario — el KYC se realiza una sola vez por usuario, no por reserva, conforme al enunciado ("antes de la *primera* reserva").
- Un campo desnormalizado `usuarios.kyc_aprobado` evita tener que hacer join cada vez que se valida si un usuario puede reservar.

### Prevención de double-booking

La integridad contra reservas solapadas se garantiza en **dos capas**, no solo una:

1. Una validación en `ReservationService` antes de insertar, que devuelve un mensaje de error claro si las fechas ya están ocupadas.
2. Una **constraint de exclusión a nivel de base de datos** (`EXCLUDE USING gist`, sobre `daterange`), que rechaza físicamente cualquier inserción solapada incluso si dos requests llegan exactamente al mismo tiempo (race condition). La validación en C# por sí sola no es suficiente para esto, porque siempre existe una ventana de tiempo entre la verificación y la inserción donde dos transacciones concurrentes podrían colarse.

### Autenticación diferida

Se implementó con **cookies de autenticación** (`Microsoft.AspNetCore.Authentication.Cookies`), no con JWT. Para un MVC clásico con vistas Razor, las cookies son el mecanismo idiomático: el navegador las gestiona automáticamente en cada request sin necesidad de JavaScript adicional, a diferencia de JWT, que requeriría guardar el token manualmente y adjuntarlo en cada petición. El catálogo de inmuebles (`PropertyController.Index`) no tiene ningún atributo `[Authorize]`, permitiendo exploración completamente anónima; el login solo se solicita al intentar reservar, guardar un favorito permanente, o acceder a las áreas de gestión.

### Roles y autorización

El sistema define dos roles (`Huesped`, `Propietario`), almacenados como `string` en la base de datos (no como tipo `ENUM` nativo de Postgres, para mantener flexibilidad en un escenario DB-first) con un `CHECK constraint` que valida los valores permitidos. Cada acción protegida verifica el rol mediante `[Authorize(Roles = "...")]`, y las operaciones sobre recursos existentes (editar inmueble, cancelar reserva, exportar reportes) verifican adicionalmente que el usuario autenticado sea el propietario real del recurso — no solo que tenga el rol correcto — para prevenir referencias directas a objetos inseguras (IDOR).

### Notificaciones por correo

Centralizadas en `IEmailService`, usando MailKit sobre SMTP. El envío de correo **nunca bloquea ni revierte** la operación principal: si el SMTP falla, el registro, la reserva o la cancelación se completan de todas formas, y el fallo de envío se absorbe silenciosamente. Se consideró preferible perder una notificación a bloquear una funcionalidad crítica del negocio por una dependencia externa caída.

### Reportes en Excel

Generados en el servidor con `ClosedXML`, a partir de las reservas de los inmuebles del propietario autenticado. El reporte puede filtrarse por inmueble específico o generarse para el portafolio completo, e incluye fechas de alquiler, precio pagado, y datos básicos del huésped, conforme al enunciado.

### Panel de rendimiento (Dashboard)

Calcula, sobre un rango de fechas seleccionable, los ingresos totales, la tasa de ocupación (noches reservadas frente a noches disponibles en el periodo) y el número de reservas, tanto a nivel de portafolio como desglosado por inmueble. La "rentabilidad" se interpreta como ingresos por inmueble en el periodo, dado que el modelo de datos no contempla una tabla de gastos/costos operativos — de existir, el cálculo se ajustaría a ingresos menos gastos.

## Estructura de carpetas

```
assestment/
├── Controllers/
├── Services/
├── Interfaces/
├── Models/              (generado por scaffold — no editar a mano)
├── Dto/
├── ViewModels/
├── Data/
├── Views/
├── wwwroot/
├── init_db.sql          (esquema y datos de prueba, ejecutado automáticamente por el contenedor de Postgres)
├── docker-compose.yml
├── Dockerfile
└── Program.cs
```

## Flujo de roles

**Huésped**: explora el catálogo sin cuenta → marca favoritos o intenta reservar → se le solicita login/registro → verifica su identidad (solo antes de su primera reserva) → completa la reserva, con horarios de check-in/check-out estandarizados automáticamente.

**Propietario**: publica inmuebles con tarifas y fotos (vía URL externa) → gestiona su inventario (editar, activar/desactivar) → consulta su panel de rendimiento con métricas de ingresos y ocupación por periodo → exporta reportes en Excel para su contabilidad.

## Pendientes y mejoras futuras

- Las fotos de los inmuebles se almacenan como **URL externa**, no como archivo subido al servidor — decisión tomada para mantener el alcance de la prueba acotado. En producción se evaluaría subir a un bucket de almacenamiento (S3, Azure Blob Storage) en lugar de depender de que el propietario aloje la imagen en otro servicio.
- No se implementó un flujo de pagos real; las reservas se confirman automáticamente tras pasar las validaciones de disponibilidad e identidad.
- Las notificaciones in-app (mencionadas en el enunciado junto con las de correo) quedan como trabajo futuro; actualmente está implementado el canal de correo electrónico, cubriendo bienvenida al registrarse, confirmación de reserva, cancelación de reserva, y aviso a huéspedes afectados cuando un propietario desactiva un inmueble con reservas activas.
- Las credenciales de servicios externos (Gemini, SMTP) deberían migrarse a un secret manager antes de cualquier despliegue real, en lugar de vivir en `appsettings.Development.json`.