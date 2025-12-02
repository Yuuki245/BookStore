using BookStore.Data;
using BookStore.Services;
using BookStore.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cart;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userMgr;

        public CartController(ICartService cart, ApplicationDbContext db, UserManager<IdentityUser> userMgr)
        {
            _cart = cart;
            _db = db;
            _userMgr = userMgr;
        }

        // GET: /Cart
        public async Task<IActionResult> Index()
        {
            var items = await _cart.GetItemsAsync();
            return View(new CartVM { Items = items });
        }

        // POST: /Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int bookId, int qty = 1)
        {
            // Chặn admin không cho thêm vào giỏ hàng
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userMgr.GetUserAsync(User);
                if (user != null && await _userMgr.IsInRoleAsync(user, "Admin"))
                {
                    TempData["Error"] = "Tài khoản Admin không thể mua hàng.";
                    var referer = Request.Headers["Referer"].ToString();
                    if (!string.IsNullOrEmpty(referer))
                        return Redirect(referer);
                    return RedirectToAction(nameof(Index));
                }
            }

            var book = await _db.Books
                .Include(b => b.FlashSale)
                .FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null)
                return NotFound();

            await _cart.AddAsync(book, qty);
            TempData["Success"] = "Đã thêm vào giỏ.";

            // 🔁 Quay lại trang trước nếu có (Home, Books, Details,...)
            var refererUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(refererUrl))
                return Redirect(refererUrl);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int bookId, int qty)
        {
            await _cart.UpdateAsync(bookId, qty);
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int bookId)
        {
            await _cart.RemoveAsync(bookId);
            TempData["Success"] = "Đã xoá khỏi giỏ.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            await _cart.ClearAsync();
            TempData["Success"] = "Đã xoá toàn bộ giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }
    }
}
