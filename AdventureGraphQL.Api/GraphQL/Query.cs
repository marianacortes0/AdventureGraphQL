using AdventureGraphQL.Api.Data;
using AdventureGraphQL.Api.Data.Entities;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;

namespace AdventureGraphQL.Api.GraphQL;

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

    /// <summary>Escenario B: clientes ordenados por monto total de órdenes del año dado.</summary>
    [UsePaging(MaxPageSize = 20)]
    public IQueryable<CustomerSales> GetTopCustomers(
        int year, AdventureWorksContext context)
        => context.SalesOrderHeaders
            .Where(o => o.OrderDate.Year == year)
            .GroupBy(o => o.CustomerID)
            // EF Core no traduce el constructor de un record posicional dentro de
            // GroupBy; se proyecta con inicializador de objeto sobre una clase.
            .Select(g => new CustomerSales
            {
                CustomerId = g.Key,
                TotalAmount = g.Sum(o => o.TotalDue),
                OrderCount = g.Count()
            })
            .OrderByDescending(c => c.TotalAmount)
            .ThenBy(c => c.CustomerId);
}

public class CustomerSales
{
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public int OrderCount { get; set; }
}
