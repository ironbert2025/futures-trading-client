# Spec 0002 — Formulario Home como pantalla de arranque

- **Estado**: aprobada (decisiones del usuario, 2026-10-13: nombre `HomeForm`, título "Futures Trading — Home", login E2E omitidos con razón)
- **Fecha**: 2026-10-13
- **Repo dueño**: client
- **Specs relacionadas**: `0001-auth-login` — **queda en pausa**: la aplicación deja de arrancar en el login. No se elimina nada de ella.

## Problema / objetivo

El usuario quiere avanzar con otras pantallas sin pasar por el login. Mientras el login queda de lado, la aplicación debe abrir directamente en un formulario propio, vacío, sobre el que se construirá lo siguiente.

## Historias de usuario

- Como desarrollador, quiero que la aplicación abra en un formulario Home vacío para construir nuevas pantallas sin depender del login ni del backend.

## Criterios de aceptación

| ID | Dado / Cuando / Entonces | Verificación |
|---|---|---|
| AC-1 | **Dado** que se inicia la aplicación, **entonces** se muestra la ventana "Futures Trading — Home", vacía, y **no** se muestra la ventana de login. | E2E · `Ac1_Startup_ShowsTheHomeWindow_AndNotTheLogin` |
| AC-2 | **Dado** que se cierra la ventana Home, **entonces** la aplicación termina por completo. | E2E · `Ac2_ClosingHome_EndsTheApplication` |
| AC-3 | **Dado** que el login está en pausa, **entonces** su código (`LoginForm`, `IAuthService`, `BackendApiClient`) y sus tests unitarios y de integración se conservan y siguen pasando; solo los E2E de ventana del login quedan omitidos, con la razón escrita en cada uno. | `dotnet test`: unit e integración pasan, los 8 E2E de login aparecen como omitidos con su razón |

## Fuera de alcance

- Eliminar o modificar `LoginForm`, `MainForm`, `IAuthService` o `BackendApiClient`.
- Definir qué contiene Home (queda vacío).
- Decidir cómo y cuándo vuelve el login al flujo.

## Preguntas abiertas

- **¿Cómo se reincorpora el login?** (antes de Home, o desde Home al necesitar identidad). Se decide al retomar `0001`.
- **Hoy la aplicación abre sin autenticar.** Es aceptable solo porque Home está vacío y no hay nada que proteger. Antes de agregar funcionalidad que dependa de saber quién es el usuario (o que toque datos suyos), hay que retomar el login: la regla del proyecto es no saltarse la autenticación real.
