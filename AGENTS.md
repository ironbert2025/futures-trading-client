# AGENTS.md — futures-trading-client

## Qué es este proyecto

Cliente de escritorio Windows (C#) para trading de futuros. Es el reemplazo del proyecto legacy `Setup_Client` (WinForms, auditado y con deuda técnica severa — ver "Lecciones del proyecto anterior"). Responsabilidades:

1. **UI de trading**: visualización de setups, gráficos, alertas, gestión de la operativa diaria.
2. **Conexión directa a plataformas externas**: NinjaTrader (vía su SDK/DLL nativo) y ThinkorSwim (reemplazo de TradeStation) para datos de mercado en tiempo real y ejecución de órdenes.
3. **Cálculo de negocio del lado del cliente**: riesgo/recompensa, tick values, lógica de setups — mientras no se decida mover esto a un servicio separado.
4. **Consumo de `futures-trading-backend`** (API Python) para dos cosas puntuales: autenticación de usuario y persistencia de trades ejecutados.

## Flujo de desarrollo: iterar directamente (SDD en pausa)

**Decisión del usuario (2026-10-13): SDD queda a un lado.** Se itera y se desarrolla directamente: no hace falta escribir una spec ni pedir su aprobación antes de cambiar código, y no se agregan más tests unitarios. Lo que ya existe se conserva como referencia (no borrar):

- `specs/` (índice en [`specs/README.md`](specs/README.md)) con `0001-auth-login` y `0002-home-startup`. **El contrato HTTP lo posee el backend** (`futures-trading-backend/specs/...`). No se mantienen al día salvo que el usuario lo pida; **si el código cambia, pueden quedar desactualizadas**.
- Los tests existentes (unit, integración, E2E) se conservan y siguen corriendo con `dotnet test`. Tests nuevos, solo si el usuario los pide.
- Estado de `0001-auth-login` (al 2026-10-13) es retroactiva y tiene 17 tests (2 unit, 7 integración, 8 E2E) con los 9 AC cubiertos, pero está **en pausa** desde 2026-10-13: la app arranca en `HomeForm` (spec `0002-home-startup`) y los 8 E2E de ventana están omitidos (`Skip = PausedReasons.Login`). Reactivarlos es T6 de `0002`. Límite documentado: el timeout (AC-7) solo se prueba en integración.

### Tests

```bash
dotnet test FuturesTrading.slnx                       # todo; los E2E requieren backend vivo y E2E_USERNAME/E2E_PASSWORD
dotnet test tests/FuturesTrading.Infrastructure.Tests # integración: sin red, sin backend
dotnet test tests/FuturesTrading.Application.Tests    # unit
```

- `Infrastructure.Tests` prueba `BackendApiClient` con un `HttpMessageHandler` falso: verifica lo que el cliente **envía** y cómo traduce cada respuesta. El handler captura la petición **dentro** de `SendAsync` (después `HttpClient` puede liberarla).
- Los tests existentes llevan su `AC-n` en el nombre (`Ac3_Unauthorized_...`). **No se agregan tests unitarios nuevos** (decisión del usuario, 2026-10-13); no borrar los que hay.

## Fuera de alcance para el backend Python (y por qué importa aquí)

`futures-trading-backend` **no** habla con NinjaTrader/ThinkorSwim, **no** calcula riesgo/recompensa. Esas responsabilidades son de este cliente. Esto es una decisión de arquitectura explícita (latencia, y porque NinjaTrader/ThinkorSwim se integran vía SDKs nativos de Windows) — no delegar estas llamadas al backend "para simplificar", y no esperar que el backend valide o calcule nada de trading.

## Stack

- **UI**: WinForms — decisión tomada sobre WPF por continuidad con la intuición ganada en el proyecto legacy y porque el SDK/ATI de NinjaTrader está pensado para Windows clásico. El desafío de separación UI/lógica que WPF resuelve con data-binding se resuelve aquí con disciplina de Clean Architecture (ver abajo), no con el framework de UI.
- **.NET**: .NET 8 (no repetir el .NET Framework legacy del proyecto anterior).
- **HTTP hacia el backend**: `HttpClient` con `async`/`await` — nunca `HttpWebRequest` síncrono como en el legacy.
- **DTOs del backend**: generados/alineados con el esquema OpenAPI que expone `futures-trading-backend` (fuente de verdad ahí, no redefinir el contrato manualmente en este repo).
- **Integraciones externas**: SDK/DLL de NinjaTrader (ATI), API de ThinkorSwim — cada una encapsulada en su propio servicio/cliente con interfaz (para poder testear/mockear).

## Arquitectura: Clean Architecture

Solución dividida en proyectos por capa, con la regla de dependencia estricta: **las capas internas no conocen a las externas**.

- **`Domain`**: entidades y reglas de negocio puras (Setup, Trade, Instrument, RiskRewardCalculator). Sin dependencias externas — nada de WinForms, nada de HTTP, nada de SDK de NinjaTrader. Es lo más parecido a "lo que no debe romperse nunca" del sistema.
- **`Application`**: casos de uso/orquestación (ej. "abrir setup", "registrar trade ejecutado", "autenticar usuario"). Define interfaces (`INinjaTraderService`, `IThinkOrSwimService`, `IBackendApiClient`, `ITradeRepository`) que las capas externas implementan — Application depende de abstracciones, no de implementaciones concretas.
- **`Infrastructure`**: implementaciones concretas de esas interfaces — el cliente HTTP real hacia `futures-trading-backend`, el wrapper real del SDK de NinjaTrader, el cliente real de ThinkorSwim.
- **`UI` (WinForms)**: la capa más externa. Los formularios son "tontos": reciben eventos del usuario, llaman a un caso de uso de `Application`, y muestran el resultado. Nada de lógica de negocio ni llamadas de red directas en el code-behind.

Esto es exactamente lo que faltó en el legacy: `Form1.cs`/`Form_Setup.cs` eran UI+Application+Infrastructure+Domain fusionados en una sola clase de miles de líneas.

## Convenciones

- **Separación de capas obligatoria desde el día uno** (ver Clean Architecture arriba) — ningún formulario contiene lógica de red o cálculo financiero en su code-behind.
- **Dominio fuertemente tipado**: precios como `decimal`, fechas como `DateTime`, flags como `bool`/`enum` — nunca `string` para representar estos valores (ver lecciones abajo).
- **Una sola fuente de verdad por cálculo de negocio** (risk/reward, tick value por instrumento) — vive en `Domain`, un único lugar, no una copia por formulario.
- **Nada de credenciales/API keys en el código fuente** — usar `appsettings.json` + `dotnet user-secrets` en desarrollo, y una solución de configuración segura en producción (no commitear secretos).
- **Todo `async` de extremo a extremo** para I/O (HTTP, brokers) — no bloquear el hilo de UI.
- **Inyección de dependencias** (`Microsoft.Extensions.DependencyInjection`) para resolver las interfaces de `Application` con las implementaciones de `Infrastructure` — sin `new NinjaTraderClient()` disperso por los formularios como en el legacy.

## Estrategia de testing

Los 3 tipos de test, incorporados a medida que el sistema crezca (no todos de una vez) — mapeados a las capas de Clean Architecture:

- **Unit tests** (`Domain.Tests`, `Application.Tests`): la prioridad más alta, porque cubren la lógica que estaba más rota en el legacy (risk/reward, tick values). `Domain` se testea sin mocks (es lógica pura). `Application` se testea mockeando las interfaces (`INinjaTraderService`, `IThinkOrSwimService`, `IBackendApiClient`, `ITradeRepository`) — nunca contra implementaciones reales. Framework: xUnit + (FluentAssertions/Moq o NSubstitute, a elegir cuando se escriba el primer test).
- **Integration tests** (`Infrastructure.Tests`): validan que las implementaciones reales de `Infrastructure` funcionan contra sus dependencias externas reales — el cliente HTTP contra una instancia real de `futures-trading-backend` (local o en Docker en Fase 2), y el wrapper de NinjaTrader/ThinkorSwim contra su SDK/API real cuando haya un entorno disponible para eso (si no lo hay en CI, usar test doubles ahí y reservar estas pruebas para ejecución manual/local).
- **E2E tests** (`E2E.Tests`): sobre la aplicación completa, incluida la UI WinForms, con **FlaUI** (o WinAppDriver como alternativa) para automatizar vía UI Automation de Windows. Es la capa más cara de mantener — reservarla para los flujos críticos de punta a punta (ej. login → crear setup → ver alerta), no para cubrir cada pantalla.

Regla general: no escribir tests de una capa que aún no existe. Cada vez que se agregue una entidad/caso de uso/servicio nuevo, su test correspondiente se agrega en el mismo cambio, no después.

## Lecciones del proyecto anterior (Setup_Client) — no repetir

Auditoría completa disponible; estas son restricciones activas, no solo historia:

- **God Forms**: `Form1.cs` llegó a 19,075 líneas y `Form_Setup.cs` a 12,892, mezclando UI + red + cálculo + archivos + integración con broker en una sola clase. Aquí: un formulario no debe superar unos pocos cientos de líneas; toda lógica no-UI va a un servicio.
- **Secretos hardcodeados**: API keys de OANDA, client_secret y refresh token de TradeStation quedaron en texto plano en el código, extraíbles por decompilación. Aquí: nunca.
- **Identidad hardcodeada**: varios formularios fijaban `Userid="aldorat"; Role="Admin"` como atajo, bypasseando la autenticación real. Aquí: siempre usar la sesión autenticada real contra el backend.
- **Login inseguro**: password enviado por query string en un GET, comparado en texto plano, y un botón que sobreescribía el campo de password con el literal `"robert"`. Aquí: el flujo de auth contra `futures-trading-backend` usa POST + HTTPS (aunque sea local) + hashing del lado del servidor.
- **Cálculo financiero duplicado y divergente**: la fórmula de riesgo/recompensa y las tablas de tick-value por instrumento se reimplementaron 6+ veces con valores ya inconsistentes entre copias. Aquí: un único `RiskRewardCalculator`/`InstrumentSpecs` compartido.
- **Contratos de futuros y cuentas de broker hardcodeados** (ej. `"CL 04-24"`, cuentas reales en texto plano) — se rompen cada vencimiento y exponen datos operativos. Aquí: configuración externa, no literales en código.
- **Hilos crudos con `Thread.Sleep`/`Thread.Abort()`** para polling, sin cancelación, causando fugas y condiciones de carrera con la UI. Aquí: `async`/`await` con `CancellationToken`, o `System.Threading.Timer`.
- **Fugas de recursos GDI** (`Font`, `Bitmap`, `Graphics`, `Image` sin `Dispose`) por una app pensada para correr toda la sesión de trading. Aquí: `using`/`IDisposable` disciplinado en todo lo gráfico.

## Estado actual

**Cambio de arranque (2026-10-13, spec `0002-home-startup`)**: la aplicación abre en `HomeForm` ("Futures Trading — Home", formulario vacío) en vez del login. El login **no se eliminó**: `LoginForm`, `IAuthService` y `BackendApiClient` siguen en el código y con sus tests de unidad e integración; solo los E2E de ventana del login están omitidos con razón escrita. Consecuencia a tener presente: **hoy la app abre sin autenticar**; es aceptable solo porque Home está vacío. Antes de agregar funcionalidad que dependa de la identidad del usuario, retomar el login (`0001`). En los E2E, `AppSession.StartupWindow` es la primera ventana que muestra la app.

**Login funcional de punta a punta (verificado 2026-09-11, hoy en pausa)**, contra el backend real corriendo en local.

Solución `FuturesTrading.sln` en `src/` y `tests/`:
- `FuturesTrading.Domain` — `AuthenticatedUser`.
- `FuturesTrading.Application` — `IAuthService`, `AuthResult` (`Auth/`).
- `FuturesTrading.Infrastructure` — `BackendApiClient` (implementa `IAuthService` con `HttpClient`, habla `POST auth/login` form-urlencoded contra el backend, mapea 401 → mensaje de credenciales inválidas, `HttpRequestException` → mensaje de conexión), `ApiSettings`.
- `FuturesTrading.UI` — `LoginForm` (formulario "tonto", UI construida en código sin `.Designer.cs` separado por simplicidad en esta fase), `MainForm` (placeholder post-login), `Program.cs` con DI (`ServiceCollection` + `IConfiguration` desde `appsettings.json`, `AddHttpClient<IAuthService, BackendApiClient>()`).
- `FuturesTrading.Application.Tests` (xUnit) — tests de `AuthResult` y del contrato de `IAuthService` con un doble de prueba (`FakeAuthService`); 4/4 pasando.

Notas de implementación:
- `Application.Run(...)` colisiona por nombre con el namespace `FuturesTrading.Application` — se resolvió con un alias `using WinFormsApplication = System.Windows.Forms.Application;` en `Program.cs`. Tenerlo en cuenta en cualquier archivo de `UI` que use la clase `Application` de WinForms.
- El login del backend usa `OAuth2PasswordRequestForm` (form-urlencoded), no JSON — `BackendApiClient` envía `FormUrlEncodedContent`, no `PostAsJsonAsync`.

### Tests E2E (FlaUI) — `tests/FuturesTrading.E2E.Tests`

Lanzan la app WinForms **real** (el `.exe` Debug ya compilado), la manejan por UI Automation con FlaUI (`FlaUI.UIA3`) y la prueban en dos modos (8 tests, pasando 2026-10-12):

- **Contra el backend real** (`LoginFlowTests`): arranque (AC-1, no necesita backend), credenciales válidas → ventana principal (AC-2), password incorrecta → error y se queda en el login (AC-3).
- **Contra un backend falso en proceso** (`LoginWindowBehaviorTests`, `FakeBackend`): servidor caído (AC-4), botón deshabilitado y UI que responde mientras la petición está en vuelo (AC-5), `500` (AC-7), `200` sin token (AC-9), y cierre de la principal que termina la app (AC-8). No necesitan backend ni credenciales.

El segundo modo funciona porque `Program.cs` lee también **variables de entorno**: `Api__BaseUrl` sobrescribe el `BaseUrl` de `appsettings.json`. `AppSession(apiBaseUrl)` lanza la app con esa variable. Sin variable, manda el archivo (comportamiento normal).

Cómo correrlos:

1. Tener el backend levantado en `http://localhost:8000` (uvicorn local o `docker compose`) y un usuario ya registrado.
2. Pasar las credenciales **solo por variables de entorno** (nunca en archivos del repo):

```bash
E2E_USERNAME=<usuario> E2E_PASSWORD=<password> dotnet test tests/FuturesTrading.E2E.Tests
```

Detalles a recordar:
- El proyecto apunta a `net8.0-windows` y referencia la UI con `ReferenceOutputAssembly="false"` solo para que se compile antes; ubica el `.exe` subiendo hasta `FuturesTrading.slnx` y bajando a `src/FuturesTrading.UI/bin/Debug/net8.0-windows/`. Solo funciona con la configuración **Debug**.
- Los controles se encuentran por `AutomationId`, que en WinForms es `Control.Name`. **Todo control que un test E2E necesite debe tener `Name` asignado** (`txtUsername`, `txtPassword`, `btnLogin`, `lblError`, `lblWelcome`). Un control con `Visible = false` no existe en el árbol de UI Automation hasta que se muestra.
- No correr los E2E con la app abierta a mano: el build bloquea el `.exe` y los tests lanzan su propia instancia (cada test mata la suya en `Dispose`).
- Los E2E contra el backend real dependen de servicios externos (backend + BD): son los más lentos y frágiles; no incluirlos en un pipeline de CI sin levantar antes el backend. Los de backend falso no tienen esa dependencia.
- **Corren estrictamente en serie** (`DisableTestParallelization` en `AssemblyConfig.cs`): manejan el teclado y el foco reales de Windows; dos a la vez escribirían en la ventana equivocada. **No usar el equipo mientras se ejecutan** (el teclado y el foco son los suyos).
- El timeout de `HttpClient` (100 s) no se espera en un E2E: AC-7 por timeout solo se verifica en `Infrastructure.Tests`.
- `FakeBackend` usa `HttpListener` sobre `localhost` con un puerto libre; no requiere permisos de administrador.

Próximos pasos: el modelo de dominio de `Setup`/`Trade` y la integración con NinjaTrader/ThinkorSwim quedan para iteraciones futuras (decisión explícita de enfocar primero solo en login).
