using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BookStore.Models;

public class Order
{
    public int Id { get; set; }

    // Giữ UserId bắt buộc để join IdentityUser
    [Required]
    public string UserId { get; set; } = string.Empty;
    public IdentityUser? User { get; set; }

    [Required, StringLength(200)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1_000_000)]
    public decimal TotalAmount { get; set; }

    // Trạng thái đơn cơ bản
    [Required, StringLength(30)]
    public string Status { get; set; } = "Pending"; // Pending/Confirmed/Shipped/Completed/Canceled

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
