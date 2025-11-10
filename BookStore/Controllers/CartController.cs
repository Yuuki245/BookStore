using BookStore.Data;
using BookStore.Services;
using BookStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cart;
        private readonly ApplicationDbContext _db;

        public CartController(ICartService cart, ApplicationDbContext db)
        {
            _cart = cart;
            _db = db;
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
            var book = await _db.Books.FindAsync(bookId);
            if (book == null)
                return NotFound();

            await _cart.AddAsync(book, qty);
            TempData["Success"] = "Đã thêm vào giỏ.";

            // 🔁 Quay lại trang trước nếu có (Home, Books, Details,...)
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
                return Redirect(referer);

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
