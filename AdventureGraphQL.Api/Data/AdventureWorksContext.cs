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
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerID).HasName("PK_Customer_CustomerID");

            entity.ToTable("Customer", "Sales");

            entity.HasIndex(e => e.AccountNumber, "AK_Customer_AccountNumber").IsUnique();

            entity.HasIndex(e => e.rowguid, "AK_Customer_rowguid").IsUnique();

            entity.HasIndex(e => e.TerritoryID, "IX_Customer_TerritoryID");

            entity.Property(e => e.AccountNumber).HasMaxLength(10);
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp");
            entity.Property(e => e.rowguid).HasDefaultValueSql("gen_random_uuid()");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductID).HasName("PK_Product_ProductID");

            entity.ToTable("Product", "Production");

            entity.HasIndex(e => e.Name, "AK_Product_Name").IsUnique();

            entity.HasIndex(e => e.ProductNumber, "AK_Product_ProductNumber").IsUnique();

            entity.HasIndex(e => e.rowguid, "AK_Product_rowguid").IsUnique();

            entity.Property(e => e.Class).HasMaxLength(2).IsFixedLength();
            entity.Property(e => e.Color).HasMaxLength(15);
            entity.Property(e => e.DiscontinuedDate).HasColumnType("timestamp");
            entity.Property(e => e.FinishedGoodsFlag).HasDefaultValue(true);
            entity.Property(e => e.ListPrice).HasColumnType("decimal(19,4)");
            entity.Property(e => e.MakeFlag).HasDefaultValue(true);
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.ProductLine).HasMaxLength(2).IsFixedLength();
            entity.Property(e => e.ProductNumber).HasMaxLength(25);
            entity.Property(e => e.SellEndDate).HasColumnType("timestamp");
            entity.Property(e => e.SellStartDate).HasColumnType("timestamp");
            entity.Property(e => e.Size).HasMaxLength(5);
            entity.Property(e => e.SizeUnitMeasureCode).HasMaxLength(3).IsFixedLength();
            entity.Property(e => e.StandardCost).HasColumnType("decimal(19,4)");
            entity.Property(e => e.Style).HasMaxLength(2).IsFixedLength();
            entity.Property(e => e.Weight).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.WeightUnitMeasureCode).HasMaxLength(3).IsFixedLength();
            entity.Property(e => e.rowguid).HasDefaultValueSql("gen_random_uuid()");

            entity.HasOne(d => d.ProductSubcategory).WithMany(p => p.Products).HasForeignKey(d => d.ProductSubcategoryID);
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.ProductCategoryID).HasName("PK_ProductCategory_ProductCategoryID");

            entity.ToTable("ProductCategory", "Production");

            entity.HasIndex(e => e.Name, "AK_ProductCategory_Name").IsUnique();

            entity.HasIndex(e => e.rowguid, "AK_ProductCategory_rowguid").IsUnique();

            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.rowguid).HasDefaultValueSql("gen_random_uuid()");
        });

        modelBuilder.Entity<ProductSubcategory>(entity =>
        {
            entity.HasKey(e => e.ProductSubcategoryID).HasName("PK_ProductSubcategory_ProductSubcategoryID");

            entity.ToTable("ProductSubcategory", "Production");

            entity.HasIndex(e => e.Name, "AK_ProductSubcategory_Name").IsUnique();

            entity.HasIndex(e => e.rowguid, "AK_ProductSubcategory_rowguid").IsUnique();

            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.rowguid).HasDefaultValueSql("gen_random_uuid()");

            entity.HasOne(d => d.ProductCategory).WithMany(p => p.ProductSubcategories)
                .HasForeignKey(d => d.ProductCategoryID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SalesOrderDetail>(entity =>
        {
            entity.HasKey(e => new { e.SalesOrderID, e.SalesOrderDetailID })
                .HasName("PK_SalesOrderDetail_SalesOrderID_SalesOrderDetailID");

            entity.ToTable("SalesOrderDetail", "Sales");

            entity.HasIndex(e => e.rowguid, "AK_SalesOrderDetail_rowguid").IsUnique();

            entity.HasIndex(e => e.ProductID, "IX_SalesOrderDetail_ProductID");

            entity.Property(e => e.SalesOrderDetailID).ValueGeneratedOnAdd();
            entity.Property(e => e.CarrierTrackingNumber).HasMaxLength(25);
            entity.Property(e => e.LineTotal).HasColumnType("numeric(38, 6)");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(19,4)");
            entity.Property(e => e.UnitPriceDiscount).HasColumnType("decimal(19,4)");
            entity.Property(e => e.rowguid).HasDefaultValueSql("gen_random_uuid()");

            entity.HasOne(d => d.SalesOrder).WithMany(p => p.SalesOrderDetails).HasForeignKey(d => d.SalesOrderID);
        });

        modelBuilder.Entity<SalesOrderHeader>(entity =>
        {
            entity.HasKey(e => e.SalesOrderID).HasName("PK_SalesOrderHeader_SalesOrderID");

            entity.ToTable("SalesOrderHeader", "Sales");

            entity.HasIndex(e => e.SalesOrderNumber, "AK_SalesOrderHeader_SalesOrderNumber").IsUnique();

            entity.HasIndex(e => e.rowguid, "AK_SalesOrderHeader_rowguid").IsUnique();

            entity.HasIndex(e => e.CustomerID, "IX_SalesOrderHeader_CustomerID");

            entity.HasIndex(e => e.SalesPersonID, "IX_SalesOrderHeader_SalesPersonID");

            entity.Property(e => e.AccountNumber).HasMaxLength(15);
            entity.Property(e => e.Comment).HasMaxLength(128);
            entity.Property(e => e.CreditCardApprovalCode).HasMaxLength(15);
            entity.Property(e => e.DueDate).HasColumnType("timestamp");
            entity.Property(e => e.Freight).HasColumnType("decimal(19,4)");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp");
            entity.Property(e => e.OnlineOrderFlag).HasDefaultValue(true);
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp");
            entity.Property(e => e.PurchaseOrderNumber).HasMaxLength(25);
            entity.Property(e => e.SalesOrderNumber).HasMaxLength(25);
            entity.Property(e => e.ShipDate).HasColumnType("timestamp");
            entity.Property(e => e.Status).HasDefaultValue((byte)1);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(19,4)");
            entity.Property(e => e.TaxAmt).HasColumnType("decimal(19,4)");
            entity.Property(e => e.TotalDue).HasColumnType("decimal(19,4)");
            entity.Property(e => e.rowguid).HasDefaultValueSql("gen_random_uuid()");

            entity.HasOne(d => d.Customer).WithMany(p => p.SalesOrderHeaders)
                .HasForeignKey(d => d.CustomerID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
