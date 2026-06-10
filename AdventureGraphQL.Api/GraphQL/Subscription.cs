using HotChocolate;
using HotChocolate.Types;

namespace AdventureGraphQL.Api.GraphQL;

public class Subscription
{
    /// <summary>Notifica cuando se crea un producto.</summary>
    [Subscribe]
    [Topic]
    public ProductPayload OnProductAdded(
        [EventMessage] ProductPayload product) => product;

    /// <summary>Escenario C: notifica cambios de precio.</summary>
    [Subscribe]
    [Topic]
    public ProductPayload OnPriceChanged(
        [EventMessage] ProductPayload product) => product;
}
