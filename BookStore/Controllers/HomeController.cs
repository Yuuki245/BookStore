using BookStore.Data;
using BookStore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq; // 🟢 1. THÊM USING NÀY

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<HomeController> _logger;
    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    { _logger = logger; _db = db; }

    public async Task<IActionResult> Index()
    {
        // 🟢 2. LOGIC MỚI CHO BESTSELLER
        // Lấy 12 ID sách bán chạy nhất (chỉ tính đơn "Completed")
        var topSellingBookIds = await _db.OrderItems
            .Where(oi => oi.Order != null && oi.Order.Status == "Completed") // Chỉ tính đơn đã hoàn thành
            .GroupBy(oi => oi.BookId) // Nhóm theo Sách
            .Select(g => new {
                BookId = g.Key,
                TotalSold = g.Sum(oi => oi.Quantity) // Tính tổng số lượng bán
            })
            .OrderByDescending(x => x.TotalSold) // Sắp xếp theo tổng số lượng
            .Take(12)
            .Select(x => x.BookId) // Chỉ lấy ID
            .ToListAsync();

        // Lấy thông tin 12 cuốn sách đó
        var bestsellers = await _db.Books
            .AsNoTracking()
            .Where(b => topSellingBookIds.Contains(b.Id))
            .ToListAsync();

        // Sắp xếp lại danh sách 'bestsellers' theo đúng thứ tự bán chạy (vì Where.Contains không đảm bảo thứ tự)
        var orderedBestsellers = topSellingBookIds
            .Select(id => bestsellers.First(b => b.Id == id))
            .ToList();

        // 🟢 3. CẬP NHẬT VIEW MODEL
        var vm = new HomeVM
        {
            Bestsellers = orderedBestsellers, // Gán danh sách đã sắp xếp đúng
            NewReleases = await _db.Books.AsNoTracking().OrderByDescending(b => b.Id).Take(12).ToListAsync()
        };
        return View(vm);
    }

    public IActionResult Privacy() => View();

    // 🟢 (Các Action cho trang tĩnh bạn đã tạo)
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
}