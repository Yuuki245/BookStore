using BookStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookStore.Controllers;

[Authorize]
public class PointsController : Controller
{
    private readonly ApplicationDbContext _db;

    public PointsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Points
    public async Task<IActionResult> Index(int page = 1)
    {
        const int PageSize = 20;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Tính tổng điểm hiện có
        var totalPoints = await _db.PointTransactions
            .Where(pt => pt.UserId == userId)
            .SumAsync(pt => pt.Points);

        // Lấy lịch sử giao dịch điểm
        var query = _db.PointTransactions
            .AsNoTracking()
            .Where(pt => pt.UserId == userId)
            .OrderByDescending(pt => pt.CreatedAt);

        var total = await query.CountAsync();
        var transactions = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.TotalPoints = totalPoints;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewData["ActivePage"] = "Points";

        return View(transactions);
    }
}

