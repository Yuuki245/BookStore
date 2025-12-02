namespace BookStore.Services;

public interface INotificationService
{
    Task CreateNotificationAsync(string userId, string title, string message, string type = "Info", string? linkUrl = null);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(int notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
}

