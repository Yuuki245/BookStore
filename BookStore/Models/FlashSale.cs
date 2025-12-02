using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookStore.Helpers;

namespace BookStore.Models;

public class FlashSale
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Range(0, 100)]
    public int DiscountPercent { get; set; } // Phần trăm giảm giá (0-100)

    public bool IsActive { get; set; } = true;

    public int? MaxQuantityPerUser { get; set; } // Giới hạn số lượng mỗi user (null = không giới hạn)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Quan hệ với Book (nhiều sách có thể trong một flash sale)
    public ICollection<Book> Books { get; set; } = new List<Book>();

    [NotMapped]
    public bool IsCurrentlyActive
    {
        get
        {
            var now = TimeHelper.GetVietnamTime(); // Dùng VN time (GMT+7)
            var startTime = TimeHelper.ToVietnamTime(StartTime);
            var endTime = TimeHelper.ToVietnamTime(EndTime);
            return IsActive && now >= startTime && now <= endTime;
        }
    }

    [NotMapped]
    public TimeSpan? TimeRemaining
    {
        get
        {
            var now = TimeHelper.GetVietnamTime(); // Dùng VN time (GMT+7)
            if (!IsCurrentlyActive) return null;
            var endTime = TimeHelper.ToVietnamTime(EndTime);
            var remaining = endTime - now;
            return remaining > TimeSpan.Zero ? remaining : null;
        }
    }
}

