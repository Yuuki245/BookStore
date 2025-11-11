using BookStore.Data;
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
            .Where(b => topSellingBookIds.Contains(b.Id))
            .ToListAsync();

        var orderedBestsellers = topSellingBookIds
            .Select(id => bestsellers.First(b => b.Id == id))
            .ToList();

        var vm = new HomeVM
        {
            Bestsellers = orderedBestsellers,
            NewReleases = await _db.Books.AsNoTracking().OrderByDescending(b => b.Id).Take(12).ToListAsync(),
            DailySales = await _db.Books.AsNoTracking()
                .Where(b => b.OriginalPrice != null)
                .OrderBy(b => b.Title)
                .ToListAsync()
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