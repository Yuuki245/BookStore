using BookStore.Data;
using BookStore.Helpers;
using BookStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers
{
    public class CouponsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CouponsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Coupons
        public async Task<IActionResult> Index()
        {
            var now = TimeHelper.GetVietnamTime(); // Dùng VN time (GMT+7)
            var allCoupons = await _db.Coupons
                .AsNoTracking()
                .ToListAsync();

            // Mã đang có hiệu lực: IsActive = true, trong khoảng thời gian, chưa hết lượt
            var activeCoupons = allCoupons
                .Where(c =>
                {
                    var startDate = TimeHelper.ToVietnamTime(c.StartDate);
                    var endDate = TimeHelper.ToVietnamTime(c.EndDate);
                    return c.IsActive &&
                           now >= startDate &&
                           now <= endDate &&
                           (c.UsageLimit == null || c.UsedCount < c.UsageLimit);
                })
                .OrderByDescending(c => c.DiscountValue)
                .ToList();

            // Mã sắp diễn ra: IsActive = true, StartDate > now
            var upcomingCoupons = allCoupons
                .Where(c =>
                {
                    var startDate = TimeHelper.ToVietnamTime(c.StartDate);
                    return c.IsActive && startDate > now;
                })
                .OrderBy(c => TimeHelper.ToVietnamTime(c.StartDate))
                .ToList();

            // Mã đã hết hiệu lực: đã hết hạn hoặc đã hết lượt hoặc bị tắt
            var expiredCoupons = allCoupons
                .Where(c =>
                {
                    var endDate = TimeHelper.ToVietnamTime(c.EndDate);
                    return !c.IsActive ||
                           now > endDate ||
                           (c.UsageLimit.HasValue && c.UsedCount >= c.UsageLimit);
                })
                .OrderByDescending(c => TimeHelper.ToVietnamTime(c.EndDate))
                .ToList();

            ViewBag.ActiveCoupons = activeCoupons;
            ViewBag.UpcomingCoupons = upcomingCoupons;
            ViewBag.ExpiredCoupons = expiredCoupons;
            ViewBag.Now = now;

            return View();
        }
    }
}

