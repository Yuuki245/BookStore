using BookStore.Data;
using BookStore.Models;
using BookStore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // 🟢 Giữ lại using này
using System.Linq; // 🟢 Giữ lại using này

namespace BookStore.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userMgr;
        public BooksController(ApplicationDbContext db, UserManager<IdentityUser> userMgr)
        {
            _db = db;
            _userMgr = userMgr;
        }

        // GET: /Books
        public async Task<IActionResult> Index(
            string? search, 
            int? categoryId, 
            string? sort = "", 
            int page = 1,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? minRating = null)
        {
            const int PageSize = 12;

            var query = _db.Books
                .Include(b => b.Category)
                .Include(b => b.FlashSale)
                .AsNoTracking();

            // Chỉ Include Reviews khi cần filter hoặc sort theo rating
            bool needReviews = minRating.HasValue || sort == "rating_desc";
            if (needReviews)
            {
                query = query.Include(b => b.Reviews);
            }

            // Full-text search tối ưu - tìm kiếm trong nhiều trường
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                var keywords = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                // Tìm kiếm với nhiều từ khóa (AND logic - tất cả từ khóa phải xuất hiện)
                foreach (var kw in keywords)
                {
                    query = query.Where(b => 
                        b.Title.ToLower().Contains(kw) ||
                        (b.Author != null && b.Author.ToLower().Contains(kw)) ||
                        (b.Description != null && b.Description.ToLower().Contains(kw)) ||
                        (b.Isbn != null && b.Isbn.Contains(kw)));
                }
            }

            // Filter theo danh mục
            if (categoryId.HasValue)
                query = query.Where(b => b.CategoryId == categoryId.Value);

            // Filter theo giá
            if (minPrice.HasValue)
                query = query.Where(b => b.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(b => b.Price <= maxPrice.Value);

            // Filter theo đánh giá tối thiểu
            if (minRating.HasValue && minRating.Value >= 1 && minRating.Value <= 5)
            {
                query = query.Where(b => b.Reviews.Any() && 
                    b.Reviews.Average(r => r.Rating) >= minRating.Value);
            }

            // Sắp xếp - tối ưu bestseller bằng cách sử dụng subquery
            query = sort switch
            {
                "price_asc" => query.OrderBy(b => b.Price),
                "price_desc" => query.OrderByDescending(b => b.Price),
                "title_asc" => query.OrderBy(b => b.Title),
                "title_desc" => query.OrderByDescending(b => b.Title),
                "rating_desc" => query.OrderByDescending(b => b.Reviews.Any() ? 
                    b.Reviews.Average(r => r.Rating) : 0)
                    .ThenByDescending(b => b.Reviews.Count),
                "bestseller" => query.OrderByDescending(b => 
                    _db.OrderItems
                        .Where(oi => oi.BookId == b.Id && 
                                     oi.Order != null && 
                                     oi.Order.Status == "Completed")
                        .Sum(oi => (int?)oi.Quantity) ?? 0)
                    .ThenByDescending(b => b.Id),
                _ => query.OrderByDescending(b => b.Id) // Mới nhất
            };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            var vm = new BookListVM
            {
                Books = items,
                Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
                Search = search,
                CategoryId = categoryId,
                Sort = sort,
                Page = page,
                TotalPages = (int)Math.Ceiling(total / (double)PageSize),
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinRating = minRating
            };

            return View(vm);
        }


        // GET: /Books/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var book = await _db.Books.AsNoTracking()
                                      .Include(b => b.Category)
                                      .Include(b => b.FlashSale)
                                      .Include(b => b.Reviews)
                                          .ThenInclude(r => r.User)
                                      .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null) return NotFound();

            var related = await _db.Books.AsNoTracking()
                .Where(b => b.CategoryId == book.CategoryId && b.Id != id)
                .OrderByDescending(b => b.Id)
                .Take(4)
                .ToListAsync();

            int totalSold = await _db.OrderItems
                .Where(oi => oi.BookId == id &&
                             oi.Order != null &&
                             oi.Order.Status == "Completed")
                .SumAsync(oi => (int?)oi.Quantity) ?? 0;

            var vm = new BookDetailVM
            {
                MainBook = book,
                RelatedBooks = related,
                Reviews = book.Reviews.OrderByDescending(r => r.CreatedAt),
                TotalSold = totalSold
            };

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                if (User.IsInRole("Admin"))
                {
                    vm.UserReviewStatus = "IsAdmin";
                }
                else if (book.Reviews.Any(r => r.UserId == userId))
                {
                    vm.UserReviewStatus = "AlreadyReviewed";
                }
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

            return View(vm);
        }

        [HttpPost]
        [Authorize] // Bắt buộc đăng nhập
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(BookDetailVM vm)
        {
            if (User.IsInRole("Admin"))
            {
                TempData["Error"] = "Tài khoản Admin không thể gửi đánh giá.";
                return RedirectToAction("Details", new { id = vm.NewReview.BookId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookId = vm.NewReview.BookId;

            bool hasPurchased = await _db.Orders
                .AnyAsync(o => o.UserId == userId &&
                               o.Status == "Completed" &&
                               o.Items.Any(i => i.BookId == bookId));

            if (!hasPurchased)
            {
                TempData["Error"] = "Bạn chỉ có thể đánh giá sách bạn đã mua.";
                return RedirectToAction("Details", new { id = bookId });
            }

            bool hasReviewed = await _db.Reviews
                .AnyAsync(r => r.BookId == bookId && r.UserId == userId);

            if (hasReviewed)
            {
                TempData["Error"] = "Bạn đã đánh giá sách này rồi.";
                return RedirectToAction("Details", new { id = vm.NewReview.BookId });
            }

            vm.NewReview.UserId = userId;
            vm.NewReview.CreatedAt = DateTime.UtcNow;

            _db.Reviews.Add(vm.NewReview);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Cảm ơn đánh giá của bạn!";
            return RedirectToAction("Details", new { id = vm.NewReview.BookId });
        }
    }
}