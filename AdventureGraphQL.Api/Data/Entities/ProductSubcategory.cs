using System;
using System.Collections.Generic;

namespace AdventureGraphQL.Api.Data.Entities;

/// <summary>
/// Product subcategories. See ProductCategory table.
/// </summary>
public partial class ProductSubcategory
{
    /// <summary>
    /// Primary key for ProductSubcategory records.
    /// </summary>
    public int ProductSubcategoryID { get; set; }

    /// <summary>
    /// Product category identification number. Foreign key to ProductCategory.ProductCategoryID.
    /// </summary>
    public int ProductCategoryID { get; set; }

    /// <summary>
    /// Subcategory description.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    public virtual ProductCategory ProductCategory { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
