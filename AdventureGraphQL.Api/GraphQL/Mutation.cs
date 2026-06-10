using AdventureGraphQL.Api.Data;
using AdventureGraphQL.Api.Data.Entities;
using HotChocolate;
using HotChocolate.Subscriptions;

namespace AdventureGraphQL.Api.GraphQL;

public class Mutation
{
    /// <summary>Crea un producto y notifica a los suscriptores.</summary>
    public async Task<ProductPayload> AddProductAsync(
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

        var payload = new ProductPayload(
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
            new ProductPayload(product.ProductID, product.Name, product.ListPrice), ct);

        return product;
    }
}
