using BookStore.Data;
using BookStore.Models;
using BookStore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _db;
        public BooksController(ApplicationDbContext db) => _db = db;

        // GET: /Books
        public async Task<IActionResult> Index()
        {
            // 🟢 Lấy top 12 sách bán chạy (theo tổng Quantity trong các đơn KHÔNG bị hủy)
            var bestsellers = await _db.Books
                .AsNoTracking()
                .Select(b => new {
                    Book = b,
                    Sold = _db.OrderItems
                        .Where(oi => oi.BookId == b.Id && oi.Order.Status != "Canceled")
                        .Sum(oi => (int?)oi.Quantity) ?? 0
                })
                .OrderByDescending(x => x.Sold)
                .Take(12)
                .Select(x => x.Book)
                .ToListAsync();

            // 🟢 Lấy 12 sách mới thêm (theo Id mới nhất)
            var newReleases = await _db.Books
                .AsNoTracking()
                .OrderByDescending(b => b.Id)
                .Take(12)
                .ToListAsync();

            var vm = new HomeVM
            {
                Bestsellers = bestsellers,
                NewReleases = newReleases
            };

            return View(vm);
        }


        // GET: /Books/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var book = await _db.Books.AsNoTracking()
                                      .Include(b => b.Category)
                                      .FirstOrDefaultAsync(b => b.Id == id);
            return book == null ? NotFound() : View(book);
        }
    }
}
