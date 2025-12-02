using BookStore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly INotificationService _notificationService;
    private readonly UserManager<IdentityUser> _userManager;

    public NotificationBellViewComponent(INotificationService notificationService, UserManager<IdentityUser> userManager)
    {
        _notificationService = notificationService;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(0); // Không có thông báo nếu chưa đăng nhập
        }

        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return View(0);
        }

        var unreadCount = await _notificationService.GetUnreadCountAsync(user.Id);
        return View(unreadCount);
    }
}

