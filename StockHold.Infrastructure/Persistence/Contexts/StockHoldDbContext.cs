
using Microsoft.EntityFrameworkCore;
using StockHold.Domain.Entities;

namespace StockHold.Infrastructure.Persistence.Contexts
{
    public class StockHoldDbContext : DbContext
    {
        public StockHoldDbContext(
            DbContextOptions<StockHoldDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();

        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<Reservation> Reservations => Set<Reservation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_Products_StockQuantity",
                        "[Quantity] >= 0");
                });
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(c => c.CustomerType)
                    .IsRequired();

                entity.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_Customers_CustomerType",
                        "[CustomerType] IN (1, 2)");
                });
            });

            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(r => r.IdempotencyKey)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(r => new
                {
                    r.CustomerId,
                    r.IdempotencyKey
                })
                    .IsUnique()
                    .HasDatabaseName("UX_Reservations_Customer_IdempotencyKey");

                entity.HasIndex(r => new
                {
                    r.ProductId,
                    r.Status,
                    r.ExpiresAt
                })
                    .HasDatabaseName("IX_Reservations_Product_Status_ExpiresAt");

                entity.HasOne<Customer>()
                    .WithMany()
                    .HasForeignKey(r => r.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Product>()
                    .WithMany()
                    .HasForeignKey(r => r.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        "CK_Reservations_Quantity",
                        "[Quantity] > 0");

                    table.HasCheckConstraint(
                        "CK_Reservations_Status",
                        "[Status] IN ('Active', 'Expired', 'Cancelled')");
                });
            });
        }
    }
}
