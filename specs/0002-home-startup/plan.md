# Plan 0002 — Formulario Home como pantalla de arranque

- **Spec**: `spec.md` (misma carpeta)
- **Estado**: implementado

## Enfoque

Agregar `HomeForm` (formulario vacío) en `UI/Forms` y hacer que `Program.cs` arranque con él en vez de con `LoginForm`. El login no se toca: `LoginForm` sigue en el código y registrado en DI, solo que nada lo muestra.

## Decisiones técnicas

| Decisión | Alternativas descartadas | Por qué |
|---|---|---|
| Omitir (`Skip`) los 8 E2E de ventana del login, con razón escrita | Interruptor por variable de entorno para elegir el formulario de arranque | Es lo mínimo y reversible; el interruptor agregaría al código de producción algo que nadie pidió. Los tests de unidad e integración del login siguen protegiéndolo |
| Título "Futures Trading — Home" | Reusar "Futures Trading" | Ese título ya es el de `MainForm` (placeholder post-login) y los E2E del login lo buscan para distinguir ventanas |
| `AppSession.LoginWindow` pasa a llamarse `StartupWindow` | Dejar el nombre | Ya no es necesariamente el login: es la primera ventana que muestra la app |
| `LoginForm` queda registrado en DI | Quitarlo | Es código en pausa, no eliminado; quitarlo y volver a ponerlo es ruido |

## Capas / archivos afectados

- `UI/Forms/HomeForm.cs` — nuevo, vacío.
- `UI/Program.cs` — `AddTransient<HomeForm>()` y `Run(HomeForm)`.
- `tests/FuturesTrading.E2E.Tests` — `HomeWindowTests` (nuevo), `PausedReasons`, `[Fact(Skip = …)]` en los E2E del login, renombre `LoginWindow` → `StartupWindow` en `AppSession`.

## Contrato

Sin cambios: no hay llamadas al backend al arrancar.

## Riesgos

- Los E2E omitidos pierden la protección a nivel ventana del login; nada fuerza reactivarlos. Mitigación: tarea explícita en `tasks.md`.
- Home abre sin autenticar (ver preguntas abiertas de la spec).
