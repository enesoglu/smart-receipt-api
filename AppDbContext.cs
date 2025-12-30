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
        }
    }
}