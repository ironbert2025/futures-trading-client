# Spec 0001 — Ventana de login

- **Estado**: implementada (spec retroactiva, escrita el 2026-10-12 a partir del código existente)
- **Fecha**: 2026-10-12
- **Repo dueño**: client
- **Specs relacionadas**: `futures-trading-backend/specs/0001-auth-login` — **dueña del contrato HTTP**. Aquí se referencia, no se copia: los códigos `401`/`5xx` y el formato del token son los que define esa spec (AC-4 y AC-5 del backend).

## Problema / objetivo

El trader debe identificarse antes de usar la aplicación. La ventana de login captura usuario y contraseña, los valida contra el backend y, si son correctos, abre la ventana principal. Reemplaza el login legacy de `Setup_Client`, que enviaba la contraseña por query string y la sobrescribía con un literal.

## Historias de usuario

- Como trader, quiero **ingresar con usuario y contraseña** para acceder a la aplicación.
- Como trader, quiero **un mensaje claro cuando algo falla** (credenciales mal, servidor caído) para saber qué hacer, sin que la aplicación se cierre sola.

## Criterios de aceptación

| ID | Dado / Cuando / Entonces | Verificación |
|---|---|---|
| AC-1 | **Dado** que se inicia la aplicación, **entonces** se muestra la ventana "Futures Trading — Ingresar" con campos Usuario y Password (enmascarado) y el botón Ingresar. | E2E · `Ac1_Startup_ShowsLoginWindowWithMaskedPasswordAndLoginButton` (título, los tres controles, password enmascarado; no necesita backend) |
| AC-2 | **Dado** credenciales válidas, **cuando** se pulsa Ingresar, **entonces** se abre la ventana principal con "Bienvenido, {usuario}" y la de login se oculta. | Integración · `Ac2_Success_*` (`BackendApiClient` devuelve usuario y token) · E2E · `Ac2_Login_WithValidCredentials_*` · Unit · `Ac2_Ok_*` |
| AC-3 | **Dado** credenciales inválidas (el backend responde `401`), **cuando** se pulsa Ingresar, **entonces** se muestra "Usuario o contraseña incorrectos." y la aplicación permanece en el login. | Integración · `Ac3_Unauthorized_*` · E2E · `Ac3_Login_WithWrongPassword_*` · Unit · `Ac3_Fail_*` |
| AC-4 | **Dado** que el backend no es alcanzable, **cuando** se pulsa Ingresar, **entonces** se muestra "No se pudo conectar con el servidor. Verifique que la API esté corriendo." sin excepción sin manejar. | Integración · `Ac4_Unreachable_*` (el cliente devuelve el mensaje en vez de lanzar). Que la **ventana** lo muestre: sin verificar (T8) |
| AC-5 | **Dado** que hay un intento de login en curso, **entonces** el botón Ingresar queda deshabilitado, la interfaz no se bloquea, y se rehabilita al terminar (éxito o error). | Sin verificar |
| AC-6 | **Dado** cualquier intento de login, **entonces** la contraseña viaja en el cuerpo de un `POST` (form-urlencoded), **nunca** en la URL. | Integración · `Ac6_SendsPostWithFormBodyAndNeverPutsThePasswordInTheUrl` (método, URL, content-type y cuerpo reales) |
| AC-7 | **Dado** que el backend responde con un error del servidor (`5xx`) o tarda demasiado, **entonces** se muestra un mensaje genérico con el código o de "tardó demasiado", sin cerrar la aplicación. | Integración · `Ac7_ServerError_*` y `Ac7_Timeout_*` (mensajes exactos). Que la **ventana** lo muestre y siga abierta: sin verificar (T8) |
| AC-8 | **Dado** que se cierra la ventana principal, **entonces** la aplicación termina por completo (no queda el login oculto en segundo plano). | E2E lo ejerce indirectamente (`Dispose` mata el proceso); sin aserción propia |
| AC-9 | **Dado** que el backend responde `200` pero sin un token utilizable, **cuando** se pulsa Ingresar, **entonces** se muestra "Respuesta inválida del servidor." y no se abre la ventana principal. *(Agregado al escribir los tests de `BackendApiClient`: el comportamiento ya existía en el código.)* | Integración · `Ac9_OkWithoutAToken_*` |

> Niveles: **Unit** `tests/FuturesTrading.Application.Tests` · **Integración** `tests/FuturesTrading.Infrastructure.Tests` (`HttpMessageHandler` falso, sin red) · **E2E** `tests/FuturesTrading.E2E.Tests` (UI real + backend real). Los tests llevan el `AC-n` en el nombre.
>
> **Brecha que queda:** los criterios AC-4, AC-5, AC-7 y AC-9 describen lo que el *usuario ve*, pero solo está verificado que `BackendApiClient` produce el resultado correcto; no hay una prueba de que `LoginForm` lo muestre, deshabilite el botón (AC-5) o siga abierto. Ver T8.

## Fuera de alcance

- Persistir la sesión entre ejecuciones, "recordarme", cierre de sesión, cambio de contraseña.
- Qué hace la ventana principal después del login (hoy es un placeholder).
- El contrato HTTP y las reglas de validación de credenciales (viven en el backend).

## Preguntas abiertas

- **¿Qué hace el cliente cuando el token expira (60 min)?** Hoy no lo usa para nada después del login; cuando existan llamadas autenticadas habrá que decidir si pide login de nuevo.
- **El campo Password no limita longitud ni valida formato en el cliente** (lo hace el backend). Decidir si se quiere validación previa en la UI.
