using BookStore.Data;
using BookStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userMgr;

    public WishlistController(ApplicationDbContext db, UserManager<IdentityUser> userMgr)
    {
        _db = db;
        _userMgr = userMgr;
    }

    // GET: /Wishlist
    public async Task<IActionResult> Index()
    {
        var user = await _userMgr.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

        var items = await _db.WishlistItems
            .AsNoTracking()
            .Include(w => w.Book)
                .ThenInclude(b => b.Category)
            .Where(w => w.UserId == user.Id)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync();

        return View(items);
    }

    // POST: /Wishlist/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int bookId)
    {
        var user = await _userMgr.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var book = await _db.Books.FindAsync(bookId);
        if (book == null) return NotFound();

        // Kiểm tra xem đã có trong wishlist chưa
        var existing = await _db.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == user.Id && w.BookId == bookId);

        if (existing != null)
        {
            TempData["Info"] = "Sách này đã có trong danh sách yêu thích.";
            return RedirectToAction("Details", "Books", new { id = bookId });
        }

        var wishlistItem = new WishlistItem
        {
            UserId = user.Id,
            BookId = bookId,
            AddedAt = DateTime.UtcNow
        };

        _db.WishlistItems.Add(wishlistItem);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã thêm vào danh sách yêu thích!";
        return RedirectToAction("Details", "Books", new { id = bookId });
    }

    // POST: /Wishlist/Remove
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var user = await _userMgr.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var item = await _db.WishlistItems
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == user.Id);

        if (item == null) return NotFound();

        _db.WishlistItems.Remove(item);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã xóa khỏi danh sách yêu thích.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Wishlist/Toggle (AJAX)
    [HttpPost]
    [Route("Wishlist/Toggle")]
    [Produces("application/json")]
    [IgnoreAntiforgeryToken] // Cho phép test qua Swagger
    public async Task<IActionResult> Toggle(int bookId)
    {
        var user = await _userMgr.GetUserAsync(User);
        if (user == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });

        var existing = await _db.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == user.Id && w.BookId == bookId);

        if (existing != null)
        {
            // Xóa khỏi wishlist
            _db.WishlistItems.Remove(existing);
            await _db.SaveChangesAsync();
            return Json(new { success = true, isInWishlist = false, message = "Đã xóa khỏi danh sách yêu thích" });
        }
        else
        {
            // Thêm vào wishlist
            var wishlistItem = new WishlistItem
            {
                UserId = user.Id,
                BookId = bookId,
                AddedAt = DateTime.UtcNow
            };
            _db.WishlistItems.Add(wishlistItem);
            await _db.SaveChangesAsync();
            return Json(new { success = true, isInWishlist = true, message = "Đã thêm vào danh sách yêu thích" });
        }
    }
}

