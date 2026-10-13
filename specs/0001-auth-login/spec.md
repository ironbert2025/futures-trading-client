# Spec 0001 — Ventana de login

- **Estado**: implementada (spec retroactiva, escrita el 2026-10-12 a partir del código existente) · **en pausa desde 2026-10-13**: la app ya no arranca en el login (ver `0002-home-startup`) y los 8 E2E de ventana están omitidos. El código y los tests de unidad e integración se conservan.
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
| AC-1 | **Dado** que se inicia la aplicación, **entonces** se muestra la ventana "Futures Trading — Ingresar" con campos Usuario y Password (enmascarado) y el botón Ingresar. | **Suspendido por 0002**: hoy la app arranca en Home. E2E omitido · `Ac1_Startup_ShowsLoginWindowWithMaskedPasswordAndLoginButton` (título, los tres controles, password enmascarado; no necesita backend) |
| AC-2 | **Dado** credenciales válidas, **cuando** se pulsa Ingresar, **entonces** se abre la ventana principal con "Bienvenido, {usuario}" y la de login se oculta. | Integración · `Ac2_Success_*` (`BackendApiClient` devuelve usuario y token) · E2E · `Ac2_Login_WithValidCredentials_*` · Unit · `Ac2_Ok_*` |
| AC-3 | **Dado** credenciales inválidas (el backend responde `401`), **cuando** se pulsa Ingresar, **entonces** se muestra "Usuario o contraseña incorrectos." y la aplicación permanece en el login. | Integración · `Ac3_Unauthorized_*` · E2E · `Ac3_Login_WithWrongPassword_*` · Unit · `Ac3_Fail_*` |
| AC-4 | **Dado** que el backend no es alcanzable, **cuando** se pulsa Ingresar, **entonces** se muestra "No se pudo conectar con el servidor. Verifique que la API esté corriendo." sin excepción sin manejar. | Integración · `Ac4_Unreachable_*` (el cliente devuelve el mensaje en vez de lanzar) · E2E de ventana contra backend falso · `Ac4_BackendUnreachable_*` |
| AC-5 | **Dado** que hay un intento de login en curso, **entonces** el botón Ingresar queda deshabilitado, la interfaz no se bloquea, y se rehabilita al terminar (éxito o error). | E2E de ventana contra backend falso (respuesta con 3 s de retraso) · `Ac5_WhileTheRequestIsInFlight_*`: botón deshabilitado y UI Automation responde **antes** de que el servidor conteste, y rehabilitado después |
| AC-6 | **Dado** cualquier intento de login, **entonces** la contraseña viaja en el cuerpo de un `POST` (form-urlencoded), **nunca** en la URL. | Integración · `Ac6_SendsPostWithFormBodyAndNeverPutsThePasswordInTheUrl` (método, URL, content-type y cuerpo reales) |
| AC-7 | **Dado** que el backend responde con un error del servidor (`5xx`) o tarda demasiado, **entonces** se muestra un mensaje genérico con el código o de "tardó demasiado", sin cerrar la aplicación. | Integración · `Ac7_ServerError_*` y `Ac7_Timeout_*` (mensajes exactos). E2E de ventana · `Ac7_ServerError_*` (mensaje con el código y la app sigue en el login). El **timeout** solo se verifica en integración: el de `HttpClient` es de 100 s y no se espera en un E2E |
| AC-8 | **Dado** que se cierra la ventana principal, **entonces** la aplicación termina por completo (no queda el login oculto en segundo plano). | E2E de ventana · `Ac8_ClosingTheMainWindow_EndsTheWholeApplication`: cierra la principal y comprueba que el proceso termina |
| AC-9 | **Dado** que el backend responde `200` pero sin un token utilizable, **cuando** se pulsa Ingresar, **entonces** se muestra "Respuesta inválida del servidor." y no se abre la ventana principal. *(Agregado al escribir los tests de `BackendApiClient`: el comportamiento ya existía en el código.)* | Integración · `Ac9_OkWithoutAToken_*` · E2E de ventana · `Ac9_OkWithoutAToken_*` |

> Niveles: **Unit** `tests/FuturesTrading.Application.Tests` · **Integración** `tests/FuturesTrading.Infrastructure.Tests` (`HttpMessageHandler` falso, sin red) · **E2E** `tests/FuturesTrading.E2E.Tests`, en dos modos: contra el **backend real** (`LoginFlowTests`: AC-1, AC-2, AC-3) y contra un **backend falso en proceso** (`LoginWindowBehaviorTests`: AC-4, AC-5, AC-7, AC-8, AC-9; la app se lanza con `Api__BaseUrl`). Los tests llevan el `AC-n` en el nombre.
>
> **En pausa (spec 0002):** los E2E de este documento (los de `LoginFlowTests` y `LoginWindowBehaviorTests`) están marcados `Skip` con razón escrita, porque la app arranca en `HomeForm`. Los de unidad e integración siguen corriendo. Reactivarlos es la tarea T6 de `0002`.
>
> **Límites conocidos:** (1) el timeout (AC-7) no se prueba en la ventana; (2) los E2E manejan teclado y foco reales, por eso corren en serie y no se debe usar el equipo mientras se ejecutan; (3) los E2E de ventana prueban el comportamiento ante respuestas simuladas, no que el backend real emita esas respuestas (eso es del backend).

## Fuera de alcance

- Persistir la sesión entre ejecuciones, "recordarme", cierre de sesión, cambio de contraseña.
- Qué hace la ventana principal después del login (hoy es un placeholder).
- El contrato HTTP y las reglas de validación de credenciales (viven en el backend).

## Preguntas abiertas

- **¿Qué hace el cliente cuando el token expira (60 min)?** Hoy no lo usa para nada después del login; cuando existan llamadas autenticadas habrá que decidir si pide login de nuevo.
- **El campo Password no limita longitud ni valida formato en el cliente** (lo hace el backend). Decidir si se quiere validación previa en la UI.
