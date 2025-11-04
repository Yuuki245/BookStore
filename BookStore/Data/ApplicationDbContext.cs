using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BookStore.Models;

namespace BookStore.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Review> Reviews => Set<Review>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Book - Category (n-1)
        builder.Entity<Book>()
            .HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Order - OrderItems (1-n)
        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId);

        // OrderItem - Book (n-1)
        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Book)
            .WithMany()
            .HasForeignKey(oi => oi.BookId);
        // 🟢 THÊM CẤU HÌNH CHO REVIEW
        // Review - Book (n-1)
        builder.Entity<Review>()
            .HasOne(r => r.Book)
            .WithMany(b => b.Reviews) // 🟢 Thêm ICollection<Review> vào Book.cs (xem Bước 2.5)
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa review nếu sách bị xóa

        // Review - User (n-1)
        builder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.ClientSetNull); // Không xóa review nếu user bị xóa
        // Indexes gợi ý (tìm kiếm nhanh)
        builder.Entity<Book>().HasIndex(b => b.Title);
        builder.Entity<Book>().HasIndex(b => b.CategoryId);
        builder.Entity<Order>().HasIndex(o => o.CreatedAt);

        // Default CreatedAt = UTC
        builder.Entity<Order>()
            .Property(o => o.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
