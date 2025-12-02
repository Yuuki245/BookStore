using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookStore.Helpers;

namespace BookStore.Models;

public class Coupon
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string DiscountType { get; set; } = "Percentage"; // "Percentage" or "FixedAmount"

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1000000)]
    public decimal DiscountValue { get; set; } // Phần trăm hoặc số tiền giảm

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10000000)]
    public decimal? MinOrderAmount { get; set; } // Đơn hàng tối thiểu để áp dụng

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10000000)]
    public decimal? MaxDiscount { get; set; } // Giảm giá tối đa (cho Percentage)

    public int? UsageLimit { get; set; } // Số lần sử dụng tối đa (null = không giới hạn)

    public int UsedCount { get; set; } = 0; // Số lần đã sử dụng

    public int? MaxUsagePerUser { get; set; } // Số lần sử dụng tối đa cho mỗi user (null = không giới hạn)

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [StringLength(500)]
    public string? Description { get; set; }

    [NotMapped]
    public bool IsValid
    {
        get
        {
            var now = TimeHelper.GetVietnamTime(); // Dùng VN time (GMT+7)
            var startDate = TimeHelper.ToVietnamTime(StartDate);
            var endDate = TimeHelper.ToVietnamTime(EndDate);
            return IsActive &&
                   now >= startDate &&
                   now <= endDate &&
                   (UsageLimit == null || UsedCount < UsageLimit);
        }
    }
}

