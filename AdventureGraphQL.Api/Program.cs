using AdventureGraphQL.Api.Data;
using AdventureGraphQL.Api.GraphQL;
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
    .AddMutationConventions()                  // habilita errores tipados y payloads
    .AddSubscriptionType<Subscription>()
    .AddInMemorySubscriptions()               // transporte en memoria (aprendizaje)
    .RegisterDbContextFactory<AdventureWorksContext>()
    .AddProjections()                          // SELECT solo de columnas pedidas
    .AddFiltering()                            // where: { ... }
    .AddSorting()                              // order: { ... }
    .AddTypeExtension<ProductTypeExtensions>() // Escenario A
    .AddMaxExecutionDepthRule(8)               // seguridad: profundidad máxima
    .ModifyRequestOptions(o =>
        o.IncludeExceptionDetails = builder.Environment.IsDevelopment());

var app = builder.Build();

app.UseWebSockets();   // requerido por las suscripciones
app.MapGraphQL();      // expone POST /graphql y el IDE Nitro

app.Run();
