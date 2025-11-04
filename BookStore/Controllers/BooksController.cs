using BookStore.Data;
using BookStore.Models;
using BookStore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization; // 🟢 THÊM
using Microsoft.AspNetCore.Identity; // 🟢 THÊM
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // 🟢 THÊM

namespace BookStore.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userMgr;
        public BooksController(ApplicationDbContext db, UserManager<IdentityUser> userMgr) // 🟢 SỬA
        {
            _db = db;
            _userMgr = userMgr; // 🟢 THÊM
        }

        // GET: /Books
        // 🟢 ĐÃ SỬA: Logic lọc, sắp xếp, phân trang cho trang /Books
        public async Task<IActionResult> Index(string? search, int? categoryId, string? sort = "", int page = 1)
        {
            const int PageSize = 12; // Hiển thị 12 sách mỗi trang

            var query = _db.Books
                .Include(b => b.Category)
                .AsNoTracking();

            // 🔸 Lọc theo từ khóa (dựa theo check/Views/Books/Index.cshtml name="search")
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(keyword) ||
                                         (b.Author != null && b.Author.ToLower().Contains(keyword)) ||
                                         (b.Isbn != null && b.Isbn.Contains(keyword)));
            }

            // 🔸 Lọc theo thể loại
            if (categoryId.HasValue)
                query = query.Where(b => b.CategoryId == categoryId.Value);

            // 🔸 Sắp xếp (dựa theo check/Views/Books/Index.cshtml name="sort")
            query = sort switch
            {
                "price_asc" => query.OrderBy(b => b.Price),
                "price_desc" => query.OrderByDescending(b => b.Price),
                "title_asc" => query.OrderBy(b => b.Title),
                "title_desc" => query.OrderByDescending(b => b.Title),
                _ => query.OrderByDescending(b => b.Id) // Mới nhất
            };

            // 🔸 Phân trang
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            // 🔸 Gửi dữ liệu ra View
            var vm = new BookListVM
            {
                Books = items,
                Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
                Search = search,
                CategoryId = categoryId,
                Sort = sort,
                Page = page,
                TotalPages = (int)Math.Ceiling(total / (double)PageSize)
            };

            return View(vm); // <-- Trả về BookListVM
        }


        // GET: /Books/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var book = await _db.Books.AsNoTracking()
                                      .Include(b => b.Category)
                                      .Include(b => b.Reviews)
                                          .ThenInclude(r => r.User)
                                      .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null) return NotFound();

            var related = await _db.Books.AsNoTracking()
                .Where(b => b.CategoryId == book.CategoryId && b.Id != id)
                .OrderByDescending(b => b.Id)
                .Take(4)
                .ToListAsync();

            var vm = new BookDetailVM
            {
                MainBook = book,
                RelatedBooks = related,
                Reviews = book.Reviews.OrderByDescending(r => r.CreatedAt)
            };

            // 🟢 BẮT ĐẦU: Logic kiểm tra trạng thái review
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                // Ưu tiên 1: Kiểm tra Admin
                if (User.IsInRole("Admin"))
                {
                    vm.UserReviewStatus = "IsAdmin";
                }
                // Ưu tiên 2: Kiểm tra đã review chưa
                else if (book.Reviews.Any(r => r.UserId == userId))
                {
                    vm.UserReviewStatus = "AlreadyReviewed";
                }
                // Ưu tiên 3: Kiểm tra đã mua chưa
                else
                {
                    bool hasPurchased = await _db.Orders
                        .AnyAsync(o => o.UserId == userId &&
                                       o.Status == "Completed" &&
                                       o.Items.Any(i => i.BookId == id));

                    vm.UserReviewStatus = hasPurchased ? "CanReview" : "NotPurchased";
                }
            }
            else
            {
                vm.UserReviewStatus = "NotLoggedIn";
            }
            // 🟢 KẾT THÚC: Logic kiểm tra trạng thái review

            return View(vm);
        }
            // 🟢 KẾT THÚC: Logic kiểm tra quyền review

        [HttpPost]
        [Authorize] // Bắt buộc đăng nhập
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(BookDetailVM vm)
        {
            // 1. Kiểm tra Admin (Admin không được review)
            if (User.IsInRole("Admin"))
            {
                TempData["Error"] = "Tài khoản Admin không thể gửi đánh giá.";
                return RedirectToAction("Details", new { id = vm.NewReview.BookId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookId = vm.NewReview.BookId;

            // 2. Kiểm tra đã mua
            bool hasPurchased = await _db.Orders
                .AnyAsync(o => o.UserId == userId &&
                               o.Status == "Completed" &&
                               o.Items.Any(i => i.BookId == bookId));

            if (!hasPurchased)
            {
                TempData["Error"] = "Bạn chỉ có thể đánh giá sách bạn đã mua.";
                return RedirectToAction("Details", new { id = bookId });
            }

            // 3. Kiểm tra đã review (tránh spam)
            bool hasReviewed = await _db.Reviews
                .AnyAsync(r => r.BookId == bookId && r.UserId == userId);

            if (hasReviewed)
            {
                TempData["Error"] = "Bạn đã đánh giá sách này rồi.";
                return RedirectToAction("Details", new { id = bookId });
            }

            // Gán thông tin và lưu
            vm.NewReview.UserId = userId;
            vm.NewReview.CreatedAt = DateTime.UtcNow;

            _db.Reviews.Add(vm.NewReview);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Cảm ơn đánh giá của bạn!";
            return RedirectToAction("Details", new { id = bookId });
        }
    }
}