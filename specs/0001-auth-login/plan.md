# Plan 0001 — Ventana de login (as-built)

- **Spec**: `spec.md` (misma carpeta) · **Contrato**: `futures-trading-backend/specs/0001-auth-login/plan.md`
- **Estado**: implementado — documenta lo construido.

## Enfoque

Clean Architecture con la regla de dependencia hacia adentro. El formulario no conoce HTTP: llama a `IAuthService` (definida en `Application`), cuya implementación real (`BackendApiClient`, en `Infrastructure`) se inyecta por DI en `Program.cs`.

## Decisiones técnicas

| Decisión | Alternativas descartadas | Por qué |
|---|---|---|
| WinForms sobre .NET 8 | WPF | Continuidad con la experiencia previa y con el SDK de NinjaTrader; la separación UI/lógica la da la arquitectura, no el framework |
| `HttpClient` inyectado vía `AddHttpClient<IAuthService, BackendApiClient>()` | `HttpWebRequest` síncrono (legacy) | Async de extremo a extremo; UI no se bloquea |
| `AuthResult` (éxito/fallo) en vez de excepciones hacia la UI | Propagar `HttpRequestException` | La UI solo decide qué mostrar; los fallos de red se convierten en mensaje en `Infrastructure` |
| Formularios con UI construida en código, sin `.Designer.cs` | Diseñador de VS | Simplicidad en esta fase; los controles llevan `Name` para UI Automation |
| `BaseUrl` en `appsettings.json` | Valor en código | Cambiable sin recompilar; ambos modos del backend usan `:8000` |

## Capas / archivos afectados

- `Domain` — `AuthenticatedUser` (`Username`, `AccessToken`).
- `Application/Auth` — `IAuthService`, `AuthResult`.
- `Infrastructure` — `Auth/BackendApiClient` (POST form-urlencoded a `auth/login`; mapea `401`, otros `!2xx`, `HttpRequestException` y timeout a `AuthResult.Fail`), `ApiSettings`.
- `UI` — `Forms/LoginForm`, `Forms/MainForm` (placeholder), `Program.cs` (DI + configuración), `appsettings.json`.

## Contrato

Consumido, no definido aquí: `POST {BaseUrl}auth/login` → `200 {access_token, token_type}` / `401`. Ver la spec del backend.

## Riesgos

- El token se guarda en memoria en `AuthenticatedUser` y hoy no se usa; cuando se use, definir expiración y renovación.
- `MainForm` no recibe `IAuthService` ni nada por DI: se crea con `new`. Aceptable mientras es un placeholder; replantear cuando tenga dependencias.
- Los E2E dependen de un backend vivo y de la compilación Debug; son frágiles ante cambios de texto en la UI.
