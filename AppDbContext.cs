using Microsoft.EntityFrameworkCore;
using smart_receipt_api.Models;

namespace smart_receipt_api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Receipt> Receipts { get; set; }
        // public DbSet<ReceiptItem> ReceiptItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // total amount of receipt
            modelBuilder.Entity<Receipt>()
                .Property(p => p.TotalAmount)
                .HasColumnType("decimal(10,2)");

            // price of item 
            // modelBuilder.Entity<ReceiptItem>()
            //    .Property(p => p.Price)
            //    .HasColumnType("decimal(18,2)");
        }
    }
}