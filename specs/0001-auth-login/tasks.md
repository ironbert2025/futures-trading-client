# Tareas 0001 — Ventana de login

## Implementación (hecha)

- [x] T1 (AC-2, AC-3) `IAuthService`, `AuthResult`, `AuthenticatedUser`
- [x] T2 (AC-2, AC-3, AC-4, AC-6, AC-7) `BackendApiClient` con manejo de `401`, `5xx`, red y timeout
- [x] T3 (AC-1, AC-2, AC-3, AC-5, AC-8) `LoginForm` y `MainForm`
- [x] T4 `Program.cs` con DI y `appsettings.json`
- [x] T5 (AC-2, AC-3) Tests E2E con FlaUI; controles con `Name` para UI Automation

## Brecha de verificación (pendiente)

Hecho (2026-10-12): 17 tests — 2 unit, 7 integración, 8 E2E (3 contra backend real, 5 contra backend falso):

- [x] T6 (AC-2, AC-3, AC-4, AC-6, AC-7, AC-9) `FuturesTrading.Infrastructure.Tests`: `BackendApiClient` con un `HttpMessageHandler` falso. Éxito, `401`, `500`, `HttpRequestException`, timeout, `200` sin token, y que la petición sea `POST` form-urlencoded con la password en el cuerpo y nunca en la URL.
- [x] T7 (AC-1) E2E de arranque: título, `txtUsername`, `txtPassword` (enmascarado, vía `IsPassword`) y `btnLogin`. No necesita backend.
- [x] T9 Eliminado `IAuthServiceContractTests` (probaba un doble escrito por nosotros). Sus casos los cubre ahora `BackendApiClientTests` con comportamiento real.
- [x] T10 Todos los tests llevan el `AC-n` en el nombre.
- [x] AC-9 agregado a la spec al escribir los tests (el comportamiento ya existía en el código).
- [x] T8 (AC-4, AC-5, AC-7, AC-8, AC-9 a nivel de **ventana**, aprobado 2026-10-12) `Program.cs` lee también variables de entorno (`Api__BaseUrl` sobrescribe `appsettings.json`; decisión en `plan.md`). `LoginWindowBehaviorTests` lanza la app real contra un `FakeBackend` en proceso (`HttpListener`): servidor caído, respuesta lenta, `500`, `200` sin token, y cierre de la ventana principal. Los E2E corren en serie (`DisableTestParallelization`) porque comparten teclado y foco.

Pendiente:

- [ ] T11 Resolver las preguntas abiertas de la spec (expiración del token, validación en cliente).

Mutaciones comprobadas a mano (y restauradas con git) sobre `LoginForm`: no deshabilitar el botón → falla AC-5; no cerrar el login al cerrar la principal → falla AC-8; no mostrar el error → fallan AC-4/5/7/9. Sobre `BackendApiClient`: no reconocer `401`, password en la URL, no capturar `HttpRequestException` → falla el test correspondiente.

## Hecho cuando

- AC-1…AC-9 tienen verificación automatizada o justificación escrita. **Cumplido**, con un límite documentado: el timeout (AC-7) solo se prueba en integración, no en la ventana.
- Unit, integración y E2E existen y corren con `dotnet test FuturesTrading.slnx`.
