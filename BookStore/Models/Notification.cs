using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BookStore.Models;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public IdentityUser? User { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Type { get; set; } = "Info"; // "Info", "Success", "Warning", "Danger", "Order", "Payment"

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [StringLength(200)]
    public string? LinkUrl { get; set; } // URL để điều hướng khi click vào thông báo
}

