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
| [0001-auth-login](0001-auth-login/spec.md) | Ventana de login | Implementada (retroactiva) | 9 AC, 12 tests (2 unit, 7 integración, 3 E2E): 7 con test automatizado, AC-8 solo indirecto, **AC-5 sin ninguno**. AC-4/7/9 se prueban en `BackendApiClient`, no en la ventana — T8 |
