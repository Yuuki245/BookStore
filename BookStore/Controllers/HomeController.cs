using BookStore.Data;
using BookStore.Helpers;
using BookStore.Models; // 🟢 THÊM USING NÀY
using BookStore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Diagnostics; // 🟢 THÊM USING NÀY

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<HomeController> _logger;
    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    { _logger = logger; _db = db; }

    public async Task<IActionResult> Index()
    {
        var topSellingBookIds = await _db.OrderItems
            .Where(oi => oi.Order != null && oi.Order.Status == "Completed")
            .GroupBy(oi => oi.BookId)
            .Select(g => new {
                BookId = g.Key,
                TotalSold = g.Sum(oi => oi.Quantity)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(12)
            .Select(x => x.BookId)
            .ToListAsync();

        var bestsellers = await _db.Books
            .AsNoTracking()
            .Include(b => b.FlashSale)
            .Where(b => topSellingBookIds.Contains(b.Id))
            .ToListAsync();

        var orderedBestsellers = topSellingBookIds
            .Select(id => bestsellers.FirstOrDefault(b => b.Id == id))
            .Where(b => b != null)
            .ToList()!;

        // Lấy Flash Sale đang active
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

        var flashSaleBooks = activeFlashSale?.Books?.Take(12).ToList() ?? new List<Book>();

        // Đề xuất sách ngẫu nhiên cho bạn
        var allBooks = await _db.Books.AsNoTracking()
            .Include(b => b.FlashSale)
            .ToListAsync();
        
        var random = new Random();
        var recommendedBooks = allBooks
            .OrderBy(x => random.Next())
            .Take(12)
            .ToList();

        var vm = new HomeVM
        {
            Bestsellers = orderedBestsellers,
            NewReleases = recommendedBooks, // Đổi thành đề xuất ngẫu nhiên
            DailySales = await _db.Books.AsNoTracking()
                .Include(b => b.FlashSale)
                .Where(b => b.OriginalPrice != null)
                .OrderBy(b => b.Title)
                .ToListAsync(),
            FlashSaleBooks = flashSaleBooks,
            ActiveFlashSale = activeFlashSale
        };
        return View(vm);
    }

    public IActionResult Privacy() => View();

    public IActionResult About()
    {
        ViewData["Title"] = "About Us";
        return View();
    }

    public IActionResult TrustAndSafety()
    {
        ViewData["Title"] = "Trust and Safety";
        return View();
    }

    public IActionResult Blog()
    {
        ViewData["Title"] = "Blog";
        return View();
    }

    public IActionResult Ambassador()
    {
        ViewData["Title"] = "Ambassador";
        return View();
    }
    public IActionResult Contact()
    {
        ViewData["Title"] = "Liên hệ";
        return View();
    }

    public IActionResult FeatureRequests()
    {
        ViewData["Title"] = "Góp ý Tính năng";
        return View();
    }

    // 🟢 THÊM LẠI ACTION ERROR ĐÃ BỊ MẤT
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}