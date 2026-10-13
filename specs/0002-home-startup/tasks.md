# Tareas 0002 — Formulario Home como pantalla de arranque

- [x] T1 `HomeForm`: formulario vacío, título "Futures Trading — Home".
- [x] T2 (AC-1, AC-2) `Program.cs` arranca con `HomeForm`.
- [x] T3 (AC-1, AC-2) E2E `HomeWindowTests`; renombrar `LoginWindow` → `StartupWindow` en `AppSession`.
- [x] T4 (AC-3) Omitir los 8 E2E de ventana del login con `Skip = PausedReasons.Login`.
- [x] T5 Marcar `0001` como en pausa; actualizar índice de specs y `AGENTS.md`.

## Pendiente

- [ ] T6 **Al retomar el login (`0001`)**: quitar `Skip = PausedReasons.Login` de los 8 tests, decidir cómo se reincorpora el login al arranque (pregunta abierta de la spec) y volver a ejecutar la suite completa.

## Hecho cuando

- `dotnet test FuturesTrading.slnx`: unit e integración pasan, los 2 E2E nuevos pasan, los 8 de login aparecen omitidos con su razón.
