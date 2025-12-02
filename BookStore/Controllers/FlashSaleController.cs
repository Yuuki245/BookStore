using BookStore.Data;
using BookStore.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers;

public class FlashSaleController : Controller
{
    private readonly ApplicationDbContext _db;

    public FlashSaleController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /FlashSale
    public async Task<IActionResult> Index()
    {
        var now = TimeHelper.GetVietnamTime(); // Dùng VN time (GMT+7)
        
        var allFlashSales = await _db.FlashSales
            .AsNoTracking()
            .Include(f => f.Books)
                .ThenInclude(b => b.Category)
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

        if (activeFlashSale == null)
        {
            ViewBag.Message = "Hiện tại không có flash sale nào đang diễn ra.";
            ViewBag.FlashSale = null;
            return View(new List<BookStore.Models.Book>());
        }

        ViewBag.FlashSale = activeFlashSale;
        return View(activeFlashSale.Books.ToList());
    }
}

