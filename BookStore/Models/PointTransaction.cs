using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BookStore.Models;

public class PointTransaction
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public IdentityUser? User { get; set; }

    [Required]
    public int Points { get; set; } // Số điểm (có thể âm nếu là sử dụng điểm)

    [Required, StringLength(50)]
    public string TransactionType { get; set; } = string.Empty; // "Earned", "Used", "Expired", "Refunded"

    [StringLength(500)]
    public string? Description { get; set; }

    public int? OrderId { get; set; } // Liên kết với đơn hàng (nếu có)
    public Order? Order { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public bool IsEarned => TransactionType == "Earned" || TransactionType == "Refunded";
    
    [NotMapped]
    public bool IsUsed => TransactionType == "Used";
}

