# Tareas 0001 — Ventana de login

## Implementación (hecha)

- [x] T1 (AC-2, AC-3) `IAuthService`, `AuthResult`, `AuthenticatedUser`
- [x] T2 (AC-2, AC-3, AC-4, AC-6, AC-7) `BackendApiClient` con manejo de `401`, `5xx`, red y timeout
- [x] T3 (AC-1, AC-2, AC-3, AC-5, AC-8) `LoginForm` y `MainForm`
- [x] T4 `Program.cs` con DI y `appsettings.json`
- [x] T5 (AC-2, AC-3) Tests E2E con FlaUI; controles con `Name` para UI Automation

## Brecha de verificación (pendiente)

Hoy: 4 unit tests (sobre `AuthResult` y un doble de prueba, no sobre lógica real) y 2 E2E. **No existe proyecto de integración.** AC-4, AC-5 y AC-7 no tienen ninguna verificación; AC-6 solo tiene revisión de código.

- [ ] T6 (AC-4, AC-6, AC-7) Crear `FuturesTrading.Infrastructure.Tests` y probar `BackendApiClient` con un `HttpMessageHandler` falso: `401`, `500`, `HttpRequestException`, timeout, y que el body sea form-urlencoded sin password en la URL.
- [ ] T7 (AC-1) Ampliar el E2E de arranque: existencia de `txtUsername`, `txtPassword` y `btnLogin`.
- [ ] T8 (AC-5) Verificar el botón deshabilitado durante la petición (E2E con backend lento o prueba de la lógica del formulario).
- [ ] T9 Reemplazar los 2 tests de `IAuthServiceContractTests` (prueban un doble que escribimos nosotros) por tests de comportamiento real, o eliminarlos.
- [ ] T10 Nombrar cada test con su `AC-n` (p. ej. `Ac3_Login_WithWrongPassword_...`).
- [ ] T11 Resolver las preguntas abiertas de la spec.

## Hecho cuando

- AC-1…AC-8 tienen verificación automatizada o justificación escrita.
- Unit, integración y E2E existen y corren con `dotnet test`.
