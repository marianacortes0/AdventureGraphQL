using AdventureGraphQL.Api.Data;
using AdventureGraphQL.Api.Data.Entities;
using GreenDonut;
using Microsoft.EntityFrameworkCore;

namespace AdventureGraphQL.Api.GraphQL;

/// <summary>
/// DataLoader cosido por <c>ProductSubcategoryID</c>. Dado el conjunto de
/// subcategorías de los productos de la petición, resuelve la categoría de cada
/// una en UNA sola consulta (WHERE ProductSubcategoryID IN (...) + JOIN a
/// ProductCategory), evitando el problema N+1 a nivel de subcategoría y categoría.
/// </summary>
public sealed class CategoryBySubcategoryDataLoader(
    IBatchScheduler scheduler,
    IDbContextFactory<AdventureWorksContext> factory,
    DataLoaderOptions options)
    : BatchDataLoader<int, ProductCategory>(scheduler, options)
{
    protected override async Task<IReadOnlyDictionary<int, ProductCategory>>
        LoadBatchAsync(IReadOnlyList<int> subcategoryIds, CancellationToken ct)
    {
        await using var ctx = factory.CreateDbContext();
        return await ctx.ProductSubcategories
            .Where(s => subcategoryIds.Contains(s.ProductSubcategoryID))
            .Select(s => new { s.ProductSubcategoryID, Category = s.ProductCategory })
            .ToDictionaryAsync(x => x.ProductSubcategoryID, x => x.Category, ct);
    }
}
