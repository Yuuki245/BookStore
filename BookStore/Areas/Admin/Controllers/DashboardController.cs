using BookStore.Data;
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
            ViewBag.TotalOrders = await _db.Orders.CountAsync();
            ViewBag.Revenue = await _db.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            ViewBag.TotalBooks = await _db.Books.CountAsync();
            return View();
        }

        // =============================
        // API trả dữ liệu JSON cho Chart
        // =============================
        // Areas/Admin/Controllers/DashboardController.cs
        [HttpGet]
        public async Task<IActionResult> GetRevenue(string range = "day")
        {
            var now = DateTime.Now;

            if (range == "day")
            {
                var start = now.Date.AddDays(-6); // 7 ngày gần nhất
                var basePoints = Enumerable.Range(0, 7)
                    .Select(i => start.AddDays(i))
                    .ToList();

                var raw = await _db.Orders
                    .Where(o => o.CreatedAt >= start)
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
                var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;

                int dayOfWeek = (int)now.Date.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7;
                var startOfWeek = now.Date.AddDays(-(dayOfWeek - 1));
                var start = startOfWeek.AddDays(-7 * 3);

                var baseWeeks = Enumerable.Range(0, 4)
                    .Select(i => start.AddDays(7 * i))
                    .ToList();

                var raw = await _db.Orders
                    .Where(o => o.CreatedAt >= start)
                    .AsNoTracking()
                    .ToListAsync();

                var byWeek = raw.GroupBy(o =>
                {
                    var week = cal.GetWeekOfYear(o.CreatedAt, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                    var year = o.CreatedAt.Year;
                    return (year, week);
                })
                .ToDictionary(k => k.Key, v => v.Sum(x => x.TotalAmount));

                var labels = baseWeeks.Select(d =>
                {
                    var week = cal.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                    return $"Tuần {week}";
                }).ToArray();

                var values = baseWeeks.Select(d =>
                {
                    var week = cal.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
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
                    .Where(o => o.CreatedAt.Year == year)
                    .GroupBy(o => o.CreatedAt.Month)
                    .Select(g => new { Month = g.Key, Total = g.Sum(x => x.TotalAmount) })
                    .ToListAsync();

                var map = raw.ToDictionary(x => x.Month, x => x.Total);

                var labels = baseMonths.Select(m => $"T{m}").ToArray();
                var values = baseMonths.Select(m => map.TryGetValue(m, out var v) ? v : 0m).ToArray();

                return Json(new { labels, values });
            }
        }

    }
}
