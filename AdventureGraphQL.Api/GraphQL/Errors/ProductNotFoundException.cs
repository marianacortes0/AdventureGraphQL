namespace AdventureGraphQL.Api.GraphQL;

public class ProductNotFoundException : Exception
{
    public ProductNotFoundException(int id)
        : base($"No existe el producto con id {id}.") { }
}
