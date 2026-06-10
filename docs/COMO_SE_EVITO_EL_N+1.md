# Cómo se evitó el problema N+1

**Proyecto:** AdventureGraphQL.Api — Hot Chocolate 16 + EF Core 10 sobre `lab08` (esquema AdventureWorks).

El problema **N+1** aparece cuando, para una lista de N elementos, se ejecuta 1 consulta
para la lista y luego N consultas adicionales (una por elemento) para resolver un dato
relacionado. En esta API se combinaron **tres** mecanismos para evitarlo.

## 1. `[UseProjection]` — solo se piden las columnas consultadas

El resolver de productos devuelve `IQueryable<Product>` decorado con `[UseProjection]`.
Hot Chocolate traduce la selección GraphQL del cliente a un único `SELECT` con solo las
columnas pedidas; no materializa la entidad completa ni dispara cargas perezosas.

Consulta GraphQL:
```graphql
query { products(first: 10) { nodes { productID name category } } }
```
SQL real generado (log de EF Core, nivel `Information`):
```sql
SELECT TOP(@p) [p].[ProductID], [p].[Name], [p].[ProductSubcategoryID]
FROM [Production].[Product] AS [p]
```
Se piden 3 columnas (no las ~25 de la tabla). `ProductSubcategoryID` aparece porque está
anotado con `[IsProjected(true)]` en la entidad: el resolver de `category` lo necesita y la
proyección debe incluirlo aunque el cliente no lo seleccione explícitamente.

## 2. DataLoader — el campo `category` se resuelve en lote (no N+1)

El campo calculado `category` (Escenario A) no consulta la BD por cada producto. Usa
`CategoryBySubcategoryDataLoader`, un `BatchDataLoader` cosido por `ProductSubcategoryID`
que **agrupa** todas las claves de la petición y resuelve subcategoría→categoría en
**una sola** consulta con `WHERE ... IN (...)` y un `JOIN`.

SQL real para una página con 10 subcategorías distintas:
```sql
SELECT [p].[ProductSubcategoryID], [p0].[ProductCategoryID], [p0].[Name], ...
FROM [Production].[ProductSubcategory] AS [p]
INNER JOIN [Production].[ProductCategory] AS [p0]
        ON [p].[ProductCategoryID] = [p0].[ProductCategoryID]
WHERE [p].[ProductSubcategoryID] IN
      (@subcategoryIds1, @subcategoryIds2, ..., @subcategoryIds10)
```
Resultado: **1 consulta de productos + 1 consulta batched de categorías**, sin importar
cuántos productos devuelva la página.

### Evidencia del antes/después
Una implementación ingenua (consultar la subcategoría con `FirstOrDefaultAsync` dentro del
resolver) producía el patrón N+1: 1 consulta de productos **+ N consultas**
`SELECT TOP(1) ... FROM ProductSubcategory WHERE ProductSubcategoryID = @id`, una por
producto. El rediseño con DataLoader colapsa esas N consultas en una sola con `IN (...)`.

## 3. `AddPooledDbContextFactory` — un contexto limpio por operación

GraphQL resuelve campos en paralelo y un `DbContext` **no** es seguro para concurrencia.
`AddPooledDbContextFactory` + `RegisterDbContextFactory` entregan una instancia agrupada
(pooled) y limpia por operación, permitiendo resolvers concurrentes sin contención ni
estados de seguimiento compartidos.

## Cómo reproducir la evidencia
En `appsettings.json` subir el nivel de log:
```json
"Microsoft.EntityFrameworkCore.Database.Command": "Information"
```
Ejecutar `dotnet run` y lanzar las consultas anteriores; el SQL aparece en consola.
