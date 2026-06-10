using System;
using System.Collections.Generic;
using AdventureGraphQL.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdventureGraphQL.Api.Data;

public partial class AdventureWorksContext : DbContext
{
    public AdventureWorksContext(DbContextOptions<AdventureWorksContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }

    public virtual DbSet<ProductSubcategory> ProductSubcategories { get; set; }

    public virtual DbSet<SalesOrderDetail> SalesOrderDetails { get; set; }

    public virtual DbSet<SalesOrderHeader> SalesOrderHeaders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("SQL_Latin1_General_CP1_CI_AS");

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerID).HasName("PK_Customer_CustomerID");

            entity.ToTable("Customer", "Sales", tb => tb.HasComment("Current customer information. Also see the Person and Store tables."));

            entity.HasIndex(e => e.AccountNumber, "AK_Customer_AccountNumber").IsUnique();

            entity.HasIndex(e => e.rowguid, "AK_Customer_rowguid").IsUnique();

            entity.HasIndex(e => e.TerritoryID, "IX_Customer_TerritoryID");

            entity.Property(e => e.CustomerID).HasComment("Primary key.");
            entity.Property(e => e.AccountNumber)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComputedColumnSql("(isnull('AW'+[dbo].[ufnLeadingZeros]([CustomerID]),''))", false)
                .HasComment("Unique number identifying the customer assigned by the accounting system.");
            entity.Property(e => e.ModifiedDate)
                .HasComment("Date and time the record was last updated.")
                .HasDefaultValueSql("(getdate())", "DF_Customer_ModifiedDate")
                .HasColumnType("datetime");
            entity.Property(e => e.PersonID).HasComment("Foreign key to Person.BusinessEntityID");
            entity.Property(e => e.StoreID).HasComment("Foreign key to Store.BusinessEntityID");
            entity.Property(e => e.TerritoryID).HasComment("ID of the territory in which the customer is located. Foreign key to SalesTerritory.SalesTerritoryID.");
            entity.Property(e => e.rowguid)
                .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                .HasDefaultValueSql("(newid())", "DF_Customer_rowguid");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductID).HasName("PK_Product_ProductID");

            entity.ToTable("Product", "Production", tb => tb.HasComment("Products sold or used in the manfacturing of sold products."));

            entity.HasIndex(e => e.Name, "AK_Product_Name").IsUnique();

            entity.HasIndex(e => e.ProductNumber, "AK_Product_ProductNumber").IsUnique();

            entity.HasIndex(e => e.rowguid, "AK_Product_rowguid").IsUnique();

            entity.Property(e => e.ProductID).HasComment("Primary key for Product records.");
            entity.Property(e => e.Class)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasComment("H = High, M = Medium, L = Low");
            entity.Property(e => e.Color)
                .HasMaxLength(15)
                .HasComment("Product color.");
            entity.Property(e => e.DaysToManufacture).HasComment("Number of days required to manufacture the product.");
            entity.Property(e => e.DiscontinuedDate)
                .HasComment("Date the product was discontinued.")
                .HasColumnType("datetime");
            entity.Property(e => e.FinishedGoodsFlag)
                .HasComment("0 = Product is not a salable item. 1 = Product is salable.")
                .HasDefaultValue(true, "DF_Product_FinishedGoodsFlag");
            entity.Property(e => e.ListPrice)
                .HasComment("Selling price.")
                .HasColumnType("money");
            entity.Property(e => e.MakeFlag)
                .HasComment("0 = Product is purchased, 1 = Product is manufactured in-house.")
                .HasDefaultValue(true, "DF_Product_MakeFlag");
            entity.Property(e => e.ModifiedDate)
                .HasComment("Date and time the record was last updated.")
                .HasDefaultValueSql("(getdate())", "DF_Product_ModifiedDate")
                .HasColumnType("datetime");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasComment("Name of the product.");
            entity.Property(e => e.ProductLine)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasComment("R = Road, M = Mountain, T = Touring, S = Standard");
            entity.Property(e => e.ProductModelID).HasComment("Product is a member of this product model. Foreign key to ProductModel.ProductModelID.");
            entity.Property(e => e.ProductNumber)
                .HasMaxLength(25)
                .HasComment("Unique product identification number.");
            entity.Property(e => e.ProductSubcategoryID).HasComment("Product is a member of this product subcategory. Foreign key to ProductSubCategory.ProductSubCategoryID. ");
            entity.Property(e => e.ReorderPoint).HasComment("Inventory level that triggers a purchase order or work order. ");
            entity.Property(e => e.SafetyStockLevel).HasComment("Minimum inventory quantity. ");
            entity.Property(e => e.SellEndDate)
                .HasComment("Date the product was no longer available for sale.")
                .HasColumnType("datetime");
            entity.Property(e => e.SellStartDate)
                .HasComment("Date the product was available for sale.")
                .HasColumnType("datetime");
            entity.Property(e => e.Size)
                .HasMaxLength(5)
                .HasComment("Product size.");
            entity.Property(e => e.SizeUnitMeasureCode)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Unit of measure for Size column.");
            entity.Property(e => e.StandardCost)
                .HasComment("Standard cost of the product.")
                .HasColumnType("money");
            entity.Property(e => e.Style)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasComment("W = Womens, M = Mens, U = Universal");
            entity.Property(e => e.Weight)
                .HasComment("Product weight.")
                .HasColumnType("decimal(8, 2)");
            entity.Property(e => e.WeightUnitMeasureCode)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasComment("Unit of measure for Weight column.");
            entity.Property(e => e.rowguid)
                .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                .HasDefaultValueSql("(newid())", "DF_Product_rowguid");

            entity.HasOne(d => d.ProductSubcategory).WithMany(p => p.Products).HasForeignKey(d => d.ProductSubcategoryID);
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.ProductCategoryID).HasName("PK_ProductCategory_ProductCategoryID");

            entity.ToTable("ProductCategory", "Production", tb => tb.HasComment("High-level product categorization."));

            entity.HasIndex(e => e.Name, "AK_ProductCategory_Name").IsUnique();

            entity.HasIndex(e => e.rowguid, "AK_ProductCategory_rowguid").IsUnique();

            entity.Property(e => e.ProductCategoryID).HasComment("Primary key for ProductCategory records.");
            entity.Property(e => e.ModifiedDate)
                .HasComment("Date and time the record was last updated.")
                .HasDefaultValueSql("(getdate())", "DF_ProductCategory_ModifiedDate")
                .HasColumnType("datetime");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasComment("Category description.");
            entity.Property(e => e.rowguid)
                .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                .HasDefaultValueSql("(newid())", "DF_ProductCategory_rowguid");
        });

        modelBuilder.Entity<ProductSubcategory>(entity =>
        {
            entity.HasKey(e => e.ProductSubcategoryID).HasName("PK_ProductSubcategory_ProductSubcategoryID");

            entity.ToTable("ProductSubcategory", "Production", tb => tb.HasComment("Product subcategories. See ProductCategory table."));

            entity.HasIndex(e => e.Name, "AK_ProductSubcategory_Name").IsUnique();

            entity.HasIndex(e => e.rowguid, "AK_ProductSubcategory_rowguid").IsUnique();

            entity.Property(e => e.ProductSubcategoryID).HasComment("Primary key for ProductSubcategory records.");
            entity.Property(e => e.ModifiedDate)
                .HasComment("Date and time the record was last updated.")
                .HasDefaultValueSql("(getdate())", "DF_ProductSubcategory_ModifiedDate")
                .HasColumnType("datetime");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasComment("Subcategory description.");
            entity.Property(e => e.ProductCategoryID).HasComment("Product category identification number. Foreign key to ProductCategory.ProductCategoryID.");
            entity.Property(e => e.rowguid)
                .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                .HasDefaultValueSql("(newid())", "DF_ProductSubcategory_rowguid");

            entity.HasOne(d => d.ProductCategory).WithMany(p => p.ProductSubcategories)
                .HasForeignKey(d => d.ProductCategoryID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SalesOrderDetail>(entity =>
        {
            entity.HasKey(e => new { e.SalesOrderID, e.SalesOrderDetailID }).HasName("PK_SalesOrderDetail_SalesOrderID_SalesOrderDetailID");

            entity.ToTable("SalesOrderDetail", "Sales", tb =>
                {
                    tb.HasComment("Individual products associated with a specific sales order. See SalesOrderHeader.");
                    tb.HasTrigger("iduSalesOrderDetail");
                });

            entity.HasIndex(e => e.rowguid, "AK_SalesOrderDetail_rowguid").IsUnique();

            entity.HasIndex(e => e.ProductID, "IX_SalesOrderDetail_ProductID");

            entity.Property(e => e.SalesOrderID).HasComment("Primary key. Foreign key to SalesOrderHeader.SalesOrderID.");
            entity.Property(e => e.SalesOrderDetailID)
                .ValueGeneratedOnAdd()
                .HasComment("Primary key. One incremental unique number per product sold.");
            entity.Property(e => e.CarrierTrackingNumber)
                .HasMaxLength(25)
                .HasComment("Shipment tracking number supplied by the shipper.");
            entity.Property(e => e.LineTotal)
                .HasComputedColumnSql("(isnull(([UnitPrice]*((1.0)-[UnitPriceDiscount]))*[OrderQty],(0.0)))", false)
                .HasComment("Per product subtotal. Computed as UnitPrice * (1 - UnitPriceDiscount) * OrderQty.")
                .HasColumnType("numeric(38, 6)");
            entity.Property(e => e.ModifiedDate)
                .HasComment("Date and time the record was last updated.")
                .HasDefaultValueSql("(getdate())", "DF_SalesOrderDetail_ModifiedDate")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderQty).HasComment("Quantity ordered per product.");
            entity.Property(e => e.ProductID).HasComment("Product sold to customer. Foreign key to Product.ProductID.");
            entity.Property(e => e.SpecialOfferID).HasComment("Promotional code. Foreign key to SpecialOffer.SpecialOfferID.");
            entity.Property(e => e.UnitPrice)
                .HasComment("Selling price of a single product.")
                .HasColumnType("money");
            entity.Property(e => e.UnitPriceDiscount)
                .HasComment("Discount amount.")
                .HasColumnType("money");
            entity.Property(e => e.rowguid)
                .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                .HasDefaultValueSql("(newid())", "DF_SalesOrderDetail_rowguid");

            entity.HasOne(d => d.SalesOrder).WithMany(p => p.SalesOrderDetails).HasForeignKey(d => d.SalesOrderID);
        });

        modelBuilder.Entity<SalesOrderHeader>(entity =>
        {
            entity.HasKey(e => e.SalesOrderID).HasName("PK_SalesOrderHeader_SalesOrderID");

            entity.ToTable("SalesOrderHeader", "Sales", tb =>
                {
                    tb.HasComment("General sales order information.");
                    tb.HasTrigger("uSalesOrderHeader");
                });

            entity.HasIndex(e => e.SalesOrderNumber, "AK_SalesOrderHeader_SalesOrderNumber").IsUnique();

            entity.HasIndex(e => e.rowguid, "AK_SalesOrderHeader_rowguid").IsUnique();

            entity.HasIndex(e => e.CustomerID, "IX_SalesOrderHeader_CustomerID");

            entity.HasIndex(e => e.SalesPersonID, "IX_SalesOrderHeader_SalesPersonID");

            entity.Property(e => e.SalesOrderID).HasComment("Primary key.");
            entity.Property(e => e.AccountNumber)
                .HasMaxLength(15)
                .HasComment("Financial accounting number reference.");
            entity.Property(e => e.BillToAddressID).HasComment("Customer billing address. Foreign key to Address.AddressID.");
            entity.Property(e => e.Comment)
                .HasMaxLength(128)
                .HasComment("Sales representative comments.");
            entity.Property(e => e.CreditCardApprovalCode)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasComment("Approval code provided by the credit card company.");
            entity.Property(e => e.CreditCardID).HasComment("Credit card identification number. Foreign key to CreditCard.CreditCardID.");
            entity.Property(e => e.CurrencyRateID).HasComment("Currency exchange rate used. Foreign key to CurrencyRate.CurrencyRateID.");
            entity.Property(e => e.CustomerID).HasComment("Customer identification number. Foreign key to Customer.BusinessEntityID.");
            entity.Property(e => e.DueDate)
                .HasComment("Date the order is due to the customer.")
                .HasColumnType("datetime");
            entity.Property(e => e.Freight)
                .HasComment("Shipping cost.")
                .HasColumnType("money");
            entity.Property(e => e.ModifiedDate)
                .HasComment("Date and time the record was last updated.")
                .HasDefaultValueSql("(getdate())", "DF_SalesOrderHeader_ModifiedDate")
                .HasColumnType("datetime");
            entity.Property(e => e.OnlineOrderFlag)
                .HasComment("0 = Order placed by sales person. 1 = Order placed online by customer.")
                .HasDefaultValue(true, "DF_SalesOrderHeader_OnlineOrderFlag");
            entity.Property(e => e.OrderDate)
                .HasComment("Dates the sales order was created.")
                .HasDefaultValueSql("(getdate())", "DF_SalesOrderHeader_OrderDate")
                .HasColumnType("datetime");
            entity.Property(e => e.PurchaseOrderNumber)
                .HasMaxLength(25)
                .HasComment("Customer purchase order number reference. ");
            entity.Property(e => e.RevisionNumber).HasComment("Incremental number to track changes to the sales order over time.");
            entity.Property(e => e.SalesOrderNumber)
                .HasMaxLength(25)
                .HasComputedColumnSql("(isnull(N'SO'+CONVERT([nvarchar](23),[SalesOrderID]),N'*** ERROR ***'))", false)
                .HasComment("Unique sales order identification number.");
            entity.Property(e => e.SalesPersonID).HasComment("Sales person who created the sales order. Foreign key to SalesPerson.BusinessEntityID.");
            entity.Property(e => e.ShipDate)
                .HasComment("Date the order was shipped to the customer.")
                .HasColumnType("datetime");
            entity.Property(e => e.ShipMethodID).HasComment("Shipping method. Foreign key to ShipMethod.ShipMethodID.");
            entity.Property(e => e.ShipToAddressID).HasComment("Customer shipping address. Foreign key to Address.AddressID.");
            entity.Property(e => e.Status)
                .HasComment("Order current status. 1 = In process; 2 = Approved; 3 = Backordered; 4 = Rejected; 5 = Shipped; 6 = Cancelled")
                .HasDefaultValue((byte)1, "DF_SalesOrderHeader_Status");
            entity.Property(e => e.SubTotal)
                .HasComment("Sales subtotal. Computed as SUM(SalesOrderDetail.LineTotal)for the appropriate SalesOrderID.")
                .HasColumnType("money");
            entity.Property(e => e.TaxAmt)
                .HasComment("Tax amount.")
                .HasColumnType("money");
            entity.Property(e => e.TerritoryID).HasComment("Territory in which the sale was made. Foreign key to SalesTerritory.SalesTerritoryID.");
            entity.Property(e => e.TotalDue)
                .HasComputedColumnSql("(isnull(([SubTotal]+[TaxAmt])+[Freight],(0)))", false)
                .HasComment("Total due from customer. Computed as Subtotal + TaxAmt + Freight.")
                .HasColumnType("money");
            entity.Property(e => e.rowguid)
                .HasComment("ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.")
                .HasDefaultValueSql("(newid())", "DF_SalesOrderHeader_rowguid");

            entity.HasOne(d => d.Customer).WithMany(p => p.SalesOrderHeaders)
                .HasForeignKey(d => d.CustomerID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
