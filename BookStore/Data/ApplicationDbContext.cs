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
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<FlashSale> FlashSales => Set<FlashSale>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
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
        // 🟢 THÊM CẤU HÌNH CHO BLOGPOST
        builder.Entity<BlogPost>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Không xóa bài post nếu user bị xóa

        // WishlistItem - User (n-1)
        builder.Entity<WishlistItem>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa wishlist khi user bị xóa

        // WishlistItem - Book (n-1)
        builder.Entity<WishlistItem>()
            .HasOne(w => w.Book)
            .WithMany()
            .HasForeignKey(w => w.BookId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa wishlist khi sách bị xóa

        // Đảm bảo mỗi user chỉ có 1 wishlist item cho mỗi sách
        builder.Entity<WishlistItem>()
            .HasIndex(w => new { w.UserId, w.BookId })
            .IsUnique();

        // Book - FlashSale (n-1, optional)
        builder.Entity<Book>()
            .HasOne(b => b.FlashSale)
            .WithMany(f => f.Books)
            .HasForeignKey(b => b.FlashSaleId)
            .OnDelete(DeleteBehavior.SetNull); // Set null khi flash sale bị xóa

        // Indexes cho performance
        builder.Entity<WishlistItem>().HasIndex(w => w.UserId);
        builder.Entity<FlashSale>().HasIndex(f => f.StartTime);
        builder.Entity<FlashSale>().HasIndex(f => f.EndTime);

        // Coupon - Indexes
        builder.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
        builder.Entity<Coupon>().HasIndex(c => c.StartDate);
        builder.Entity<Coupon>().HasIndex(c => c.EndDate);

        // Notification - Indexes
        builder.Entity<Notification>().HasIndex(n => n.UserId);
        builder.Entity<Notification>().HasIndex(n => new { n.UserId, n.IsRead });
        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Address - User (n-1)
        builder.Entity<Address>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Address>().HasIndex(a => a.UserId);
        builder.Entity<Address>().HasIndex(a => new { a.UserId, a.IsDefault });

        // PointTransaction - User (n-1)
        builder.Entity<PointTransaction>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PointTransaction>().HasIndex(p => p.UserId);
        builder.Entity<PointTransaction>().HasIndex(p => p.OrderId);
        builder.Entity<PointTransaction>()
            .HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
