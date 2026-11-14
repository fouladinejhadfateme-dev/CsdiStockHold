using Microsoft.EntityFrameworkCore;
using StockHold.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockHold.Infrastructure.Persistence.Contexts
{
    public class StockHoldDbContext:DbContext
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
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(c => c.CustomerType)
                    .HasConversion<string>()
                    .HasMaxLength(20);
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
                    .IsUnique();

                entity.HasIndex(r => new
                {
                    r.ProductId,
                    r.Status,
                    r.ExpiresAt
                });

                entity.HasOne<Customer>()
                    .WithMany()
                    .HasForeignKey(r => r.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Product>()
                    .WithMany()
                    .HasForeignKey(r => r.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
