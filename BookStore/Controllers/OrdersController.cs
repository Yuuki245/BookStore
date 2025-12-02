using BookStore.Data;
using BookStore.Models;
using BookStore.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookStore.Controllers
{
    [Authorize] // khách phải đăng nhập mới xem đơn mình
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OrdersController(ApplicationDbContext db) { _db = db; }

        // GET: /Orders?page=1
        public async Task<IActionResult> Index(int page = 1)
        {
            const int PageSize = 10;
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(uid))
            {
                return Unauthorized();
            }

            var q = _db.Orders
                .AsNoTracking()
                .Where(o => o.UserId == uid)
                .OrderByDescending(o => o.CreatedAt);

            var total = await q.CountAsync();
            var items = await q.Skip((page - 1) * PageSize).Take(PageSize)
                .Select(o => new OrderListItemVM
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount
                })
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);

            return View(items);
        }

        // GET: /Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(uid))
            {
                return Unauthorized();
            }
            
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Items).ThenInclude(i => i.Book)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            if (order.UserId != uid && !User.IsInRole("Admin")) return Forbid(); // bảo vệ quyền xem

            return View(order);
        }

        // POST: /Orders/Cancel/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(uid))
            {
                return Unauthorized();
            }
            
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            if (order.UserId != uid) return Forbid();

            // Chỉ cho phép hủy khi đơn ở trạng thái Pending hoặc Confirmed
            if (order.Status != "Pending" && order.Status != "Confirmed")
            {
                TempData["Warning"] = "Chỉ được hủy đơn khi trạng thái đang chờ (Pending) hoặc đã xác nhận (Confirmed).";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Không cho phép hủy đơn đã bị hủy
            if (order.Status == "Canceled")
            {
                TempData["Warning"] = "Đơn hàng đã bị hủy.";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.Status = "Canceled";
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã hủy đơn hàng.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
