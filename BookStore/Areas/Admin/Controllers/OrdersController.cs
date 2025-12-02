using BookStore.Data;
using BookStore.Models.ViewModels;
using BookStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace BookStore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;
    
    public OrdersController(ApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index(string? status, DateTime? from, DateTime? to, int page = 1)
    {
        const int PageSize = 12;

        var q = _db.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(o => o.Status == status);

        if (from.HasValue) q = q.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(o => o.CreatedAt < to.Value.AddDays(1));

        q = q.OrderByDescending(o => o.CreatedAt);

        var total = await q.CountAsync();
        var data = await q.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        // Map UserEmail (ApplicationDbContext kế thừa IdentityDbContext => có DbSet Users)
        var userIds = data.Select(d => d.UserId).Distinct().ToList();
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.Id);

        var rows = data.Select(o => new OrderAdminRowVM
        {
            Id = o.Id,
            CreatedAt = o.CreatedAt,
            UserEmail = users.TryGetValue(o.UserId, out var mail) ? mail! : o.UserId,
            Status = o.Status,
            TotalAmount = o.TotalAmount
        }).ToList();

        var vm = new OrderAdminListVM
        {
            Items = rows,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / (double)PageSize),
            Status = status,
            From = from,
            To = to
        };

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Book)
            .FirstOrDefaultAsync(o => o.Id == id);
        return order == null ? NotFound() : View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        
        // Không cho phép thay đổi trạng thái khi đơn đã bị hủy
        if (order.Status == "Canceled")
        {
            TempData["Error"] = "Không thể thay đổi trạng thái đơn hàng đã bị hủy.";
            return RedirectToAction(nameof(Details), new { id });
        }
        
        // Không cho phép thay đổi trạng thái khi đơn đã hoàn thành
        if (order.Status == "Completed")
        {
            TempData["Error"] = "Không thể thay đổi trạng thái đơn hàng đã hoàn thành.";
            return RedirectToAction(nameof(Details), new { id });
        }
        
        var oldStatus = order.Status;
        order.Status = status;
        await _db.SaveChangesAsync();

        // Tạo thông báo cho user khi trạng thái đơn hàng thay đổi
        var statusMessages = new Dictionary<string, string>
        {
            { "Confirmed", "Đơn hàng #" + order.Id + " đã được xác nhận." },
            { "Shipped", "Đơn hàng #" + order.Id + " đã được gửi đi." },
            { "Completed", "Đơn hàng #" + order.Id + " đã hoàn thành. Cảm ơn bạn đã mua sắm!" },
            { "Canceled", "Đơn hàng #" + order.Id + " đã bị hủy." }
        };

        if (statusMessages.ContainsKey(status) && oldStatus != status)
        {
            var message = statusMessages[status];
            var type = status == "Completed" ? "Success" : status == "Canceled" ? "Danger" : "Info";
            await _notificationService.CreateNotificationAsync(
                order.UserId,
                "Cập nhật đơn hàng",
                message,
                type,
                $"/Orders/Details/{order.Id}"
            );
        }

        TempData["Success"] = "Đã cập nhật trạng thái đơn hàng.";
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? status, DateTime? from, DateTime? to)
    {
        var q = _db.Orders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(o => o.Status == status);
        if (from.HasValue) q = q.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(o => o.CreatedAt < to.Value.AddDays(1));

        var list = await q.OrderByDescending(o => o.CreatedAt)
                          .Include(o => o.Items)
                          .ToListAsync();

        // map email
        var uids = list.Select(o => o.UserId).Distinct().ToList();
        var userMap = await _db.Users
            .Where(u => uids.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.Id);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Id,CreatedAt,UserEmail,Status,TotalAmount,ItemsCount");
        foreach (var o in list)
        {
            var email = userMap.TryGetValue(o.UserId, out var e) ? e : o.UserId;
            sb.AppendLine(string.Join(",",
                o.Id,
                o.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                Csv(email),
                Csv(o.Status),
                o.TotalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                o.Items.Count
            ));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"orders_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv", fileName);

        static string Csv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var t = s.Replace("\"", "\"\"");
            return (t.Contains(',') || t.Contains('"') || t.Contains('\n') || t.Contains('\r'))
                ? $"\"{t}\"" : t;
        }
    }
}
