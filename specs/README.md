# Specs — futures-trading-client

Este proyecto se desarrolla con **SDD (Spec-Driven Development)**: nada de código sin una spec aprobada.

## Flujo por feature

1. **`spec.md`** — qué y por qué: criterios de aceptación con ID (`AC-n`), fuera de alcance, preguntas abiertas. *Aprueba el usuario.*
2. **`plan.md`** — cómo: decisiones técnicas, capas, contrato, riesgos. *Aprueba el usuario.*
3. **`tasks.md`** — checklist ordenada, cada tarea ligada a un `AC`. *Se ejecuta y se marca.*
4. **Código + tests** en el mismo cambio. Cada test lleva su `AC-n` en el nombre.

Reglas:

- Si el comportamiento cambia, **la spec cambia primero**. Un bug es una spec incompleta.
- La spec describe comportamiento observable, no implementación.
- Una feature que cruza repos tiene **una spec dueña del contrato** (el backend para todo lo HTTP) y una spec aquí que la referencia sin copiarla.
- Plantillas en `_template/` (copia de las del backend; mantenerlas iguales). Numeración correlativa de 4 dígitos, compartida con el backend para la misma feature.
- Cada AC se verifica en el nivel **más barato** que pueda demostrarlo: unit en `Domain`/`Application`, integración en `Infrastructure`, E2E solo para flujos críticos de punta a punta.

## Índice

| Spec | Título | Estado | Cobertura de AC |
|---|---|---|---|
| [0001-auth-login](0001-auth-login/spec.md) | Ventana de login | Implementada · **en pausa** desde 2026-10-13 (ver 0002) | 9 AC, 17 tests (2 unit, 7 integración, 8 E2E **omitidos** mientras el login está en pausa). Límite: el timeout (AC-7) solo en integración |
| [0002-home-startup](0002-home-startup/spec.md) | Formulario Home como pantalla de arranque | Implementada | 3 AC: AC-1 y AC-2 con E2E (2 tests, sin backend), AC-3 verificado por `dotnet test` (8 E2E omitidos con razón) |
