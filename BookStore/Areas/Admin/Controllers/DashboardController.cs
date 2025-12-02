using BookStore.Data;
using BookStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // ✅ chỉ tính đơn không bị hủy
            var revenueStatuses = new[] { "Confirmed", "Shipped", "Completed" };

            // Chạy các query tuần tự với AsNoTracking để tối ưu
            ViewBag.TotalOrders = await _db.Orders
                .AsNoTracking()
                .CountAsync(o => !o.Status.Equals("Canceled"));

            ViewBag.Revenue = await _db.Orders
                .AsNoTracking()
                .Where(o => revenueStatuses.Contains(o.Status))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.TotalBooks = await _db.Books
                .AsNoTracking()
                .CountAsync();

            return View();
        }

        // =============================
        // API trả dữ liệu JSON cho Chart
        // =============================
        [HttpGet]
        [Route("Admin/Dashboard/GetRevenue")]
        [Produces("application/json")]
        public async Task<IActionResult> GetRevenue(string range = "day")
        {
            var now = DateTime.Now;
            var revenueStatuses = new[] { "Confirmed", "Shipped", "Completed" }; // ✅ lọc doanh thu thực

            if (range == "day")
            {
                var start = now.Date.AddDays(-6); // 7 ngày gần nhất
                var basePoints = Enumerable.Range(0, 7)
                    .Select(i => start.AddDays(i))
                    .ToList();

                var raw = await _db.Orders
                    .AsNoTracking()
                    .Where(o => o.CreatedAt >= start)
                    .Where(o => revenueStatuses.Contains(o.Status))
                    .GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new { Day = g.Key, Total = g.Sum(x => x.TotalAmount) })
                    .ToListAsync();

                var map = raw.ToDictionary(x => x.Day, x => x.Total);
                var labels = basePoints.Select(d => d.ToString("dd/MM")).ToArray();
                var values = basePoints.Select(d => map.TryGetValue(d, out var v) ? v : 0m).ToArray();

                return Json(new { labels, values });
            }
            else if (range == "week")
            {
                // Tính toán 4 tuần gần nhất
                int dayOfWeek = (int)now.Date.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7;
                var startOfWeek = now.Date.AddDays(-(dayOfWeek - 1));
                var start = startOfWeek.AddDays(-7 * 3); // 4 tuần

                // Tạo danh sách các tuần cần hiển thị
                var baseWeeks = Enumerable.Range(0, 4)
                    .Select(i => start.AddDays(7 * i))
                    .ToList();

                // Query tối ưu: group theo tuần trong database
                var raw = await _db.Orders
                    .AsNoTracking()
                    .Where(o => o.CreatedAt >= start && o.CreatedAt < startOfWeek.AddDays(7))
                    .Where(o => revenueStatuses.Contains(o.Status))
                    .ToListAsync();

                // Group theo tuần (tính toán tuần trong memory nhưng đã filter trong DB)
                var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                var byWeek = raw.GroupBy(o =>
                {
                    var week = cal.GetWeekOfYear(o.CreatedAt,
                        System.Globalization.CalendarWeekRule.FirstDay,
                        DayOfWeek.Monday);
                    var year = o.CreatedAt.Year;
                    return (year, week);
                })
                .ToDictionary(k => k.Key, v => v.Sum(x => x.TotalAmount));

                var labels = baseWeeks.Select(d =>
                {
                    var week = cal.GetWeekOfYear(d,
                        System.Globalization.CalendarWeekRule.FirstDay,
                        DayOfWeek.Monday);
                    return $"Tuần {week}";
                }).ToArray();

                var values = baseWeeks.Select(d =>
                {
                    var week = cal.GetWeekOfYear(d,
                        System.Globalization.CalendarWeekRule.FirstDay,
                        DayOfWeek.Monday);
                    var key = (d.Year, week);
                    return byWeek.TryGetValue(key, out var v) ? v : 0m;
                }).ToArray();

                return Json(new { labels, values });
            }
            else
            {
                // 12 tháng trong năm hiện tại
                var year = now.Year;
                var baseMonths = Enumerable.Range(1, 12).ToList();

                var raw = await _db.Orders
                    .AsNoTracking()
                    .Where(o => o.CreatedAt.Year == year)
                    .Where(o => revenueStatuses.Contains(o.Status))
                    .GroupBy(o => o.CreatedAt.Month)
                    .Select(g => new { Month = g.Key, Total = g.Sum(x => x.TotalAmount) })
                    .ToListAsync();

                var map = raw.ToDictionary(x => x.Month, x => x.Total);
                var labels = baseMonths.Select(m => $"T{m}").ToArray();
                var values = baseMonths.Select(m => map.TryGetValue(m, out var v) ? v : 0m).ToArray();

                return Json(new { labels, values });
            }
        }

        // API: Thống kê doanh thu theo thể loại
        [HttpGet]
        [Route("Admin/Dashboard/GetRevenueByCategory")]
        [Produces("application/json")]
        public async Task<IActionResult> GetRevenueByCategory()
        {
            var revenueStatuses = new[] { "Confirmed", "Shipped", "Completed" };
            
            var categoryRevenue = await _db.OrderItems
                .AsNoTracking()
                .Include(oi => oi.Order)
                .Include(oi => oi.Book)
                    .ThenInclude(b => b.Category)
                .Where(oi => oi.Order != null && revenueStatuses.Contains(oi.Order.Status) && oi.Book != null)
                .GroupBy(oi => new { 
                    CategoryId = oi.Book!.CategoryId,
                    CategoryName = oi.Book.Category != null ? oi.Book.Category.Name : "Chưa phân loại"
                })
                .Select(g => new CategoryRevenueVM
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                    TotalBooksSold = g.Sum(oi => oi.Quantity),
                    TotalOrders = g.Select(oi => oi.OrderId).Distinct().Count()
                })
                .OrderByDescending(c => c.Revenue)
                .ToListAsync();

            return Json(categoryRevenue);
        }

        // API: Thống kê sách bán chậm (< 5 sách/tuần trong 4 tuần gần nhất)
        [HttpGet]
        [Route("Admin/Dashboard/GetSlowSellingBooks")]
        [Produces("application/json")]
        public async Task<IActionResult> GetSlowSellingBooks(int page = 1, int pageSize = 5)
        {
            var revenueStatuses = new[] { "Confirmed", "Shipped", "Completed" };
            var now = DateTime.Now;
            var fourWeeksAgo = now.AddDays(-28); // 4 tuần = 28 ngày

            // Tính số lượng bán được trong 4 tuần gần nhất cho mỗi sách - tối ưu query
            var bookSales = await _db.OrderItems
                .AsNoTracking()
                .Where(oi => oi.Order != null 
                    && revenueStatuses.Contains(oi.Order.Status)
                    && oi.Order.CreatedAt >= fourWeeksAgo)
                .GroupBy(oi => oi.BookId)
                .Select(g => new
                {
                    BookId = g.Key,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .Where(x => x.QuantitySold < 5) // Lọc ngay trong database
                .ToListAsync();

            // Lấy thông tin sách cho những sách đã bán (chỉ những sách cần thiết)
            var soldBookIds = bookSales.Select(b => b.BookId).ToList();
            
            List<SlowSellingBookVM> slowSellingBooks = new List<SlowSellingBookVM>();
            
            if (soldBookIds.Any())
            {
                var soldBooksInfo = await _db.Books
                    .AsNoTracking()
                    .Include(b => b.Category)
                    .Where(b => soldBookIds.Contains(b.Id))
                    .Select(b => new
                    {
                        b.Id,
                        b.Title,
                        b.Author,
                        CategoryName = b.Category != null ? b.Category.Name : "Chưa phân loại",
                        b.Stock,
                        b.Price
                    })
                    .ToListAsync();

                // Kết hợp dữ liệu
                slowSellingBooks = bookSales
                    .Join(soldBooksInfo, 
                        bs => bs.BookId, 
                        bi => bi.Id, 
                        (bs, bi) => new SlowSellingBookVM
                        {
                            BookId = bi.Id,
                            Title = bi.Title,
                            Author = bi.Author,
                            CategoryName = bi.CategoryName,
                            QuantitySold = bs.QuantitySold,
                            Revenue = bs.Revenue,
                            Stock = bi.Stock,
                            Price = bi.Price
                        })
                    .OrderBy(b => b.QuantitySold)
                    .ThenByDescending(b => b.Stock)
                    .ToList();
            }

            // Chỉ lấy sách chưa bán được (tối ưu: chỉ lấy những sách cần thiết cho phân trang)
            // Thay vì load tất cả, chỉ load những sách có stock cao (ưu tiên)
            var neverSoldBooks = await _db.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .Where(b => !soldBookIds.Contains(b.Id))
                .OrderByDescending(b => b.Stock)
                .Take(pageSize * 2) // Chỉ lấy đủ cho phân trang, không cần tất cả
                .Select(b => new SlowSellingBookVM
                {
                    BookId = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    CategoryName = b.Category != null ? b.Category.Name : "Chưa phân loại",
                    QuantitySold = 0,
                    Revenue = 0,
                    Stock = b.Stock,
                    Price = b.Price
                })
                .ToListAsync();

            // Kết hợp và sắp xếp lại
            var allSlowSelling = slowSellingBooks
                .Concat(neverSoldBooks)
                .OrderBy(b => b.QuantitySold)
                .ThenByDescending(b => b.Stock)
                .ToList();

            // Phân trang
            var total = allSlowSelling.Count;
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var pagedResult = allSlowSelling
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Json(new
            {
                items = pagedResult,
                page = page,
                pageSize = pageSize,
                total = total,
                totalPages = totalPages
            });
        }
    }
}
