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
| AC-1 | **Dado** que se inicia la aplicación, **entonces** se muestra la ventana "Futures Trading — Ingresar" con campos Usuario y Password (enmascarado) y el botón Ingresar. | E2E (solo comprueba el título); campos y enmascarado sin verificar |
| AC-2 | **Dado** credenciales válidas, **cuando** se pulsa Ingresar, **entonces** se abre la ventana principal con "Bienvenido, {usuario}" y la de login se oculta. | E2E · `Login_WithValidCredentials_ShowsMainWindowWithWelcome` |
| AC-3 | **Dado** credenciales inválidas (el backend responde `401`), **cuando** se pulsa Ingresar, **entonces** se muestra "Usuario o contraseña incorrectos." y la aplicación permanece en el login. | E2E · `Login_WithWrongPassword_ShowsErrorAndStaysOnLogin` |
| AC-4 | **Dado** que el backend no es alcanzable, **cuando** se pulsa Ingresar, **entonces** se muestra "No se pudo conectar con el servidor. Verifique que la API esté corriendo." sin excepción sin manejar. | Sin verificar |
| AC-5 | **Dado** que hay un intento de login en curso, **entonces** el botón Ingresar queda deshabilitado, la interfaz no se bloquea, y se rehabilita al terminar (éxito o error). | Sin verificar |
| AC-6 | **Dado** cualquier intento de login, **entonces** la contraseña viaja en el cuerpo de un `POST` (form-urlencoded), **nunca** en la URL. | Revisión de código; sin test automatizado |
| AC-7 | **Dado** que el backend responde con un error del servidor (`5xx`) o tarda demasiado, **entonces** se muestra un mensaje genérico con el código o de "tardó demasiado", sin cerrar la aplicación. | Sin verificar |
| AC-8 | **Dado** que se cierra la ventana principal, **entonces** la aplicación termina por completo (no queda el login oculto en segundo plano). | E2E lo ejerce indirectamente (`Dispose` mata el proceso); sin aserción propia |

## Fuera de alcance

- Persistir la sesión entre ejecuciones, "recordarme", cierre de sesión, cambio de contraseña.
- Qué hace la ventana principal después del login (hoy es un placeholder).
- El contrato HTTP y las reglas de validación de credenciales (viven en el backend).

## Preguntas abiertas

- **¿Qué hace el cliente cuando el token expira (60 min)?** Hoy no lo usa para nada después del login; cuando existan llamadas autenticadas habrá que decidir si pide login de nuevo.
- **El campo Password no limita longitud ni valida formato en el cliente** (lo hace el backend). Decidir si se quiere validación previa en la UI.
