using Microsoft.EntityFrameworkCore;
using smart_receipt_api.Models;

namespace smart_receipt_api
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Receipt> Receipts { get; set; }
        public DbSet<ReceiptItem> ReceiptItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Store> Stores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var seedDate = new DateTime(2026, 1, 1);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Receipt>()
                .Property(p => p.TotalAmount)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Category>()
                .Property(p => p.MonthlyBudgetLimit)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<ReceiptItem>()
                .Property(p => p.Price)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<ReceiptItem>()
                .Property(p => p.Quantity)
                .HasColumnType("decimal(10,3)");

            modelBuilder.Entity<ReceiptItem>()
                .Property(p => p.UnitPrice)
                .HasColumnType("decimal(10,2)");

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

            modelBuilder.Entity<Category>()
                .HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Receipt>()
                .HasOne(r => r.Category)
                .WithMany(c => c.Receipts)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Receipt>()
                .HasOne(r => r.Store)
                .WithMany(s => s.Receipts)
                .HasForeignKey(r => r.StoreId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Groceries", IconUrl = "shopping_cart", CreatedAt = seedDate },
                new Category { Id = 2, Name = "Restaurant", IconUrl = "restaurant", CreatedAt = seedDate },
                new Category { Id = 3, Name = "Fuel", IconUrl = "local_gas_station", CreatedAt = seedDate },
                new Category { Id = 4, Name = "Clothing", IconUrl = "checkroom", CreatedAt = seedDate },
                new Category { Id = 5, Name = "Health", IconUrl = "medical_services", CreatedAt = seedDate },
                new Category { Id = 6, Name = "Other", IconUrl = "receipt", CreatedAt = seedDate }
            );
        }
    }
}
