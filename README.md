# AdventureGraphQL

API GraphQL con **C# / .NET 10** y **Hot Chocolate 16**, sobre **EF Core 10** (Database-First)
contra la base `lab08` (esquema AdventureWorks) en SQL Server.

Cumple la guía SENA CEET ADSO: queries con proyección/filtrado/orden/paginación, mutations,
subscriptions, errores tipados y buenas prácticas (DataLoader, factory de DbContext,
límite de profundidad).

## Requisitos
- .NET SDK 10 (fijado en `global.json`).
- SQL Server con la base `lab08` accesible. Cadena en `AdventureGraphQL.Api/appsettings.json`:
  `Server=MARIEZERO;Database=lab08;Trusted_Connection=True;TrustServerCertificate=True;`

## Ejecutar
```bash
dotnet run --project AdventureGraphQL.Api
# Abrir el IDE Nitro en http://localhost:<puerto>/graphql
```

## Estructura
```
AdventureGraphQL.Api/
├─ Data/                 # DbContext + entidades (scaffold EF Core)
├─ GraphQL/
│  ├─ Query.cs           # products, customers, productById, topCustomers (Escenario B)
│  ├─ Mutation.cs        # addProduct, updatePrice (error tipado)
│  ├─ Subscription.cs    # onProductAdded, onPriceChanged (Escenario C)
│  ├─ Inputs/  Errors/  Types/  DataLoaders/   # Escenario A: category + batching
│  └─ ...
└─ Program.cs
docs/                    # Evidencias, documento N+1, buenas prácticas, reflexión
```

Ver [`docs/`](docs/) para evidencias de ejecución y la explicación de cómo se evitó el N+1.
