# Tareas 0001 — Ventana de login

## Implementación (hecha)

- [x] T1 (AC-2, AC-3) `IAuthService`, `AuthResult`, `AuthenticatedUser`
- [x] T2 (AC-2, AC-3, AC-4, AC-6, AC-7) `BackendApiClient` con manejo de `401`, `5xx`, red y timeout
- [x] T3 (AC-1, AC-2, AC-3, AC-5, AC-8) `LoginForm` y `MainForm`
- [x] T4 `Program.cs` con DI y `appsettings.json`
- [x] T5 (AC-2, AC-3) Tests E2E con FlaUI; controles con `Name` para UI Automation

## Brecha de verificación (pendiente)

Hecho (2026-10-12): 12 tests — 2 unit, 7 integración, 3 E2E:

- [x] T6 (AC-2, AC-3, AC-4, AC-6, AC-7, AC-9) `FuturesTrading.Infrastructure.Tests`: `BackendApiClient` con un `HttpMessageHandler` falso. Éxito, `401`, `500`, `HttpRequestException`, timeout, `200` sin token, y que la petición sea `POST` form-urlencoded con la password en el cuerpo y nunca en la URL.
- [x] T7 (AC-1) E2E de arranque: título, `txtUsername`, `txtPassword` (enmascarado, vía `IsPassword`) y `btnLogin`. No necesita backend.
- [x] T9 Eliminado `IAuthServiceContractTests` (probaba un doble escrito por nosotros). Sus casos los cubre ahora `BackendApiClientTests` con comportamiento real.
- [x] T10 Todos los tests llevan el `AC-n` en el nombre.
- [x] AC-9 agregado a la spec al escribir los tests (el comportamiento ya existía en el código).

Pendiente:

- [ ] T8 (AC-4, AC-5, AC-7, AC-9 a nivel de **ventana**) Verificar que `LoginForm` muestra el mensaje, deshabilita el botón mientras espera y no se cierra. Hoy solo se prueba que `BackendApiClient` produce el resultado correcto, no que la UI lo presente. **Requiere decisión:** ver "Propuesta T8" abajo.
- [ ] T11 Resolver las preguntas abiertas de la spec (expiración del token, validación en cliente).

### Propuesta T8 (pendiente de aprobación)

Para probar AC-5/AC-4/AC-7 en la ventana hace falta un backend controlable (lento, caído, que devuelva `500`). Opción propuesta:

1. En `Program.cs`, que la configuración también lea variables de entorno (`AddEnvironmentVariables()`), de modo que `Api__BaseUrl` sobrescriba `appsettings.json`. Es un cambio pequeño y sin efecto en el uso normal.
2. En los E2E, levantar un servidor falso en proceso (`HttpListener`) con el comportamiento deseado y lanzar la app apuntando a él.

Beneficio: E2E deterministas que no dependen del backend real ni de credenciales. Costo: toca código de producción (aunque solo configuración) y agrega un servidor falso a mantener. Alternativa: probar la lógica del formulario sin UI Automation, lo que exigiría extraer esa lógica a una clase testeable (más refactor).

## Hecho cuando

- AC-1…AC-9 tienen verificación automatizada o justificación escrita. **No cumplido todavía:** AC-5 no tiene ningún test, AC-8 solo se ejerce indirectamente, y AC-4/7/9 se verifican en `BackendApiClient` pero no en la ventana (T8).
- Unit, integración y E2E existen y corren con `dotnet test FuturesTrading.slnx`.
