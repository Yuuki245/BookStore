using BookStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Đảm bảo có using này
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public SalesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Admin/Sales
        public async Task<IActionResult> Index()
        {
            var saleItems = await _db.Books
                .AsNoTracking()
                .Where(b => b.OriginalPrice != null) // Sách đang sale
                .OrderBy(b => b.Title)
                .ToListAsync();

            // Tải danh sách sách CHƯA sale để đưa vào dropdown
            ViewBag.BooksList = new SelectList(
                await _db.Books.AsNoTracking()
                         .Where(b => b.OriginalPrice == null) // Chỉ lấy sách chưa sale
                         .OrderBy(b => b.Title)
                         .ToListAsync(),
                "Id", "Title");

            return View(saleItems);
        }

        // POST: /Admin/Sales/AddBookToSale (Thêm thủ công)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBookToSale(int bookId, decimal newSalePrice)
        {
            // 1. Kiểm tra giá mới phải > 0
            if (newSalePrice <= 0)
            {
                TempData["Error"] = "Giá sale mới phải lớn hơn 0.";
                return RedirectToAction(nameof(Index));
            }

            var book = await _db.Books.FindAsync(bookId);
            if (book == null || book.OriginalPrice != null)
            {
                TempData["Error"] = "Sách không hợp lệ hoặc đã được sale.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Kiểm tra giá mới phải RẺ HƠN giá gốc
            if (newSalePrice >= book.Price)
            {
                TempData["Error"] = "Giá sale mới phải nhỏ hơn giá gốc hiện tại.";
                return RedirectToAction(nameof(Index));
            }

            // 3. Áp dụng sale
            book.OriginalPrice = book.Price; // Lưu giá hiện tại (giá gốc)
            book.Price = newSalePrice; // Đặt giá sale mới

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã thêm '{book.Title}' vào danh sách sale.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Sales/RemoveSale (Gỡ 1 sách)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSale(int id)
        {
            var book = await _db.Books.FindAsync(id);
            if (book != null && book.OriginalPrice != null)
            {
                book.Price = book.OriginalPrice.Value;
                book.OriginalPrice = null;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã gỡ sale khỏi '{book.Title}'.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Sales/GenerateNewSale (Tạo ngẫu nhiên)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateNewSale(int count = 12, int maxDiscount = 50)
        {
            // 1. Reset tất cả sale cũ
            await ResetAllSalesLogic();

            if (maxDiscount > 50) maxDiscount = 50;
            var rand = new Random();

            // 2. Lấy ngẫu nhiên 'count' sách (đang không sale)
            var booksToSale = await _db.Books
                .Where(b => b.OriginalPrice == null)
                .OrderBy(x => Guid.NewGuid())
                .Take(count)
                .ToListAsync();

            // 3. Tính toán giá mới
            foreach (var book in booksToSale)
            {
                int discountPercent = rand.Next(10, maxDiscount + 1);
                book.OriginalPrice = book.Price;
                decimal discountAmount = book.Price * (discountPercent / 100.0m);
                book.Price = book.Price - discountAmount;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã tạo sale ngẫu nhiên mới cho {booksToSale.Count} sách.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Sales/ResetAllSales (Reset toàn bộ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAllSales()
        {
            await ResetAllSalesLogic();
            TempData["Success"] = "Đã reset toàn bộ sale.";
            return RedirectToAction(nameof(Index));
        }

        // HÀM HỖ TRỢ (PRIVATE)
        private async Task ResetAllSalesLogic()
        {
            var saleItems = await _db.Books.Where(b => b.OriginalPrice != null).ToListAsync();
            foreach (var item in saleItems)
            {
                item.Price = item.OriginalPrice.Value;
                item.OriginalPrice = null;
            }
            await _db.SaveChangesAsync();
        }
    }
}