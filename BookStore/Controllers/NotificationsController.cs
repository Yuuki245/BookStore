using BookStore.Data;
using BookStore.Models;
using BookStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public NotificationsController(INotificationService notificationService, ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _notificationService = notificationService;
        _db = db;
        _userManager = userManager;
    }

    // GET: /Notifications
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 20;
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync();
        var notifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Total = total;

        return View(notifications);
    }

    // GET: /Notifications/GetNotifications (AJAX)
    [HttpGet]
    [Route("Notifications/GetNotifications")]
    [Produces("application/json")]
    public async Task<IActionResult> GetNotifications(int limit = 10)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(new { success = false, message = "Unauthorized" });
        }

        var notifications = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                type = n.Type,
                isRead = n.IsRead,
                createdAt = n.CreatedAt,
                linkUrl = n.LinkUrl
            })
            .ToListAsync();

        return Json(new { success = true, notifications });
    }

    // POST: /Notifications/MarkAsRead
    [HttpPost]
    [Route("Notifications/MarkAsRead")]
    [Produces("application/json")]
    [IgnoreAntiforgeryToken] // Cho phép test qua Swagger
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        await _notificationService.MarkAsReadAsync(id, user.Id);
        return Ok();
    }

    // POST: /Notifications/MarkAllAsRead
    [HttpPost]
    [Route("Notifications/MarkAllAsRead")]
    [Produces("application/json")]
    [IgnoreAntiforgeryToken] // Cho phép test qua Swagger
    public async Task<IActionResult> MarkAllAsRead()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        await _notificationService.MarkAllAsReadAsync(user.Id);
        return Ok();
    }
}

