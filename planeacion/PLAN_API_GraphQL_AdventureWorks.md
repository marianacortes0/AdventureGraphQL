# Plan de ejecución — API GraphQL con C# y .NET 10 (AdventureWorks)

> **Documento de instrucciones para una IA asistente de código (o desarrollador).**
> Objetivo: construir, paso a paso y sin omitir verificaciones, una API GraphQL funcional
> con Hot Chocolate 14+, EF Core 10 y SQL Server LocalDB sobre la base **AdventureWorks**,
> cumpliendo la guía SENA CEET ADSO (queries, mutations, subscriptions, proyección,
> filtrado, paginación, errores tipados y buenas prácticas).

---

## 0. Contexto y estado inicial (NO ASUMIR NADA DISTINTO)

| Ítem | Estado |
|---|---|
| Motor de BD | **SQL Server LocalDB** — instancia `(localdb)\MSSQLLocalDB` |
| Base de datos | **AdventureWorks** ya restaurada y operativa |
| IDE | Visual Studio 2022 (17.12+) / 2026 |
| SDK | .NET 10 (`dotnet --version` debe retornar `10.x.x`) |
| Autenticación BD | Windows (`Trusted_Connection=True`) |

**Cadena de conexión oficial del proyecto (usar EXACTAMENTE esta):**

```
Server=(localdb)\MSSQLLocalDB;Database=AdventureWorks;Trusted_Connection=True;TrustServerCertificate=True;
```

### Reglas globales para la IA ejecutora

1. Ejecutar las fases **en orden**. No avanzar a la siguiente fase si el criterio de salida falla.
2. Después de cada fase: compilar (`dotnet build`) y reportar errores antes de continuar.
3. La cadena de conexión vive **únicamente** en `appsettings.json` (nunca en código C#).
4. Todo método de mutación es `async` y recibe `CancellationToken`.
5. Los nombres de clases, archivos y carpetas deben coincidir con los de este documento.
6. Si el scaffolding genera nombres de propiedades distintos (p. ej. `ProductID` vs `ProductId`
   por `--use-database-names`), **adaptar el código de Query/Mutation a los nombres reales
   generados**, no al revés.

---

## FASE 0 — Verificación del entorno

**Comandos de verificación (ejecutar y validar salida):**

```bash
dotnet --version
# Esperado: 10.x.x

dotnet --list-sdks
# Esperado: SDK 10 presente

sqllocaldb info MSSQLLocalDB
# Esperado: instancia existente. Si está detenida: sqllocaldb start MSSQLLocalDB
```

**Verificación de la BD (ejecutar en SSMS o con sqlcmd):**

```sql
USE AdventureWorks;
SELECT TOP 5 ProductID, Name, ListPrice FROM Production.Product;
SELECT TOP 5 CustomerID FROM Sales.Customer;
```

✅ **Criterio de salida F0:** ambas consultas retornan filas.

---

## FASE 1 — Solución, proyecto y scaffolding

### 1.1 Crear solución y proyecto

```bash
mkdir AdventureGraphQL && cd AdventureGraphQL

dotnet new sln -n AdventureGraphQL
dotnet new web -n AdventureGraphQL.Api -f net10.0
dotnet sln add AdventureGraphQL.Api/AdventureGraphQL.Api.csproj
```

> Se usa la plantilla `web` (minimal hosting), **no** `webapi`: GraphQL se sirve por su
> propio middleware y no se necesitan controladores MVC.

### 1.2 Paquetes NuGet

```bash
cd AdventureGraphQL.Api

dotnet add package HotChocolate.AspNetCore
dotnet add package HotChocolate.Data.EntityFramework
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design

# Herramienta EF (si no está instalada globalmente)
dotnet tool install --global dotnet-ef || dotnet tool update --global dotnet-ef
```

### 1.3 Scaffolding Database-First (solo 6 tablas)

```bash
dotnet ef dbcontext scaffold "Server=(localdb)\MSSQLLocalDB;Database=AdventureWorks;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --table Production.Product --table Production.ProductCategory --table Production.ProductSubcategory --table Sales.Customer --table Sales.SalesOrderHeader --table Sales.SalesOrderDetail --context AdventureWorksContext --context-dir Data --output-dir Data/Entities --no-onconfiguring --use-database-names
```

Significado de las banderas (mantenerlas todas):
- `--no-onconfiguring`: no incrusta la cadena de conexión en el código.
- `--context-dir Data` / `--output-dir Data/Entities`: separa contexto y entidades.
- `--use-database-names`: conserva los nombres reales de columnas/tablas.

### 1.4 `appsettings.json`

Reemplazar el contenido del archivo por:

```json
{
  "ConnectionStrings": {
    "AdventureWorks": "Server=(localdb)\\MSSQLLocalDB;Database=AdventureWorks;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

✅ **Criterio de salida F1:** `dotnet build` sin errores; existen `Data/AdventureWorksContext.cs`
y `Data/Entities/*.cs` (Product, ProductCategory, ProductSubcategory, Customer,
SalesOrderHeader, SalesOrderDetail).

---

## FASE 2 — Estructura de carpetas GraphQL

Crear esta estructura dentro de `AdventureGraphQL.Api` (capa GraphQL como fachada delgada):

```
AdventureGraphQL.Api/
├─ Data/
│  ├─ AdventureWorksContext.cs        (generado)
│  └─ Entities/                        (generado)
├─ GraphQL/
│  ├─ Query.cs
│  ├─ Mutation.cs
│  ├─ Subscription.cs
│  ├─ Inputs/
│  │  └─ ProductInputs.cs
│  ├─ Errors/
│  │  └─ ProductNotFoundException.cs
│  ├─ Types/
│  │  └─ ProductTypeExtensions.cs      (Escenario A)
│  └─ DataLoaders/
│     └─ CategoryByIdDataLoader.cs
├─ appsettings.json
└─ Program.cs
```

---

## FASE 3 — `Program.cs` (registro de servicios)

Reemplazar `Program.cs` completo por:

```csharp
using AdventureGraphQL.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1) DbContext como FACTORY agrupada (pooled).
//    GraphQL resuelve campos en paralelo y un DbContext NO es seguro
//    para concurrencia: la factory entrega una instancia limpia por operación.
builder.Services.AddPooledDbContextFactory<AdventureWorksContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("AdventureWorks")));

// 2) Servidor GraphQL + capacidades
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddSubscriptionType<Subscription>()
    .AddInMemorySubscriptions()               // transporte en memoria (aprendizaje)
    .RegisterDbContextFactory<AdventureWorksContext>()
    .AddProjections()                          // SELECT solo de columnas pedidas
    .AddFiltering()                            // where: { ... }
    .AddSorting()                              // order: { ... }
    .AddTypeExtension<ProductTypeExtensions>() // Escenario A (agregar en Fase 7)
    .AddMaxExecutionDepthRule(8)               // seguridad: profundidad máxima
    .ModifyRequestOptions(o => o.Complexity.Enable = true); // control de complejidad

var app = builder.Build();

app.UseWebSockets();   // requerido por las suscripciones
app.MapGraphQL();      // expone POST /graphql y el IDE Nitro

app.Run();
```

> **Nota para la IA:** si en la Fase 3 aún no existen `Mutation`, `Subscription` ni
> `ProductTypeExtensions`, crear primero los archivos de las Fases 4–7 y compilar al final
> de la Fase 5; o comentar temporalmente esas líneas y descomentarlas al crear cada clase.

---

## FASE 4 — Tipo `Query` (`GraphQL/Query.cs`)

```csharp
using AdventureGraphQL.Api.Data;
using AdventureGraphQL.Api.Data.Entities;
using HotChocolate;
using HotChocolate.Data;

public class Query
{
    /// <summary>Lista paginada de productos con filtro, orden y proyección.</summary>
    [UsePaging(MaxPageSize = 50)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Product> GetProducts(AdventureWorksContext context)
        => context.Products;

    /// <summary>Clientes con sus órdenes (la proyección evita el N+1).</summary>
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Customer> GetCustomers(AdventureWorksContext context)
        => context.Customers;

    /// <summary>Un producto por su identificador.</summary>
    [UseProjection]
    public IQueryable<Product> GetProductById(int id, AdventureWorksContext context)
        => context.Products.Where(p => p.ProductID == id);
}
```

> ⚠ Con `--use-database-names` la clave primaria suele llamarse `ProductID` (no `ProductId`).
> **Verificar el nombre real en `Data/Entities/Product.cs` y usar ese.** Lo mismo aplica a
> `CustomerID`, `SalesOrderID`, etc., en todo el documento.

✅ **Criterio de salida F4:** compila (con Mutation/Subscription comentadas si aún no existen).

---

## FASE 5 — Inputs, Mutación y errores tipados

### 5.1 `GraphQL/Inputs/ProductInputs.cs`

```csharp
public record AddProductInput(
    string Name,
    string ProductNumber,
    decimal ListPrice,
    int? ProductSubcategoryId);

public record AddProductPayload(
    int ProductId,
    string Name,
    decimal ListPrice);
```

### 5.2 `GraphQL/Errors/ProductNotFoundException.cs`

```csharp
public class ProductNotFoundException : Exception
{
    public ProductNotFoundException(int id)
        : base($"No existe el producto con id {id}.") { }
}
```

### 5.3 `GraphQL/Mutation.cs`

```csharp
using AdventureGraphQL.Api.Data;
using AdventureGraphQL.Api.Data.Entities;
using HotChocolate;
using HotChocolate.Subscriptions;

public class Mutation
{
    /// <summary>Crea un producto y notifica a los suscriptores.</summary>
    public async Task<AddProductPayload> AddProductAsync(
        AddProductInput input,
        AdventureWorksContext context,
        [Service] ITopicEventSender sender,
        CancellationToken ct)
    {
        // Validación de reglas de negocio
        if (input.ListPrice < 0)
            throw new GraphQLException("El precio no puede ser negativo.");

        var product = new Product
        {
            Name = input.Name,
            ProductNumber = input.ProductNumber,
            ListPrice = input.ListPrice,
            ProductSubcategoryID = input.ProductSubcategoryId,
            // Columnas NOT NULL de Production.Product que el scaffold expone:
            MakeFlag = false,
            FinishedGoodsFlag = false,
            SafetyStockLevel = 100,
            ReorderPoint = 75,
            StandardCost = 0m,
            DaysToManufacture = 0,
            SellStartDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            rowguid = Guid.NewGuid()
        };

        context.Products.Add(product);
        await context.SaveChangesAsync(ct);

        var payload = new AddProductPayload(
            product.ProductID, product.Name, product.ListPrice);

        // Publica el evento para la suscripción (Fase 6)
        await sender.SendAsync(nameof(Subscription.OnProductAdded), payload, ct);

        return payload;
    }

    /// <summary>Actualiza el precio con patrón de error tipado.</summary>
    [Error(typeof(ProductNotFoundException))]
    public async Task<Product> UpdatePriceAsync(
        int id,
        decimal newPrice,
        AdventureWorksContext context,
        [Service] ITopicEventSender sender,
        CancellationToken ct)
    {
        var product = await context.Products.FindAsync([id], ct)
            ?? throw new ProductNotFoundException(id);

        if (newPrice < 0)
            throw new GraphQLException("El precio no puede ser negativo.");

        product.ListPrice = newPrice;
        product.ModifiedDate = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        // Escenario C: notificar cambio de precio
        await sender.SendAsync(nameof(Subscription.OnPriceChanged),
            new AddProductPayload(product.ProductID, product.Name, product.ListPrice), ct);

        return product;
    }
}
```

> ⚠ **Adaptación obligatoria:** los nombres `ProductSubcategoryID`, `rowguid`, `MakeFlag`,
> etc., dependen del scaffold. Abrir `Data/Entities/Product.cs`, listar las propiedades NOT
> NULL sin valor por defecto y asignarlas todas; de lo contrario `SaveChangesAsync` fallará.

✅ **Criterio de salida F5:** compila; queda pendiente probar contra BD en la Fase 8.

---

## FASE 6 — Suscripciones (`GraphQL/Subscription.cs`)

```csharp
using HotChocolate;
using HotChocolate.Types;

public class Subscription
{
    /// <summary>Notifica cuando se crea un producto.</summary>
    [Subscribe]
    [Topic]
    public AddProductPayload OnProductAdded(
        [EventMessage] AddProductPayload product) => product;

    /// <summary>Escenario C: notifica cambios de precio.</summary>
    [Subscribe]
    [Topic]
    public AddProductPayload OnPriceChanged(
        [EventMessage] AddProductPayload product) => product;
}
```

✅ **Criterio de salida F6:** `dotnet build` de toda la solución sin errores
(descomentar ya todas las líneas de `Program.cs` excepto `AddTypeExtension` si la Fase 7
aún no se ha hecho).

---

## FASE 7 — Escenarios de la guía (mínimo 2; aquí se implementan A y C, D opcional)

### Escenario A — Campo `category` en Product (resolver + DataLoader)

**7.1 `GraphQL/DataLoaders/CategoryByIdDataLoader.cs`** (batching, evita N+1):

```csharp
using AdventureGraphQL.Api.Data;
using AdventureGraphQL.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

internal sealed class CategoryByIdDataLoader(
    IBatchScheduler scheduler,
    IDbContextFactory<AdventureWorksContext> factory,
    DataLoaderOptions options)
    : BatchDataLoader<int, ProductCategory>(scheduler, options)
{
    protected override async Task<IReadOnlyDictionary<int, ProductCategory>>
        LoadBatchAsync(IReadOnlyList<int> keys, CancellationToken ct)
    {
        await using var ctx = factory.CreateDbContext();
        return await ctx.ProductCategories
            .Where(c => keys.Contains(c.ProductCategoryID))
            .ToDictionaryAsync(c => c.ProductCategoryID, ct);
    }
}
```

**7.2 `GraphQL/Types/ProductTypeExtensions.cs`** (campo calculado `category`):

```csharp
using AdventureGraphQL.Api.Data;
using AdventureGraphQL.Api.Data.Entities;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

[ExtendObjectType(typeof(Product))]
public class ProductTypeExtensions
{
    /// <summary>Nombre de la categoría: ProductSubcategory → ProductCategory.</summary>
    public async Task<string?> GetCategory(
        [Parent] Product product,
        AdventureWorksContext context,
        CategoryByIdDataLoader categoryLoader,
        CancellationToken ct)
    {
        if (product.ProductSubcategoryID is null) return null;

        var subcategory = await context.ProductSubcategories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.ProductSubcategoryID == product.ProductSubcategoryID, ct);

        if (subcategory is null) return null;

        var category = await categoryLoader
            .LoadAsync(subcategory.ProductCategoryID, ct);

        return category?.Name;
    }
}
```

Registrar en `Program.cs` (descomentar/añadir): `.AddTypeExtension<ProductTypeExtensions>()`.

### Escenario C — Suscripción `onPriceChanged`

Ya quedó implementado en las Fases 5 y 6 (`UpdatePriceAsync` publica el evento y
`Subscription.OnPriceChanged` lo expone). No requiere pasos adicionales.

### Escenario D (OPCIONAL, sube la rúbrica a «Excelente») — JWT + roles

```bash
dotnet add package HotChocolate.AspNetCore.Authorization
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

En `Program.cs`, antes de `builder.Build()`:

```csharp
builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", opt =>
    {
        opt.TokenValidationParameters = new()
        {
            ValidIssuer = "AdventureGraphQL",
            ValidAudience = "AdventureGraphQL",
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]
                    ?? "clave-de-desarrollo-32-caracteres!!"))
        };
    });
builder.Services.AddAuthorization();

// y en la cadena de AddGraphQLServer():
//     .AddAuthorization()
// y después de app.UseWebSockets():
//     app.UseAuthentication();
//     app.UseAuthorization();
```

Proteger la mutación:

```csharp
using HotChocolate.Authorization;

[Authorize(Roles = ["Gestor"])]
public class Mutation { /* ... */ }
```

✅ **Criterio de salida F7:** compila con `ProductTypeExtensions` registrado; query de
productos con campo `category` retorna el nombre de categoría.

---

## FASE 8 — Ejecución y pruebas en Nitro

```bash
dotnet run --project AdventureGraphQL.Api
# Abrir https://localhost:<puerto>/graphql  → IDE Nitro (Banana Cake Pop)
```

Ejecutar en orden y **capturar pantalla de cada una** (evidencias):

**P1 — Productos paginados/ordenados (solo campos pedidos):**

```graphql
query {
  products(first: 5, order: { listPrice: DESC }) {
    nodes { productID name listPrice category }
    pageInfo { hasNextPage endCursor }
  }
}
```

**P2 — Filtro por nombre:**

```graphql
query {
  products(where: { name: { contains: "Mountain" } }) {
    nodes { productID name listPrice }
  }
}
```

**P3 — Cliente con órdenes (relación anidada):**

```graphql
query {
  customers(first: 3) {
    nodes {
      customerID
      salesOrderHeaders { salesOrderID orderDate totalDue }
    }
  }
}
```

**P4 — Mutación (crear producto):**

```graphql
mutation {
  addProduct(input: {
    name: "Casco SENA Edition"
    productNumber: "HL-9999"
    listPrice: 89.99
  }) {
    productId
    name
    listPrice
  }
}
```

**P5 — Validación (debe fallar con error controlado):**

```graphql
mutation {
  addProduct(input: { name: "X", productNumber: "X-0001", listPrice: -1 }) {
    productId
  }
}
```

**P6 — Prueba integradora de suscripción:**
1. Pestaña 1 de Nitro: ejecutar y dejar activa:
   ```graphql
   subscription { onProductAdded { productId name listPrice } }
   ```
2. Pestaña 2: ejecutar P4 con otro `productNumber` (debe ser único en la BD).
3. Verificar que el evento llega en tiempo real a la pestaña 1. **Capturar.**

**P7 — Escenario C:**
1. Pestaña 1: `subscription { onPriceChanged { productId name listPrice } }`
2. Pestaña 2:
   ```graphql
   mutation {
     updatePrice(id: 999999, newPrice: 10.0) {
       product { productID name listPrice }
       errors { ... on ProductNotFoundError { message } }
     }
   }
   ```
   (primero con un id inexistente para evidenciar el error tipado, luego con un id real).

> ⚠ Los nombres exactos de campos en el esquema (`productID` vs `productId`, forma del
> payload de `updatePrice`) los define Hot Chocolate a partir de las clases generadas.
> **Usar el autocompletado/esquema de Nitro como fuente de verdad** y ajustar las consultas.

✅ **Criterio de salida F8:** P1–P7 ejecutadas con capturas; la suscripción recibe eventos.

---

## FASE 9 — Checklist de buenas prácticas (rúbrica)

| Categoría | Práctica | Cómo se cumplió |
|---|---|---|
| Datos | DbContext por operación (factory) | `AddPooledDbContextFactory` + `RegisterDbContextFactory` |
| Datos | Proyección activada | `[UseProjection]` + `.AddProjections()` |
| Datos | DataLoader en relaciones N+1 | `CategoryByIdDataLoader` (Escenario A) |
| Seguridad | Límite profundidad/complejidad | `AddMaxExecutionDepthRule(8)` + `Complexity.Enable` |
| Seguridad | Errores tipados (sin stack traces) | `[Error(typeof(ProductNotFoundException))]` |
| Seguridad | Autorización por rol en mutaciones | Escenario D (opcional) `[Authorize(Roles=["Gestor"])]` |
| Código | Capa GraphQL delgada | Carpeta `GraphQL/` separada de `Data/` |
| Código | Cadena de conexión fuera del código | `appsettings.json` + `--no-onconfiguring` |

---

## FASE 10 — Evidencias a entregar

1. **Repositorio Git** con la solución compilando (`git init`, commits por fase:
   `feat: scaffolding`, `feat: queries`, `feat: mutations`, `feat: subscriptions`,
   `feat: escenarios A y C`, `docs: evidencias`).
2. **Capturas de Nitro**: P1 (query con filtro/orden/paginación), P4 (mutación),
   P5 (validación), P6 (suscripción recibiendo evento), P7 (error tipado).
3. **Documento de 1 página — "Cómo se evitó el problema N+1"**, cubriendo:
   - `[UseProjection]` traduce la selección del cliente a un único `SELECT` con solo
     las columnas pedidas (sin cargar entidades completas ni navegaciones perezosas).
   - `CategoryByIdDataLoader` agrupa (batch) las búsquedas de categoría de N productos
     en **una sola** consulta `WHERE ProductCategoryID IN (...)` por petición.
   - `AddPooledDbContextFactory` entrega un contexto limpio por operación, permitiendo
     resolvers paralelos sin contención ni problemas de concurrencia.
   - Evidencia: comparar el SQL generado (log de EF Core en nivel `Information`)
     con y sin proyección.
4. **Resolución de 2+ escenarios**: A y C implementados (D opcional documentado).
5. **Respuestas a las preguntas de reflexión** (sección 11.3 de la guía):
   - ¿Cuándo preferir REST? — recursos simples y cacheables vía HTTP/CDN, cargas de
     archivos, equipos sin necesidad de flexibilidad de consulta, APIs públicas con
     contratos muy estables.
   - ¿Qué cambia con `[UseProjection]`? — sin él, EF materializa entidades completas
     (más columnas, más memoria, posibles N+1); con él, el SELECT pide solo lo consultado.
   - ¿Riesgos de consultas profundas? — explosión combinatoria de joins, DoS por
     complejidad; se mitiga con `AddMaxExecutionDepthRule` y análisis de complejidad.

---

## Resolución de problemas frecuentes (para la IA ejecutora)

| Síntoma | Causa probable | Acción |
|---|---|---|
| `dotnet ef` no encontrado | Herramienta no instalada | `dotnet tool install --global dotnet-ef` |
| Error de login en scaffolding | LocalDB detenida | `sqllocaldb start MSSQLLocalDB` |
| `SaveChangesAsync` falla en `addProduct` | Columnas NOT NULL sin asignar o `ProductNumber` duplicado | Asignar todas las NOT NULL del entity; usar `ProductNumber` único |
| La suscripción no conecta | WebSockets no habilitado | Confirmar `app.UseWebSockets()` antes de `MapGraphQL()` |
| Campos con nombre inesperado en Nitro | `--use-database-names` | Usar el esquema/autocompletado de Nitro como referencia |
| `Product` requiere campos de versión/estilo | Diferencias de versión AdventureWorks | Revisar `Data/Entities/Product.cs` y asignar requeridos |

---

*Fin del plan. Ejecutar fase por fase, validando cada criterio de salida antes de avanzar.*

---

## ANEXO — Complementos de la guía (opcionales pero recomendados)

### A.1 Escenario B (OPCIONAL) — `topCustomers(year: Int!)` para panel de ventas (BFF)

Agregar este método a `GraphQL/Query.cs`:

```csharp
/// <summary>Escenario B: clientes ordenados por monto total de órdenes del año dado.</summary>
[UsePaging(MaxPageSize = 20)]
public IQueryable<CustomerSales> GetTopCustomers(
    int year, AdventureWorksContext context)
    => context.SalesOrderHeaders
        .Where(o => o.OrderDate.Year == year)
        .GroupBy(o => o.CustomerID)
        .Select(g => new CustomerSales(
            g.Key,
            g.Sum(o => o.TotalDue),
            g.Count()))
        .OrderByDescending(c => c.TotalAmount);

public record CustomerSales(int CustomerId, decimal TotalAmount, int OrderCount);
```

Prueba en Nitro:

```graphql
query {
  topCustomers(year: 2013, first: 5) {
    nodes { customerId totalAmount orderCount }
  }
}
```

> Justificación pedagógica (para la entrega): en REST esto requeriría varias llamadas
> (clientes + órdenes por cliente) o un endpoint ad-hoc; en GraphQL es **una sola
> petición** con la forma exacta que el panel necesita — eso es un BFF.

### A.2 Paso 7 de la guía — alternativa explícita de inyección de la factory

El plan usa `RegisterDbContextFactory` (Hot Chocolate 14 inyecta el contexto
automáticamente). La alternativa explícita que la guía pide conocer es:

```csharp
public IQueryable<Product> GetProducts(
    [Service] IDbContextFactory<AdventureWorksContext> factory)
{
    var context = factory.CreateDbContext();
    return context.Products; // Hot Chocolate gestiona el ciclo de vida
}
```

No reemplazar el código de la Fase 4; este bloque es de referencia conceptual.

### A.3 Notas de producción (sección 10.1 de la guía — solo documentar, NO aplicar en el taller)

```csharp
// 1) Suscripciones escalables entre varias instancias (en lugar de memoria):
//    dotnet add package HotChocolate.Subscriptions.Redis
//    .AddRedisSubscriptions(sp => ConnectionMultiplexer.Connect("redis:6379"))

// 2) Desactivar la introspección si la API es privada:
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(o => o.EnableIntrospection = builder.Environment.IsDevelopment());
```

En el taller se mantiene `AddInMemorySubscriptions()` e introspección activa
(Nitro la necesita para el autocompletado).
