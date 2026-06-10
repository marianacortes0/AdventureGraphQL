namespace AdventureGraphQL.Api.GraphQL;

public record AddProductInput(
    string Name,
    string ProductNumber,
    decimal ListPrice,
    int? ProductSubcategoryId);

public record ProductPayload(
    int ProductId,
    string Name,
    decimal ListPrice);
