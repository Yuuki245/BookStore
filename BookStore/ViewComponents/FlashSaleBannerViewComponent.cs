using BookStore.Data;
using BookStore.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.ViewComponents;

public class FlashSaleBannerViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;

    public FlashSaleBannerViewComponent(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var now = TimeHelper.GetVietnamTime(); // Dùng VN time (GMT+7)
        var allFlashSales = await _db.FlashSales
            .AsNoTracking()
            .Include(f => f.Books)
            .Where(f => f.IsActive)
            .ToListAsync();
        
        // Filter bằng VN time
        var activeFlashSale = allFlashSales
            .Where(f =>
            {
                var startTime = TimeHelper.ToVietnamTime(f.StartTime);
                var endTime = TimeHelper.ToVietnamTime(f.EndTime);
                return now >= startTime && now <= endTime;
            })
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefault();

        return View(activeFlashSale);
    }
}

