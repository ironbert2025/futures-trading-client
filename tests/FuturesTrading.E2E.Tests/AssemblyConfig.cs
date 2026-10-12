// Los E2E manejan teclado y foco reales de la sesión de Windows: dos tests a la vez
// escribirían en la ventana equivocada. Se ejecutan estrictamente en serie.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
