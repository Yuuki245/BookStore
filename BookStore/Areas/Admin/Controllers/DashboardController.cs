using BookStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalOrders = await _db.Orders.CountAsync();
        ViewBag.Revenue = await _db.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        ViewBag.TotalBooks = await _db.Books.CountAsync();
        return View();
    }
}
