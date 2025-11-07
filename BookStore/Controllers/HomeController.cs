using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BookStore.Models;

namespace BookStore.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    public HomeController(ILogger<HomeController> logger) => _logger = logger;

<<<<<<< Updated upstream
    // Trang chủ: chuyển thẳng tới danh sách sách
    public IActionResult Index() => RedirectToAction("Index", "Books");

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
=======
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
            Bestsellers = orderedBestsellers, // Logic Bestseller từ lượt trước
            NewReleases = await _db.Books.AsNoTracking().OrderByDescending(b => b.Id).Take(12).ToListAsync(),

            // 🟢 THÊM LOGIC LẤY SÁCH SALE
            DailySales = await _db.Books.AsNoTracking()
                .Where(b => b.OriginalPrice != null) // Chỉ lấy sách có giá gốc (đang sale)
                .OrderBy(b => b.Title) // Sắp xếp theo tên
                .ToListAsync()
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
>>>>>>> Stashed changes
