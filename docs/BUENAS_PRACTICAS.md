# Fase 9 — Checklist de buenas prácticas (rúbrica)

| Categoría | Práctica | Cómo se cumplió |
|---|---|---|
| Datos | DbContext por operación (factory) | `AddPooledDbContextFactory` + `RegisterDbContextFactory<AdventureWorksContext>()` en `Program.cs` |
| Datos | Proyección activada | `[UseProjection]` en los resolvers + `.AddProjections()`; SQL pide solo columnas consultadas |
| Datos | DataLoader en relaciones N+1 | `CategoryBySubcategoryDataLoader` (Escenario A) — batch `WHERE ... IN (...)` con JOIN |
| Datos | FK necesaria por el resolver siempre proyectada | `[IsProjected(true)]` en `Product.ProductSubcategoryID` |
| Seguridad | Límite de profundidad | `AddMaxExecutionDepthRule(8)` |
| Seguridad | Análisis de costo/complejidad | Habilitado por defecto en Hot Chocolate 16 (directiva `@cost`) |
| Seguridad | Errores tipados (sin stack traces en prod) | `[Error(typeof(ProductNotFoundException))]` + `.AddMutationConventions()`; `IncludeExceptionDetails` solo en Development |
| Código | Capa GraphQL delgada | Carpeta `GraphQL/` (Query, Mutation, Subscription, Inputs, Errors, Types, DataLoaders) separada de `Data/` |
| Código | Cadena de conexión fuera del código | `appsettings.json` + scaffolding con `--no-onconfiguring` |
| Código | Mutaciones `async` con `CancellationToken` | `AddProductAsync` / `UpdatePriceAsync` reciben y propagan `CancellationToken ct` |
| Tiempo real | Suscripciones | `AddSubscriptionType` + `AddInMemorySubscriptions` + `app.UseWebSockets()` |

## Desviaciones respecto al plan original (entorno real y versión de librerías)

1. **BD real:** `Server=MARIEZERO;Database=lab08;…` (la base AdventureWorks está restaurada
   como `lab08` en SQL Server completo, no en LocalDB).
2. **`global.json`** fija el SDK .NET 10.0.203 (el equipo tiene .NET 11 preview por defecto).
3. **Hot Chocolate 16.1.3** (el plan asumía 14+):
   - `.AddMutationConventions()` es **obligatorio** para `[Error(typeof(...))]`; sin él el
     esquema no arranca. Esto define la forma de los payloads (`productPayload`,
     `{ product, errors }`).
   - DTO `AddProductPayload` renombrado a `ProductPayload` para no colisionar con el
     `AddProductPayload` que autogeneran las conventions.
   - La línea `ModifyRequestOptions(o => o.Complexity.Enable = true)` del plan ya no existe;
     el análisis de costo viene activo por defecto.
4. **Escenario A — corrección de N+1:** el DataLoader del plan (cosido por id de categoría)
   dejaba un N+1 en la consulta de subcategoría dentro del resolver. Se rediseñó cosiéndolo
   por `ProductSubcategoryID` con JOIN, logrando una sola consulta batched (ver
   `COMO_SE_EVITO_EL_N+1.md`).
5. **Escenario B:** `CustomerSales` pasó de record posicional a clase con inicializador de
   objeto (EF Core no traduce el constructor de un record dentro de `GroupBy`).
