using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data;

public static class Seed
{
    private const string AdminEmail = "admin@bookstore.local";
    private const string AdminPass = "Admin@123"; // ĐỔI khi deploy

    public static async Task EnsureSeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Roles
        if (!await roleMgr.RoleExistsAsync("Admin"))
            await roleMgr.CreateAsync(new IdentityRole("Admin"));
        if (!await roleMgr.RoleExistsAsync("Customer"))
            await roleMgr.CreateAsync(new IdentityRole("Customer"));

        // Admin user
        var admin = await userMgr.Users.FirstOrDefaultAsync(u => u.Email == AdminEmail);
        if (admin == null)
        {
            admin = new IdentityUser { UserName = AdminEmail, Email = AdminEmail, EmailConfirmed = true };
            var created = await userMgr.CreateAsync(admin, AdminPass);
            if (created.Succeeded)
                await userMgr.AddToRoleAsync(admin, "Admin");
        }

        // Seed Category/Book demo nếu trống (giả sử bạn đã có models & DbSet)
        if (!await db.Categories.AnyAsync())
        {
            var c1 = new Models.Category { Name = "Fiction" };
            var c2 = new Models.Category { Name = "Technology" };
            db.Categories.AddRange(c1, c2);
            await db.SaveChangesAsync();

            db.Books.AddRange(
                new Models.Book { Title = "The Great Novel", Author = "A. Writer", Price = 99000, Stock = 50, CategoryId = c1.Id, CoverUrl = "https://via.placeholder.com/300x400?text=Novel" },
                new Models.Book { Title = "ASP.NET Core in Action", Author = "J. Doe", Price = 299000, Stock = 20, CategoryId = c2.Id, CoverUrl = "https://via.placeholder.com/300x400?text=ASP.NET" }
            );
            await db.SaveChangesAsync();
        }
    }
}
