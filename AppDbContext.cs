using Microsoft.EntityFrameworkCore;
using smart_receipt_api.Models;

namespace smart_receipt_api
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Receipt> Receipts { get; set; }
        public DbSet<ReceiptItems> ReceiptItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Store> Stores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // total amount of receipt
            modelBuilder.Entity<Receipt>()
                .Property(p => p.TotalAmount)
                .HasColumnType("decimal(10,2)");

            // price of item 
            modelBuilder.Entity<ReceiptItems>()
                .Property(p => p.Price)
                .HasColumnType("decimal(10,2)");

            // quantity of item
            modelBuilder.Entity<ReceiptItems>()
                .Property(p => p.Quantity)
                .HasColumnType("decimal(10,3)");

            // unit price of item
            modelBuilder.Entity<ReceiptItems>()
                .Property(p => p.UnitPrice)
                .HasColumnType("decimal(10,2)");

            // Foreign key relationships
            modelBuilder.Entity<Receipt>()
                .HasMany(r => r.Items)
                .WithOne(i => i.Receipt)
                .HasForeignKey(i => i.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Receipt>()
                .HasOne(r => r.User)
                .WithMany(u => u.Receipts)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Category relationship
            modelBuilder.Entity<Receipt>()
                .HasOne(r => r.Category)
                .WithMany(c => c.Receipts)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Store relationship
            modelBuilder.Entity<Receipt>()
                .HasOne(r => r.Store)
                .WithMany(s => s.Receipts)
                .HasForeignKey(r => r.StoreId)
                .OnDelete(DeleteBehavior.SetNull);

            // Seed default categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Market", Description = "Günlük market alışverişleri", IconName = "shopping_cart" },
                new Category { Id = 2, Name = "Restoran", Description = "Yemek ve restoran harcamaları", IconName = "restaurant" },
                new Category { Id = 3, Name = "Yakıt", Description = "Benzin ve akaryakıt giderleri", IconName = "local_gas_station" },
                new Category { Id = 4, Name = "Sağlık", Description = "Eczane ve sağlık harcamaları", IconName = "medical_services" },
                new Category { Id = 5, Name = "Diğer", Description = "Diğer harcamalar", IconName = "receipt" }
            );
        }
    }
}