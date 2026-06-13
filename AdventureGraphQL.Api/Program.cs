using System.Text;
using AdventureGraphQL.Api.Auth;
using AdventureGraphQL.Api.Data;
using AdventureGraphQL.Api.GraphQL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
    .AddAuthorization()                        // habilita [Authorize] en el esquema (Escenario D)
    .RegisterDbContextFactory<AdventureWorksContext>()
    .AddProjections()                          // SELECT solo de columnas pedidas
    .AddFiltering()                            // where: { ... }
    .AddSorting()                              // order: { ... }
    .AddTypeExtension<ProductTypeExtensions>() // Escenario A
    .AddMaxExecutionDepthRule(8)               // seguridad: profundidad máxima
    .ModifyCostOptions(o =>
    {
        // El análisis de costo de HotChocolate (activo por defecto desde v14)
        // multiplica el costo de los campos hijos por el tamaño de página.
        // Con MaxPageSize=50 y varios campos se supera el límite por defecto
        // (1000). Subimos el techo manteniendo la protección activa.
        o.MaxFieldCost = 50_000;
        o.MaxTypeCost = 50_000;
    })
    .ModifyRequestOptions(o =>
        o.IncludeExceptionDetails = builder.Environment.IsDevelopment());

// 2.1) Login: servicio que firma los JWT.
builder.Services.AddSingleton<TokenService>();

// 2.2) Autenticación JWT: cómo se VALIDA un token que llega en el header.
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });
builder.Services.AddAuthorization();

// 3) CORS: el front (localhost:3000) es un origen distinto al de la API.
builder.Services.AddCors(o => o.AddPolicy("front", p => p
    .WithOrigins("http://localhost:3000")
    .AllowAnyHeader()    // permite el header Authorization (Escenario D)
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors("front");      // debe ir antes de MapGraphQL
app.UseAuthentication();   // lee/valida el JWT del header Authorization
app.UseAuthorization();    // aplica reglas [Authorize] (cuando se usen)
app.UseWebSockets();       // requerido por las suscripciones
app.MapGraphQL();      // expone POST /graphql y el IDE Nitro

app.Run();
