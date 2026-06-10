using AdventureGraphQL.Api.Data.Entities;
using HotChocolate;
using HotChocolate.Types;

namespace AdventureGraphQL.Api.GraphQL;

[ExtendObjectType(typeof(Product))]
public class ProductTypeExtensions
{
    /// <summary>
    /// Escenario A — campo calculado <c>category</c>: ProductSubcategory → ProductCategory.
    /// Usa un DataLoader cosido por ProductSubcategoryID: todas las resoluciones de
    /// categoría de la petición se agrupan en una sola consulta (evita N+1).
    /// El FK ProductSubcategoryID se trae siempre gracias a [IsProjected(true)].
    /// </summary>
    public async Task<string?> GetCategory(
        [Parent] Product product,
        CategoryBySubcategoryDataLoader categoryLoader,
        CancellationToken ct)
    {
        if (product.ProductSubcategoryID is null) return null;

        var category = await categoryLoader.LoadAsync(product.ProductSubcategoryID.Value, ct);
        return category?.Name;
    }
}
